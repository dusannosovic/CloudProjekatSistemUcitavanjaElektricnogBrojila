using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    public class BrokerService : IBrokerService
    {
        IReliableDictionary<string, bool> Subscribed;
        IReliableDictionary<string, CurrentMeter> ActiveData;
        IReliableDictionary<string, CurrentMeter> HistoryData;
        IReliableStateManager StateManager;

        public BrokerService(IReliableStateManager stateManager)
        {
            StateManager = stateManager;
        }
        public BrokerService()
        {

        }
        public async Task<bool> Publish(string topic)
        {
        Subscribed = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>("Subscribe");

            using (var tx = this.StateManager.CreateTransaction())
            {
                    if ((await Subscribed.TryGetValueAsync(tx, topic)).Value)
                    {
                        var myBinding = new NetTcpBinding(SecurityMode.None);
                        var myEndpoint = new EndpointAddress("net.tcp://localhost:52811/WebCommunication");
                    
                        using (var myChannelFactory = new ChannelFactory<IClientService>(myBinding, myEndpoint))
                        {
                            IClientService clientService = null;
                            try
                            {
                                clientService = myChannelFactory.CreateChannel();
                                clientService.Publish(topic);
                                ((ICommunicationObject)clientService).Close();
                                myChannelFactory.Close();
                            }
                            catch
                            {
                                (clientService as ICommunicationObject)?.Abort();
                                return false;
                            }
                            
                        }
                    }
                }

            return true;
        }
        public async Task<bool> PublishActive(List<CurrentMeter> currentMeters)
        {
            try
            {
                ActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("ActiveData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var enumerator = (await ActiveData.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                    while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                    {
                        await ActiveData.TryRemoveAsync(tx, enumerator.Current.Key);
                    }
                    foreach (CurrentMeter currentMeter in currentMeters)
                    {
                        await ActiveData.TryAddAsync(tx, currentMeter.ID, currentMeter);
                    }
                    await tx.CommitAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
            
        }
        public async Task<bool> PublishHistory(List<CurrentMeter> currentMeters)
        {
            try
            {
                HistoryData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("HistoryData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    foreach (CurrentMeter currentMeter in currentMeters)
                    {
                        await HistoryData.TryAddAsync(tx, currentMeter.ID, currentMeter);
                    }
                    await tx.CommitAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> Subscribe(string type)
        {
            Subscribed = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>("Subscribe");
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await Subscribed.TryRemoveAsync(tx, type);
                    await Subscribed.TryAddAsync(tx, type, true);
                    await tx.CommitAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Unsubscribe(string type)
        {
            Subscribed = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>("Subscribe");
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    await Subscribed.TryRemoveAsync(tx, type);
                    await Subscribed.TryAddAsync(tx, type, false);
                    await tx.CommitAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<CurrentMeter>> GetHistoryData()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            HistoryData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("HistoryData");
            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await HistoryData.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    currentMeters.Add(enumerator.Current.Value);
                }
            }
            return currentMeters;
        }

        public async Task<List<CurrentMeter>> GetActiveData()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            ActiveData = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("ActiveData");
            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await ActiveData.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    currentMeters.Add(enumerator.Current.Value);
                }
            }
            return currentMeters;
        }
    }
}
