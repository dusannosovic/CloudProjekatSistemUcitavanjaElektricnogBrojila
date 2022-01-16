using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HistoryService
{
    [ServiceContract]
    public interface IHistoryS
    {
        [OperationContract]
        List<CurrentMeter> GetAllHistoricalData();
    }
}
