using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            try
            {
                CloudStorageAccount _storageAccount;
                CloudTable _table;
                string a = ConfigurationManager.AppSettings["DataConnectionString"];
                _storageAccount = CloudStorageAccount.Parse(a);
                CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
                _table = tableClient.GetTableReference("CountTableStorage");
                var results = from g in _table.CreateQuery<CurrentMeterEntity>() where g.PartitionKey == "ActiveCurrentMeterData" select g;
                foreach(CurrentMeterEntity currentMeterEntity in results.ToList())
                {
                var currentEntity = new CurrentMeterEntity()
                {
                    PartitionKey = currentMeterEntity.PartitionKey,
                    RowKey = currentMeterEntity.RowKey,
                    ETag = "*"

                };

                
                    TableOperation deleteOperation = TableOperation.Delete(currentEntity);
                    _table.Execute(deleteOperation);
                }
            }
            catch 
            {
                ServiceEventSource.Current.Message("Nije napravljen cloud");
            }
            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerator = (await CurrentMeterDict.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(new System.Threading.CancellationToken()))
                {
                    await CurrentMeterDict.TryRemoveAsync(tx, enumerator.Current.Key);
                }
                await tx.CommitAsync();
            }

            return true;
        }
    }
}
