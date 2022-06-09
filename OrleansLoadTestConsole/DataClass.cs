using LoadTest.Grains.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansLoadTestConsole
{
    public class DataClass
    {
        public int GrainId { get; set; }

        public string HttpPayloadJson { get; set; }

        public NumberInfo NumberInfo { get; set; }
    }
}
