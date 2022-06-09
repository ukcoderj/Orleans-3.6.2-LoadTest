using LoadTest.Grains.Interfaces;
using LoadTest.Grains.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansLoadTestConsole
{
    public class GrainConsoleCalls : ITestCalls, IDisposable
    {
        IClusterClient _client;

        public GrainConsoleCalls()
        {
        }

        public async Task Init()
        {
            _client = new ClientBuilder()
                .UseLocalhostClustering()
                //.UseLocalhostClustering(gatewayPort: gatewayPort) /* For non-standard port */
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansLoadTest";

                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await _client.Connect();
        }

        public async Task Post(DataClass data)
        {
            var grain = _client.GetGrain<INumberStoreGrain>(data.GrainId);
            await grain.UpdateNumberInfo(data.NumberInfo);
            Console.WriteLine($"Success: {data.GrainId}");
        }


        public async Task<NumberInfo> GetGrainData(int grainId)
        {
            var grain = _client.GetGrain<INumberStoreGrain>(grainId);
            var currentState = await grain.GetState();
            return currentState;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
