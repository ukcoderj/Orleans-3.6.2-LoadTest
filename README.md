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

4. In the Azure Portal (or PowerShell), create at least 1 Storage Account for saving grain data. For each one, note the storage account name, generate a SAS token and copy the info into the settings. The table name will be auto generated when you run the silo. 

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

Azure VM - Standard DS3 v2 (4 vcpus, 14 GiB memory)

  1. Azure - - Trial 5 silos (with a storage account for each) and 2 client consoles - client task rate approx 2k/s - 5315/s overall. Re-run= client 6.4K/s, Overall 16,145/s.

  2. 10 Silos (each with a storage account) - 2 client consoles - client task rate 633/s. Re-run = client x, Overall 6-22K/s. CPU maxing out at 100%.

  3. 4 client consoles, 10 Silos (each with a storage account)
  - (CPU maxing out at 100%, no memory issues)
  Warm up rate 88,000 requests/s
   First run 8802/s. 
   Second Run 12,419/s

   4. 1 Client console, 10 Silos (each with a storage account) 
  - (CPU maxing out at 100%, no memory issues)
   Warm up rate 58,823/s
   First run 3065/s  (client running at 1.2K/s)
   Second Run 16,159/s (client running at 14K/s) - warm up rate 102K
   Third Run 4,344/s

5. Tried removing 5 silos- many errors bought system to stand still with some errors. Warm up time rate just 300/s.
   System then recovered to 1.7K/s, then on the next run to 12K/s, then 6.8K/s with warm up rate 90K/s.

  
  Azure F16s v2

  10 silos
  1 client console
  Run 1: Warm up 5784ms (1.7K/s). Data 7.4k/s
  Run 2: Warm up 89K/s. Data 4.6K/s
  Run 3: Data 5K/s

  5 silos
  1 client console
  Run 1: Warm up - very slow 2K/s. Data 15K/s
  Run 2: Warm up 72K/s. Data 7.5K/s
  Run 3: Warm up 113K/s. Data 7.5K/s
  Run 4: Warm up 66K/s Data 7K/s


  5 silos 100K in (1 console)
  Run 1: Warm up 3k/s. Run failed.
  Run 2: 66K/s. Data 5K/s
  Run 3 Data 4K/s
  Run 4: Data 22K/s
  Run 5: Data 16K/s
  Run 6: Data 18K/s

  
  D8s v5 - 5 Silos, 1 Console

  Re running tests with reset first to be 100% sure of no missed updates.
  100K Test
  Warmup & reset - several fails. Data 7K/s - 11 fails
  Next run Data 7K/s - 0 fails

 20K Test
  Completely fresh (all previous data deleted 20K test)
  Run 1: Warm up 2K/s. Reset 11.2K/s. Data 4K/s (no fails) - CPU
  Run 2: Warm up 93K/s. Reset 0.9K/s. Data 11.5K/s (no fails)


  3 Silos - Data consistently 13.5K/s (warm up max'd at 121K/s)



WARNING: The strategy of using 1 storage per silo causes problems with data loss if that silo goes down! Better storage strategy required.



  ## Learning References

  1. [Pluralsight - Introduction To Microsoft Orleans](https://app.pluralsight.com/library/courses/microsoft-orleans-introduction/table-of-contents)
  2. [Distributed .NET with Microsoft Orleans (Packt)](https://www.packtpub.com/product/distributed-net-with-microsoft-orleans/9781801818971)
  3. [The docs - good in parts, but needed the other resources](https://dotnet.github.io/orleans/docs/index.html)