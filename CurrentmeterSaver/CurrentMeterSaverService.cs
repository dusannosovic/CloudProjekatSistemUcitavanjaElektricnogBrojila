using Broker;
using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrentmeterSaver
{
    class CurrentMeterSaverService : ICurrentMeterSaverService
    {
        IReliableDictionary<string, CurrentMeter> CurrentMeterDict;
        IReliableDictionary<string, bool> Subscribed;
        IReliableStateManager StateManager;

        public CurrentMeterSaverService()
        {

        }
        public CurrentMeterSaverService(IReliableStateManager stateManager)
        {
            StateManager = stateManager;
        }
        public async Task<int> AddCurrentMeter(int idi,string currentMeterId, string location, double oldState, double newState)
        {
            CloudStorageAccount _storageAccount;
            CloudTable _table;
            string a = ConfigurationManager.AppSettings["DataConnectionString"];
            _storageAccount = CloudStorageAccount.Parse(a);
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("CurrentMeterDataStorage");
            var results = from g in _table.CreateQuery<CurrentMeterEntity>() where g.PartitionKey == "CurrentMeterData" select g;
            string id;
            int idReturn;
            if (idi < 0)
            {
                idReturn = results.ToList().Count;
                id = results.ToList().Count.ToString();
            }
            else
            {
                idReturn = idi;
                id = idi.ToString();
            }
            CurrentMeterDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            using (var tx = this.StateManager.CreateTransaction())
            {
                await CurrentMeterDict.TryAddAsync(tx, id, new CurrentMeter(id, currentMeterId, location, oldState, newState));
                await tx.CommitAsync();
            }
            List<CurrentMeter> currentMeters = await GetAllActiveData();
                FabricClient fabricClient = new FabricClient();
                int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"))).Count;
                var binding = WcfUtility.CreateTcpClientBinding();
                int index = 0;
                for (int i = 0; i < partitionsNumber; i++)
                {
                    ServicePartitionClient<WcfCommunicationClient<IBrokerService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<IBrokerService>>(
                        new WcfCommunicationClientFactory<IBrokerService>(clientBinding: binding),
                        new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"),
                        new ServicePartitionKey(index%partitionsNumber));
                    bool tempPublish = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.PublishActive(currentMeters));
                    index++;
                }
            
            return idReturn;
        }

        public async Task<List<CurrentMeter>> GetAllActiveData()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            CurrentMeterDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            using(var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await CurrentMeterDict.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    currentMeters.Add(enumerator.Current.Value);
                }
            }
            return currentMeters;
        }
        public async Task<bool> DeleteAllActiveData()
        {
            CurrentMeterDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await CurrentMeterDict.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    await CurrentMeterDict.TryRemoveAsync(tx, enumerator.Current.Key);
                }
                await tx.CommitAsync();
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
                bool tempPublish = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.PublishActive(new List<CurrentMeter>()));
                index1++;
            }


            return true;
        }
    }
}
