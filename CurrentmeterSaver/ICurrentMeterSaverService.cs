using Common;
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
        Task<int> AddCurrentMeter(int idi,string currentMeterId, string location, double oldState, double newState);
        [OperationContract]
        Task<List<CurrentMeter>> GetAllActiveData();
        [OperationContract]
        Task<bool> DeleteAllActiveData();
    }
}
