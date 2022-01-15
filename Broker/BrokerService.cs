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
            try
            {
                using (var tx = this.StateManager.CreateTransaction())
                {
                    if ((await Subscribed.TryGetValueAsync(tx, topic)).Value)
                    {
                        var myBinding = new NetTcpBinding(SecurityMode.None);
                        var myEndpoint = new EndpointAddress("net.tcp://localhost:52811/WebCommunication");
                    
                        using (var myChannelFactory = new ChannelFactory<IClientService>(myBinding, myEndpoint))
                        {
                            IClientService clientService = null;
                            clientService = myChannelFactory.CreateChannel();
                            clientService.Publish();
                            myChannelFactory.Close();
                        }
                    }
                }
            }
            catch { return false; }

            return true;
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
    }
}
