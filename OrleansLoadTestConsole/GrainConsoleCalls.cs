using LoadTest.Grains.Interfaces;
using LoadTest.Grains.Interfaces.Models;
using LoadTest.SharedBase.Helpers;
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
                .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Error))
                .Build();

            await _client.Connect();
        }

        public async Task WarmUp(int grainId)
        {
            try
            {
                var grain = _client.GetGrain<INumberStoreGrain>(grainId);
                await grain.WarmUp();
            }
            catch (Exception ex)
            {
                DisplayHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
            }


        }

        public async Task Reset(int grainId)
        {
            try
            {
                var grain = _client.GetGrain<INumberStoreGrain>(grainId);
                await grain.ResetState();
            }
            catch (Exception ex)
            {
                DisplayHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
            }
        }

        public async Task Post(DataClass data)
        {
            try
            {
                var grain = _client.GetGrain<INumberStoreGrain>(data.GrainId);
                await grain.UpdateNumberInfo(data.NumberInfo);
            }
            catch (Exception ex)
            {
                DisplayHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
            }
        }


        public async Task<NumberInfo> GetGrainData(int grainId)
        {
            NumberInfo retVal = new NumberInfo(-1, new DateTime());
            try
            {
                var grain = _client.GetGrain<INumberStoreGrain>(grainId);
                retVal = await grain.GetState();
            }
            catch (Exception ex)
            {
                DisplayHelper.WriteLine(ex.ToString(), ConsoleColor.Red);
            }
            return retVal;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
