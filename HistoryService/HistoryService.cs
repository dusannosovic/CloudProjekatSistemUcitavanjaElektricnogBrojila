using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CurrentmeterSaver;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace HistoryService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class HistoryService : StatelessService
    {
        public HistoryService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[] { new ServiceInstanceListener(context => this.CreateInternalListener(context)) };
        }
        private ICommunicationListener CreateInternalListener(StatelessServiceContext context)
        {
            string host = context.NodeContext.IPAddressOrFQDN;

            var endpointConfig = context.CodePackageActivationContext.GetEndpoint("HistoryServiceEndpoint");
            int port = endpointConfig.Port;
            var scheme = endpointConfig.Protocol.ToString();
            string uri = string.Format(CultureInfo.InvariantCulture, "net.{0}://{1}:{2}/HistoryServiceEndpoint", scheme, host, port);

            var listener = new WcfCommunicationListener<IHistoryS>(
                serviceContext: context,
                wcfServiceObject: new HistoryS(),
                listenerBinding: WcfUtility.CreateTcpListenerBinding(maxMessageSize: 1024 * 1024 * 1024),
                address: new System.ServiceModel.EndpointAddress(uri)
                );

            ServiceEventSource.Current.Message("Napravljen listener!! ");
            return listener;
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.
            DateTime dt = DateTime.Now;
            long iterations = 0;
            await Task.Delay(TimeSpan.FromSeconds(40), cancellationToken);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);
                if (dt.Day == 1)
                {
                    bool tempbool = await GetDataFromCurrentMeter();
                }
                dt = dt.AddDays(1);

                await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
            }
        }
        async Task<bool> GetDataFromCurrentMeter()
        {
            Random random = new Random();
            FabricClient fabricClient = new FabricClient();
            int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
            var binding = WcfUtility.CreateTcpClientBinding();
            int index = 0;
            //for (int i = 0; i < partitionsNumber; i++)
            //{
            ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                new ServicePartitionKey(random.Next(partitionsNumber)));
            List<CurrentMeter> currentMeters = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.GetAllActiveData());
            if (currentMeters.Count > 0)
            {
                try
                {
                    CloudStorageAccount _storageAccount;
                    CloudTable _table;
                    string a = ConfigurationManager.AppSettings["DataConnectionString"];
                    _storageAccount = CloudStorageAccount.Parse(a);
                    CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
                    _table = tableClient.GetTableReference("HistoryDataStorage");
                    foreach (CurrentMeter currentMeter in currentMeters)
                    {
                        CurrentMeterEntity currentMeterEntity = new CurrentMeterEntity(currentMeter.ID, currentMeter.CurrentMeterID, currentMeter.Location, currentMeter.OldState, currentMeter.NewState);
                        TableOperation insertOperation = TableOperation.InsertOrReplace(currentMeterEntity);
                        _table.Execute(insertOperation);
                    }
                    bool tempBool = false;
                    for (int i = 0; i < partitionsNumber; i++)
                    {
                        ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient2 = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                            new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                            new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                            new ServicePartitionKey(index%partitionsNumber));
                        tempBool = await servicePartitionClient2.InvokeWithRetryAsync(client => client.Channel.DeleteAllActiveData());
                        index++;
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
                                new ServicePartitionKey(index1%partitionsNumber1));
                            bool tempPublish = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.Publish("history"));
                            index1++;
                        }

                }
                catch
                {
                    ServiceEventSource.Current.Message("Nije napravljen cloud");
                }
            }
            return true;
        }
    }
}
