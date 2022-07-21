# Orleans Load / Performance Test

A simple orleans cluster for performance testing, with test console.

## Project Parts

The main parts for this project are

1. `LoadTest.Silo.Console` - includes a setup for a clustered silo. 

- The format of the information required to run the silo can be found in `example-secrets.json`. You can put this info in `appsettings.json` or `secrets.json`.

- When running, the silo will load references to table storage for both the cluster and silo data

- In the event there are more silos than storage accounts, the storage accounts will be shared as evenly as possible (see warning!).

2. `OrleansLoadTestConsole` can be used to point to the cluster and send in information.

3. `OrleansLoadTest` is a Web API using Orleans, but this has not been used for deployed testing to reduce latency of calling an API. If you use this, you will need to add your own application insights key. 
You will also need to copy the content of `example-secrets.json` to the secrets file and adjust accordingly.

## WARNING

If you choose the 'distributing multiple storage accounts between Silos, be aware that this is not done properly. Its good enough for this load test, but it's not resilient. 

You should use a 'custom persistence provider' to shard properly and ensure data will load back nicely in the event of silo failures. See my other repo -> [Sharded Orleans Table Storage](https://github.com/JsAndDotNet/OrleansShardedTableStorage) for an example of this.

## What does the project do?

This project creates an Orleans cluster. The cluster has persistent grains that contain only a random number and the time it was received (min/max used later to determine the length of the entire run and hence the overall requests per second).

The project also creates a test console for sending such data to the cluster and various timings

## To run the project

1. Open in Visual Studio (2022)
2. In `LoadTest.Silo.Console`, Fill in `appsettings.json` or `secrets.json` from `example-secrets.json`. Don't copy over the comments!

3. In the Azure Portal (or PowerShell), create a Storage Account for the cluster. In that, add a table called `OrleansSiloInstances`. Generate + copy a SAS token and note the storage account name for putting into the settings.

4. In the Azure Portal (or PowerShell), create at least 1 Storage Account for saving grain data. For each one, note the storage account name, generate a SAS token and copy the info into the settings. The table name will be auto generated when you run the silo. 

5. Run one or more instances of `LoadTest.Silo.Console`. By entering a number into the console, the Silo will be set up on a unique port. (For the first console, enter '1', the second '2' and so on)

6. On the same machine, run one or more instances of `OrleansLoadTestConsole`. 

- Choose 'c' to connect to the console client (rather than a web client)
- Enter the starting grain number and the end grain number (e.g. '1-500')
- The console will wait for a 0 or 30 second mark to send the data to ensure everything is sent in one big hit. 
- It will then create grains of those numbers.


## Notes

I'd started running 10 Silo's and 10 consoles, but this was not working well. 

After discussing with the Microsoft Team, I learned that one cosole using Tasks will perform better than running multiple consoles. There is also a balance between the number of Silo's and storage accounts as too many silo's per storage account can lead to timeouts.

## Results so far.

Run on clean Azure D8s v5 Windows 2022 VM

For best results, see the final section.

## Single Storage Tests

NOTES: 
- Saving is one of the key bottlenecks in an actor system. See Sharded Storage results below for a better impression.
- CPU max'd out at 100% (serialization?)

3 Silos
- 20K 3 silos 1 storage - 9.6k/s, but client sees it as 4-5k/s - Info is getting in fast, but storage is holding up returning data
- 100K crashes

5 Silos
- 100K - Crashes - Storage Busy
- 20K - 14k/s, but client sees it as 2.7k/s - Storage is holding up returning data

- No save calls - Around 100k/s


## Split/ Sharded Storage Tests (one storage account per silo)

NOTES: 
- CPU max'd out at 100% (serialization?)
- Working this way cannot handle silo's going down (data loss). Should use a 'custom persistence provider' to shard info.

5 Silos
Warm up (first time) = 2K-6k/s - seems standard for first time instantiation (often nearer 2).

-20k - First run Client sees 5.4K, Server sees 7k/s
-20k - Subsequently client sees 15-18k/s, Server sees 15-22k/s (i.e. storage isn't holiding up return)

-50K - Client sees 16-19k/s, Server sees 18-21k/s

-100K - First run, Client sees 13k/s, Server sees 14k/s

-100K - Subsequently, Client sees 16k/s, Server sees 18k/s

-100K - Subsequently, Client sees 9k/s, Server sees 20k/s - 1 fail! (storage timeout which crashed a silo)

## Properly Sharded storage using [OrleansShardedStorage](https://github.com/JsAndDotNet/OrleansShardedTableStorage)

-100K Sharded with 10 storage accounts 
6k/s warm up intially, then 92k/s
Save rates - client sees 23.5k/s, Server sees 23.5k (0 fails)

-No Save Calls approx 100-120k/s




  ## Learning References

  1. [Pluralsight - Introduction To Microsoft Orleans](https://app.pluralsight.com/library/courses/microsoft-orleans-introduction/table-of-contents)
  2. [Distributed .NET with Microsoft Orleans (Packt)](https://www.packtpub.com/product/distributed-net-with-microsoft-orleans/9781801818971)
  3. [The docs - good in parts, but needed the other resources](https://dotnet.github.io/orleans/docs/index.html)
