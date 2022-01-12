using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace CurrentmeterSaver
{
    [ServiceContract]
    public interface ICurrentMeterSaverService
    {
        [OperationContract]
        Task<bool> AddCurrentMeter(string id, string currentMeterId, string location, double oldState, double newState);
    }
}
