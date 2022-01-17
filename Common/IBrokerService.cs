using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface IBrokerService
    {
        [OperationContract]
        Task<bool> Subscribe(string type);
        [OperationContract]
        Task<bool> Unsubscribe(string type);
        [OperationContract]
        Task<bool> Publish(string topic);
        [OperationContract]
        Task<bool> PublishActive(List<CurrentMeter> currentMeters);
        [OperationContract]
        Task<bool> PublishHistory(List<CurrentMeter> currentMeters);
        [OperationContract]
        Task<List<CurrentMeter>> GetHistoryData();
        [OperationContract]
        Task<List<CurrentMeter>> GetActiveData();
    }
}
