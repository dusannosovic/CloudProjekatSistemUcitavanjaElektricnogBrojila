using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrentmeterSaver
{
    class CurrentMeterSaverService : ICurrentMeterSaverService
    {
        IReliableDictionary<string, CurrentMeter> CurrentMeterDict;
        IReliableStateManager StateManager;

        public CurrentMeterSaverService()
        {

        }
        public CurrentMeterSaverService(IReliableStateManager stateManager)
        {
            StateManager = stateManager;
        }
        public async Task<bool> AddCurrentMeter(string id, string currentMeterId, string location, double oldState, double newState)
        {
            CurrentMeterDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, CurrentMeter>>("CurrentMeterActiveData");
            using(var tx = this.StateManager.CreateTransaction())
            {
                await CurrentMeterDict.TryAddAsync(tx, id, new CurrentMeter(id, currentMeterId, location, oldState, newState));
                await tx.CommitAsync();
            }
            return true;
        }
    }
}
