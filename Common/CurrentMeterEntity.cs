using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CurrentMeterEntity : TableEntity
    {
        public CurrentMeterEntity()
        {

        }
        public CurrentMeterEntity(string iD, string currentMeterID, string location, double oldState, double newState)
        {
            RowKey = iD;
            PartitionKey = "ActiveCurrentMeterData";
            OldState = oldState;
            NewState = newState;
            Location = location;
            CurrentMeterID = currentMeterID;
        }
        public string CurrentMeterID { get; set; }
        public double OldState { get; set; }
        public double NewState { get; set; }
        public string Location { get; set; }
    }
}
