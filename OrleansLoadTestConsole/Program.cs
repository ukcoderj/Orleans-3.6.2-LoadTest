// See https://aka.ms/new-console-template for more information
using LoadTest.Grains.Interfaces.Models;
using LoadTest.SharedBase.Helpers;
using LoadTest.SharedBase.Models;
using Newtonsoft.Json;
using OrleansLoadTestConsole;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;



Console.WriteLine("Run load test");


try
{
    ITestCalls testCalls = null;
    ConsoleKey key = ConsoleKey.Escape;

    while (key != ConsoleKey.W && key != ConsoleKey.C && key != ConsoleKey.S)
    {
        DisplayHelper.WriteLine("Press 'c' or 's' to target the console silo, or 'w' to target the website");
        key = Console.ReadKey().Key;

        if (key == ConsoleKey.W)
        {
            testCalls = new HttpCalls("https://localhost:44326/");
        }
        else if (key == ConsoleKey.C || key == ConsoleKey.S)
        {
            var grainCalls = new GrainConsoleCalls();
            await grainCalls.Init();
            Thread.Sleep(2000);
            testCalls = grainCalls;
        }

    }




    List<long> msTaken = new List<long>();

    int minGrainNumber = 0;
    int maxGrainNumber = 0;

    while (minGrainNumber == 0 && maxGrainNumber == 0)
    {
        DisplayHelper.WriteLine("Remember the first run will be a little slower due to grain activation.", ConsoleColor.Yellow);

        DisplayHelper.WriteLine("Enter the start and end grain numbers", ConsoleColor.Cyan);
        Console.WriteLine("e.g. '1-999' will start at grain 1 and finish at grain 999 (inclusive)", ConsoleColor.Cyan);
        Console.WriteLine("e.g. '1000-1999' will start at grain 1000 and finish at grain 1999 (inclusive)", ConsoleColor.Cyan);

        var input = Console.ReadLine() ?? "";

        if (input == "exit" || input == "e")
        {
            return;
        }

        if (!input.Contains("-")) continue;

        var splitVals = input.Split("-");
        int.TryParse(splitVals[0], out minGrainNumber);
        int.TryParse(splitVals[1], out maxGrainNumber);
    }

    Console.WriteLine("Setting up the information for sending now to save a few milliseconds");

    List<DataClass> numberAndGrainPosts = new List<DataClass>();
    for (int i = minGrainNumber; i <= maxGrainNumber; i++)
    {
        DataClass d = new DataClass();
        var content = new NumberAndGrainPost()
        {
            GrainId = i,
            Number = i /* Doesn't really matter! */
        };

        d.GrainId = i;
        d.NumberInfo = new NumberInfo(i);
        d.HttpPayloadJson = System.Text.Json.JsonSerializer.Serialize(content);
        numberAndGrainPosts.Add(d);
    }

    ConsoleKey consoleKey = ConsoleKey.Y;

    while(consoleKey == ConsoleKey.Y)
    {

        DisplayHelper.WriteLine($"Great! Set up for grains {minGrainNumber}-{maxGrainNumber}");


        DisplayHelper.WriteLine($"Press 'Enter' when all consoles are ready to push data", ConsoleColor.White);

        Console.ReadLine();


        DisplayHelper.WriteLine("Wait for the 0 or 30s mark to start", ConsoleColor.Yellow);


        while (1 == 1)
        {
            var nowSeconds = DateTime.Now.Second;

            if (nowSeconds == 0 ||
                nowSeconds == 1 ||
                nowSeconds == 30 ||
                nowSeconds == 31)
            {
                break;
            }

            Thread.Sleep(10);
        }




        // Send the data
        foreach (var row in numberAndGrainPosts)
        {
            Stopwatch st = new Stopwatch();
            st.Start();

            // TODO: Consider not awaiting for speed? Maybe even kick off a new thread? Task.Run()
            await testCalls.Post(row);

            st.Stop();
            msTaken.Add(st.ElapsedMilliseconds);
        }


        DisplayHelper.WriteLine($"Console Total Time: {msTaken.Sum()}ms, Average Time: {msTaken.Average()}ms");


        Console.WriteLine("Wait 30s to get grain data back");

        Thread.Sleep(30000);


        var grain1Info = await testCalls.GetGrainData(1);
        var minGrainInfo = await testCalls.GetGrainData(minGrainNumber);
        var maxGrainInfo = await testCalls.GetGrainData(maxGrainNumber);


        TimeSpan spanMinToMax = maxGrainInfo.DateTimeReceived - minGrainInfo.DateTimeReceived;
        var msMinToMax = spanMinToMax.TotalMilliseconds;
        var ctMinToMaxPerSecond = ((maxGrainNumber - minGrainNumber) + 1) / ((decimal)msMinToMax / 1000);
        var ctMinToMaxPerSecondRound = decimal.Round(ctMinToMaxPerSecond, 2, MidpointRounding.AwayFromZero);
        DisplayHelper.WriteLine($"This console: Time from grain {minGrainNumber} - {maxGrainNumber} = {msMinToMax}ms ({ctMinToMaxPerSecondRound}/s)");



        TimeSpan span1ToMax = maxGrainInfo.DateTimeReceived - grain1Info.DateTimeReceived;
        var ms1ToMax = span1ToMax.TotalMilliseconds;
        var ct1ToMaxPerSecond = (maxGrainNumber) / ((decimal)ms1ToMax / 1000);
        var ct1ToMaxPerSecondRound = decimal.Round(ct1ToMaxPerSecond, 2, MidpointRounding.AwayFromZero);
        DisplayHelper.WriteLine("The next value is only useful for the final console....");
        var grain1DateString = grain1Info.DateTimeReceived.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        DisplayHelper.WriteLine($"Grain 1: {grain1DateString}");
        var grainMaxDateString = maxGrainInfo.DateTimeReceived.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        DisplayHelper.WriteLine($"Grain {maxGrainNumber}: {grainMaxDateString}");
        DisplayHelper.WriteLine($"All consoles [provided all finished in time]: Time from grain 1 - {maxGrainNumber} = {span1ToMax.Milliseconds}ms ({ct1ToMaxPerSecondRound}/s)");

        Console.WriteLine();
        Console.WriteLine();
        DisplayHelper.WriteLine("Press 'y' or 'Y' to redo the test, or any other key to start exiting.");
        consoleKey = Console.ReadKey().Key;

    }


}
catch (Exception ex)
{
    DisplayHelper.WriteLine("ERROR:" + ex.ToString(), ConsoleColor.Red);
}




Console.WriteLine();
Console.WriteLine();
Console.WriteLine("Press enter a few times to exit");

Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
Console.ReadLine();




