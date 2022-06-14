using LoadTest.Grains.Interfaces;
using LoadTest.Grains.Interfaces.Models;
using LoadTest.SharedBase.Helpers;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTest.Grains
{
    [StorageProvider(ProviderName = "LoadTestNumbersTableStorage1")]
    public class NumberStoreGrain : Orleans.Grain, INumberStoreGrain
    {
        private readonly ILogger logger;
        private readonly IPersistentState<NumberInfo> _state;

        public NumberStoreGrain(ILogger<NumberStoreGrain> logger,
            [PersistentState("numberinfo", "LoadTestNumbersTableStorage1")] IPersistentState<NumberInfo> state)
        {
            this.logger = logger;
            this._state = state;
        }

        public override Task OnActivateAsync()
        {
            var id = this.GrainReference;
            var dt = this._state.State.DateTimeReceived;

            if (dt != new DateTime())
            {
                Console.WriteLine($"GrainRef:{id}, val:{this._state.State.Number} ({this._state.State.DateTimeReceived.ToString("U")}).");
            }
            else
            {
                Console.WriteLine($"GrainRef:{id}, is brand new.");
            }

            return base.OnActivateAsync();
        }

        public Task WarmUp()
        {
            return Task.CompletedTask;
        }

        public async Task ResetState()
        {
            var inputUpdated = new NumberInfo(0, new DateTime());
            this._state.State = inputUpdated;
            await this._state.WriteStateAsync();
        }

        public Task<NumberInfo> GetState() => Task.FromResult(this._state.State);

        public async Task UpdateNumberInfo(NumberInfo input)
        {
            //Stopwatch st = new Stopwatch();
            //st.Start();

            var inputUpdated = new NumberInfo(input.Number, DateTime.UtcNow);
            this._state.State = inputUpdated;
            await this._state.WriteStateAsync();

            //st.Stop();
            //Console.WriteLine($"Save: {this.GrainReference}: {st.ElapsedMilliseconds}ms");
        }
    }
}
