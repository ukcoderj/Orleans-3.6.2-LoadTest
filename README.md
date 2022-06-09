# Orleans Load / Performance Test

A simple orleans cluster for performance testing, with test console.

## Project Parts

The main parts for this project are

1. `LoadTest.Silo.Console` - includes a setup for a clustered silo. 

- The format of the information required to run the silo can be found in `example-secrets.json`. You can put this info in `appsettings.json` or `secrets.json`.

- When running, the silo will load references to table storage for both the cluster and silo data

- In the event there are more silos than storage accounts, the storage accounts will be shared as evenly as possible.

2. `OrleansLoadTestConsole` can be used to point to the cluster and send in information.

3. `OrleansLoadTest` is a Web API using Orleans, but this has not been used for deployed testing to reduce latency of calling an API. If you use this, you will need to add your own application insights key.

## What does the project do?

This project creates an Orleans cluster. The cluster has persistent grains that contain only a random number and the time it was received (min/max used later to determine the length of the entire run and hence the overall requests per second).

The project also creates a test console for sending such data to the cluster and various timings

## To run the project

1. Open in Visual Studio (2022)
2. In `LoadTest.Silo.Console`, Fill in `appsettings.json` or `secrets.json` from `example-secrets.json`. Don't copy over the comments!

3. In the Azure Portal (or PowerShell), create a Storage Account for the cluster. In that, add a table called `OrleansSiloInstances`. Generate + copy a SAS token and note the storage account name for putting into the settings.

4. In the Azure Portal (or PowerShell), create at least 1 Storage Account for saving grain data. For each one, note the storage account name, generate a SAS token and copy the info into the settings. 

5. Run one or more instances of `LoadTest.Silo.Console`. By entering a number into the console, the Silo will be set up on a unique port. (For the first console, enter '1', the second '2' and so on)

6. On the same machine, run one or more instances of `OrleansLoadTestConsole`. 

- Choose 'c' to connect to the console client (rather than a web client)
- Enter the starting grain number and the end grain number (e.g. '1-500')
- The console will wait for a 0 or 30 second mark to send the data to ensure everything is sent in one big hit. 
- It will then create grains of those numbers.


## Notes

I've been running 10 Silo's with 10 Storage accounts connected (and 1 cluster storage account).

The 10 client consoles have been inputting 1-499, 500-999, 1000-1499 ... up to 5,000.

## Results so far.

Tests (5000 requests total)

Local Tests
1. Local - 1 storage account  - Trial 5 silos and 5 consoles - 137/s (new grains 32ms repsonse). Re-run= 235/s (grains ready, ave response 20ms)
2. Local - 2 storage accounts - Trial 6 silos and 5 consoles -  136/s (new grains 32ms repsonse). Re-run= 256/s (grains ready, ave response 19ms)

Azure VM - Standard DS3 v2 (4 vcpus, 14 GiB memory)
  1. Azure - 1 storage account   - Trial 1 silo and 1 console - 58/s (new grains 11ms response). Re-run= 154/s (grains ready, 6m average response)
  2. Azure - 2 storage accounts  - Trial 5 silos and 5 consoles - 271/s (new grains 18ms repsonse). Re-run= 317/s (grains ready, ave response 15ms) - save time alone 4-5ms
  3. Azure - 10 storage accounts - Trial 10 silos and 10 consoles 316/s (new grains 31ms response). Re-run= 500/s (grains ready, ave response 20ms) - save time 4-10ms. Subsequent runs got up to 700req/s.



  ## Learning References

  1. [Pluralsight - Introduction To Microsoft Orleans](https://app.pluralsight.com/library/courses/microsoft-orleans-introduction/table-of-contents)
  2. [Distributed .NET with Microsoft Orleans (Packt)](https://www.packtpub.com/product/distributed-net-with-microsoft-orleans/9781801818971)
  3. [The docs - good in parts, but needed the other resources](https://dotnet.github.io/orleans/docs/index.html)