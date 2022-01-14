using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HistoryDataService
{
    [ServiceContract]
    public interface IHistoryData
    {
        [OperationContract]
        List<CurrentMeter> GetAllHistoricalData();
    }
}
