using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CurrentmeterSaver;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Spire.Email;
using Spire.Email.Pop3;

namespace MailService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class MailService : StatefulService
    {
        public MailService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
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
            int a;
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
                a = await MailServiceFunction();
                if (a > 0)
                {
                   await SendMailsData();
                }
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
        public async Task<int> MailServiceFunction()
        {
            try
            {
                Pop3Client pop = new Pop3Client();
                pop.Host = "pop.gmail.com";
                pop.Username = "currentmetermailservice@gmail.com";
                pop.Password = "Bs3265ca";
                pop.Port = 995;
                pop.EnableSsl = true;
                pop.Connect();
                int numberofMails = pop.GetMessageCount();
                var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    for (int i = 1; i <= numberofMails; i++)
                    {
                        MailMessage message = pop.GetMessage(i);
                        string[] mail = message.BodyText.Split(';');
                        try
                        {
                            CurrentMeter currentMeter = new CurrentMeter();
                            currentMeter.ID = i.ToString();
                            currentMeter.CurrentMeterID = mail[0];
                            currentMeter.Location = mail[1];
                            currentMeter.OldState = Convert.ToDouble(mail[2]);
                            currentMeter.NewState = Convert.ToDouble(mail[3]);
                            await CurrentMeterActiveData.TryAddAsync(tx, currentMeter.ID, currentMeter);
                        }
                        catch
                        {
                            ServiceEventSource.Current.Message("Pogresan format mail-a");
                        }
                    }
                    await tx.CommitAsync();
                }
                pop.DeleteAllMessages();
                pop.Disconnect();
                return numberofMails;
            }
            catch
            {
                ServiceEventSource.Current.Message("Email servis trenutno ne radi");
                return 0;
            }
            
        }
        public async Task SendMailsData()
        {
            try
            {
                var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
                FabricClient fabricClient = new FabricClient();
                int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
                var binding = WcfUtility.CreateTcpClientBinding();
                int index = 0;
                //var CurrentMeterActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var enumerator = (await CurrentMeterActiveData.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                    {
                        CurrentMeter currentMeter = (await CurrentMeterActiveData.TryGetValueAsync(tx, enumerator.Current.Key)).Value;
                        int id = -1;
                        for (int i = 0; i < partitionsNumber; i++)
                        {
                            ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                                new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                                new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                                new ServicePartitionKey(index%partitionsNumber));
                            id = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.AddCurrentMeter(id,currentMeter.CurrentMeterID, currentMeter.Location, currentMeter.OldState, currentMeter.NewState));
                            index++;
                        }   
                        
                        if (id>0)
                        {
                            await CurrentMeterActiveData.TryRemoveAsync(tx, enumerator.Current.Key);
                        }
                    }
                    await tx.CommitAsync();
                }

            }
            catch
            {
                ServiceEventSource.Current.Message("Servis currentmetersaver trenutno nije dostupan");
            }
        }
    }
}
