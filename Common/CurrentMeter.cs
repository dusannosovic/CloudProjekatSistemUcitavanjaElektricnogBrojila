using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CurrentMeter
    {
        public CurrentMeter()
        {

        }
        public CurrentMeter(string iD, string currentMeterID, string location, double oldState, double newState)
        {
            ID = iD;
            CurrentMeterID = currentMeterID;
            Location = location;
            OldState = oldState;
            NewState = newState;
        }
        public string ID { get; set; }
        public string CurrentMeterID { get; set; }
        public string Location { get; set; }
        public double OldState { get; set; }
        public double NewState { get; set; }

    }
}
