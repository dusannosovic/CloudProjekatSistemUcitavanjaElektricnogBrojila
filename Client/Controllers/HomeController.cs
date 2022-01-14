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
using HistoryDataService;

namespace Client.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        [Route("/HomeController/PostData")]
        public async Task<IActionResult> PostData(string id, string idCurrentMeter, string location, double oldState, double newState)
        {
            FabricClient fabricClient = new FabricClient();
            int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
            var binding = WcfUtility.CreateTcpClientBinding();
            int index = 0;
            //for (int i = 0; i < partitionsNumber; i++)
            //{
            ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                new ServicePartitionKey(0));

            bool a = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.AddCurrentMeter(id,idCurrentMeter,location,oldState,newState));
            return View("Index");
        }
        public async Task<IActionResult> About()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            FabricClient fabricClient = new FabricClient();
            int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"))).Count;
            var binding = WcfUtility.CreateTcpClientBinding();
            int index = 0;
            //for (int i = 0; i < partitionsNumber; i++)
            //{
            ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<ICurrentMeterSaverService>>(
                new WcfCommunicationClientFactory<ICurrentMeterSaverService>(clientBinding: binding),
                new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/CurrentmeterSaver"),
                new ServicePartitionKey(0));

            currentMeters = await servicePartitionClient.InvokeWithRetryAsync(client => client.Channel.GetAllActiveData());

            return View(currentMeters);
        }

        public async Task<IActionResult> Contact()
        {
            List<CurrentMeter> currentMeters = new List<CurrentMeter>();
            FabricClient fabricClient = new FabricClient();
            int partitionsNumber = (await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/HistoryDataService"))).Count;
            var binding = WcfUtility.CreateTcpClientBinding();
            int index = 0;
            //for (int i = 0; i < partitionsNumber; i++)
            //{
            ServicePartitionClient<WcfCommunicationClient<IHistoryData>> servicePartitionClient = new ServicePartitionClient<WcfCommunicationClient<IHistoryData>>(
                new WcfCommunicationClientFactory<IHistoryData>(clientBinding: binding),
                new Uri("fabric:/CloudProjekatSistemUcitavanjaElektricnogBrojila/HistoryDataService"),
                new ServicePartitionKey(0));

            currentMeters =  servicePartitionClient.InvokeWithRetry(client => client.Channel.GetAllHistoricalData());

            return View(currentMeters);
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
