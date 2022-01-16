using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace HistoryService
{
    public class HistoryS : IHistoryS
    {
        public List<CurrentMeter> GetAllHistoricalData()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            CloudStorageAccount _storageAccount;
            CloudTable _table;
            string a = ConfigurationManager.AppSettings["DataConnectionString"];
            _storageAccount = CloudStorageAccount.Parse(a);
            CloudTableClient tableClient = new CloudTableClient(new Uri(_storageAccount.TableEndpoint.AbsoluteUri), _storageAccount.Credentials);
            _table = tableClient.GetTableReference("HistoryDataStorage");
            var results = from g in _table.CreateQuery<CurrentMeterEntity>() where g.PartitionKey == "ActiveCurrentMeterData" select g;
            foreach (CurrentMeterEntity currentMeterEntity in results.ToList())
            {
                currentMeters.Add(new CurrentMeter(currentMeterEntity.RowKey, currentMeterEntity.CurrentMeterID, currentMeterEntity.Location, currentMeterEntity.OldState, currentMeterEntity.NewState));
            }
            return currentMeters;
        }
    }
}
