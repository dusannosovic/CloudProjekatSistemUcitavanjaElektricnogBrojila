using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Client.Models;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Wcf;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Client;
using Microsoft.ServiceFabric.Services.Client;
using CurrentmeterSaver;
using Common;
using Broker;
using System.ServiceModel;
using HistoryService;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            /* FabricClient fabricClient1 = new FabricClient();
             int partitionsNumber1 = (await fabricClient1.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"))).Count;
             var binding1 = WcfUtility.CreateTcpClientBinding();
             int index1 = 0;
             for (int i = 0; i < partitionsNumber1; i++)
             {
                 ServicePartitionClient<WcfCommunicationClient<IBrokerService>> servicePartitionClient1 = new ServicePartitionClient<WcfCommunicationClient<IBrokerService>>(
                     new WcfCommunicationClientFactory<IBrokerService>(clientBinding: binding1),
                     new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"),
                     new ServicePartitionKey(index1 % partitionsNumber1));
                 bool b = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.Unsubscribe("active"));
                 bool c = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.Unsubscribe("history"));
                 index1++;
             }*/
            ViewData["Title"] = null;
            return View();
        }
        [HttpPost]
        [Route("/HomeController/PostData")]
        public async Task<IActionResult> PostData(string idCurrentMeter, string location, double oldState, double newState)
        {
            if (string.IsNullOrEmpty(idCurrentMeter))
            {
                ViewData["Title"] = "Nije dodato novo merenje";
                return View("Index");
            }
            if (string.IsNullOrEmpty(location))
            {
                ViewData["Title"] = "Nije dodato novo merenje";
                return View("Index");
            }
            try
            {
                int id = -1;
                FabricClient fabricClient = new FabricClient();
                int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
                var binding = WcfUtility.CreateTcpClientBinding();
                int index = 0;
                for (int i = 0; i < partitionsNumber; i++)
                {
                    ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                        new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                        new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                        new ServicePartitionKey(index % partitionsNumber));
                    id = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.AddCurrentMeter(id, idCurrentMeter, location, oldState, newState));
                    index++;
                }
                ViewData["Title"] = "Uspesno dodato novo merenje";
                return View("Index");
            }
            catch
            {
                ViewData["Title"] = "Nije dodato novo merenje";
                return View("Index");
            }

        }
        public async Task<IActionResult> About()
        {
            ViewData["About"] = null;
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();

            /*FabricClient fabricClient = new FabricClient();
            int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
            var binding = WcfUtility.CreateTcpClientBinding();
            int index = 0;
            for (int i = 0; i < partitionsNumber; i++)
            {
                ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                    new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                    new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                    new ServicePartitionKey(index%partitionsNumber));
                currentMeters = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.GetAllActiveData());
                index++;
            }*/
            try
            {
                FabricClient fabricClient1 = new FabricClient();
                int partitionsNumber1 = (await fabricClient1.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"))).Count;
                var binding1 = WcfUtility.CreateTcpClientBinding();
                int index1 = 0;
                for (int i = 0; i < partitionsNumber1; i++)
                {
                    ServicePartitionClient<WcfCommunicationClient<IBrokerService>> servicePartitionClient1 = new ServicePartitionClient<WcfCommunicationClient<IBrokerService>>(
                        new WcfCommunicationClientFactory<IBrokerService>(clientBinding: binding1),
                        new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"),
                        new ServicePartitionKey(index1 % partitionsNumber1));
                    currentMeters = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.GetActiveData());

                    index1++;
                }
                return View(currentMeters);
            }
            catch
            {
                ViewData["About"] = "Servis nije dostupan";
                return View();
            }
            
        }

        public async Task<IActionResult> Contact()
        {
            ViewData["Contact"] = null;
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            /*var myBinding = new NetTcpBinding(SecurityMode.None);
            var myEndpoint = new EndpointAddress("net.tcp://localhost:54675/HistoryServiceEndpoint");
            using (var myChannelFactory = new ChannelFactory<IHistoryS>(myBinding, myEndpoint))
            {

                IHistoryS client = null;
                try
                {
                    client = myChannelFactory.CreateChannel();
                    currentMeters = client.GetAllHistoricalData();
                    ((ICommunicationObject)client).Close();
                    myChannelFactory.Close();
                }
                catch
                {
                    (client as ICommunicationObject)?.Abort();
                }
            }*/

            try
            {
                FabricClient fabricClient1 = new FabricClient();
                int partitionsNumber1 = (await fabricClient1.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"))).Count;
                var binding1 = WcfUtility.CreateTcpClientBinding();
                int index1 = 0;
                for (int i = 0; i < partitionsNumber1; i++)
                {
                    ServicePartitionClient<WcfCommunicationClient<IBrokerService>> servicePartitionClient1 = new ServicePartitionClient<WcfCommunicationClient<IBrokerService>>(
                        new WcfCommunicationClientFactory<IBrokerService>(clientBinding: binding1),
                        new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/Broker"),
                        new ServicePartitionKey(index1 % partitionsNumber1));
                    currentMeters = await servicePartitionClient1.InvokeWithRetryAsync(client => client.Channel.GetHistoryData());
                    index1++;
                }
                return View(currentMeters);
            }
            catch
            {
                ViewData["Contact"] = "Servis trenutno nije dostupan";
                return View();
            }
            //currentMeters =  servicePartitionClient.InvokeWithRetry(client => client.Channel.GetAllHistoricalData());
            
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
