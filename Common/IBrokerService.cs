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
    }
}
