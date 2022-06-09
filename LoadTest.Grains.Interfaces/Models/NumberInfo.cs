using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTest.Grains.Interfaces.Models
{
    [Immutable] // Immutable for faster serialization
    public class NumberInfo
    {
        public NumberInfo(){}

        public NumberInfo(int number)
        {
            Number = number;
        }

        public NumberInfo(int number, DateTime dt) : this(number)
        {
            DateTimeReceived = dt;
        }

        public int Number { get; set; }
        public DateTime DateTimeReceived { get; set; }
    }
}
