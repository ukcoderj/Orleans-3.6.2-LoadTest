using LoadTest.Grains.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansLoadTestConsole
{
    public interface ITestCalls
    {
        Task WarmUp(int grainId);
        Task Reset(int grainId);
        Task Post(DataClass data);
        Task<NumberInfo> GetGrainData(int grainId);
    }
}
