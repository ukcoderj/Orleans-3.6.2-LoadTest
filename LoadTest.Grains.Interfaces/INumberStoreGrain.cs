using LoadTest.Grains.Interfaces.Models;
using Orleans.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTest.Grains.Interfaces
{
    [Version(1)]
    public interface INumberStoreGrain : Orleans.IGrainWithIntegerKey
    {
        Task<NumberInfo> GetState();

        Task UpdateNumberInfo(NumberInfo input);
    }
}
