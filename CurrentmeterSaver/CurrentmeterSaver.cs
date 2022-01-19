﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Spire.Email;
using Spire.Email.Pop3;

namespace CurrentmeterSaver
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CurrentmeterSaver : StatefulService
    {
        CurrentMeterSaverService currentMeterSaverService;
        public CurrentmeterSaver(StatefulServiceContext context)
            : base(context)
        { currentMeterSaverService = new CurrentMeterSaverService(this.StateManager); }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] { new ServiceReplicaListener(context => this.CreateInternalListener(context)) };
        }
        private ICommunicationListener CreateInternalListener(ServiceContext context)
        {

            EndpointResourceDescription internalEndpoint = context.CodePackageActivationContext.GetEndpoint("ProcessingServiceEndpoint");
            string uriPrefix = String.Format(
                   "{0}://+:{1}/{2}/{3}-{4}/",
                   internalEndpoint.Protocol,
                   internalEndpoint.Port,
                   context.PartitionId,
                   context.ReplicaOrInstanceId,
                   Guid.NewGuid());

            string nodeIP = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

            string uriPublished = uriPrefix.Replace("+", nodeIP);
            return new WcfCommunicationListener<ICurrentMeterSaverService>(context, currentMeterSaverService , WcfUtility.CreateTcpListenerBinding(), uriPrefix);
        }
        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            await ReadFromTable();
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            await SendDataToBroker(cancellationToken);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }
                //await MailService();
                AddToTableStorage();
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }

        }

        public async Task ReadFromTable()
        {
            try
            {
                CloudStorageAccount _storageAccount;
                CloudTable _table;
                string a = ConfigurationManager.AppSettings["DataConnectionString"];
                _storageAccount = CloudStorageAccount.Parse(a);
                CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
                _table = tableClient.GetTableReference("CurrentMeterDataStorage");
                var results = from g in _table.CreateQuery<CurrentMeterEntity>() where g.PartitionKey == "CurrentMeterData" && !g.HistoryData select g;
                if (results.ToList().Count > 0)
                {
                    var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        foreach (CurrentMeterEntity currentMeterEntity in results.ToList())
                        {
                            await CurrentMeterActiveData.TryAddAsync(tx, currentMeterEntity.RowKey, new CurrentMeter(currentMeterEntity.RowKey, currentMeterEntity.CurrentMeterID, currentMeterEntity.Location, currentMeterEntity.OldState, currentMeterEntity.NewState));
                        }
                        await tx.CommitAsync();
                    }
                }
            }
            catch
            {
                ServiceEventSource.Current.Message("Nije napravljen cloud");
            }
        }
        public async Task AddToTableStorage()
        {
            List<CurrentMeterEntity> currentMeterEntities = new List<CurrentMeterEntity>();
            var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            
            using(var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await CurrentMeterActiveData.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while(await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    CurrentMeter currentMeter = (await CurrentMeterActiveData.TryGetValueAsync(tx, enumerator.Current.Key)).Value;
                    currentMeterEntities.Add(new CurrentMeterEntity(currentMeter.ID, currentMeter.CurrentMeterID, currentMeter.Location, currentMeter.OldState, currentMeter.NewState,false));
                }
            }

            try
            {
                CloudStorageAccount _storageAccount;
                CloudTable _table;
                string a = ConfigurationManager.AppSettings["DataConnectionString"];
                _storageAccount = CloudStorageAccount.Parse(a);
                CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
                _table = tableClient.GetTableReference("CurrentMeterDataStorage");
                foreach (CurrentMeterEntity currentMeterEntity in currentMeterEntities)
                {
                    TableOperation insertOperation = TableOperation.InsertOrReplace(currentMeterEntity);
                    _table.Execute(insertOperation);
                }
            }
            catch
            {
                ServiceEventSource.Current.Message("Nije napravljen cloud");
            }
        }
        public async Task SendDataToBroker(CancellationToken cancellationToken)
        {
            try
            {
                bool tempPublish = false;
                List<CurrentMeter> currentMeters = new List<CurrentMeter>();
                var CurrentMeterDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var enumerator = (await CurrentMeterDict.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                    {
                        currentMeters.Add(enumerator.Current.Value);
                    }
                }
                FabricClient fabricClient1 = new FabricClient();
                int partitionsNumber1 = (await fabricClient1.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"))).Count;
                var binding1 = WcfUtility.CreateTcpClientBinding();
                int index1 = 0;
                for (int i = 0; i < partitionsNumber1; i++)
                {
                    ServicePartitionClient<WcfCommunicationClient<IBrokerService>> servicePartitionClient1 = new ServicePartitionClient<WcfCommunicationClient<IBrokerService>>(
                        new WcfCommunicationClientFactory<IBrokerService>(clientBinding: binding1),
                        new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"),
                        new ServicePartitionKey(index1 % partitionsNumber1));
                    while (!tempPublish)
                    {
                        tempPublish = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.PublishActive(currentMeters));
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                    index1++;
                }
            }
            catch (Exception e)
            {
                string err = e.Message;
                ServiceEventSource.Current.Message(err);
            }
        }


    }
}
