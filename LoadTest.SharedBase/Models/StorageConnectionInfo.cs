using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadTest.SharedBase.Models
{
    public class StorageConnectionInfo
    {
        public StorageConnectionInfo(string url, string sasToken)
        {
            StorageUri = new Uri(url);
            SasCredential = new AzureSasCredential(sasToken);
        }

        public Uri StorageUri { get; set; }
        public AzureSasCredential SasCredential { get; set; }
    }
}
