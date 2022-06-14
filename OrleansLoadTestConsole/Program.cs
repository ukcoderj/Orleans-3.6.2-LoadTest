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
        Console.WriteLine();

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
    string dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";


    DisplayHelper.WriteLine("Should this console be multi-threaded? N/n = No, anything else = Yes.");
    bool useMultiThreading = Console.ReadKey().Key != ConsoleKey.N;
    Console.WriteLine();


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

    var numberOfGrains = (maxGrainNumber - minGrainNumber) + 1;

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


    #region "Print Current Values (used in manual resiliency testing)"

    DisplayHelper.WriteLine($"Do you want to print the existing values of all grains? Y/y = yes, any other key = No");
    var printVals = Console.ReadKey().Key == ConsoleKey.Y;
    Console.WriteLine();
    if (printVals)
    {
        foreach (var row in numberAndGrainPosts)
        {
            var lastVal = await testCalls.GetGrainData(row.GrainId);
            if (lastVal != null)
            {
                var dtFormatted = lastVal.DateTimeReceived.ToString(dateTimeFormat, CultureInfo.InvariantCulture);
                Console.WriteLine($"{row.GrainId.ToString().PadRight(3)}:{lastVal.Number.ToString().PadRight(3)}: {dtFormatted}");
            }
            else
            {
                Console.WriteLine($"{row.GrainId}: null");
            }
        }
    }

    #endregion


    ConsoleKey consoleKey = ConsoleKey.Y;

    bool hasAlreadyRun = false;
    bool shouldWaitForOtherConsoles = true;
    
    // MAIN TEST LOOP
    while (consoleKey == ConsoleKey.Y)
    {

        DisplayHelper.WriteLine($"Great! Set up for grains {minGrainNumber}-{maxGrainNumber}");


        #region "Warm Up"

        DisplayHelper.WriteLine($"Warming up grains {minGrainNumber}-{maxGrainNumber}...", ConsoleColor.Yellow);
        List<Task> taskList = new List<Task>();

        Stopwatch st = new Stopwatch();
        st.Start();

        // Warm up grains
        if (useMultiThreading)
        {
            foreach (var row in numberAndGrainPosts)
            {
                taskList.Add(testCalls.WarmUp(row.GrainId));
            }
            Task.WaitAll(taskList.ToArray());
        }
        else
        {
            foreach (var row in numberAndGrainPosts)
            {
                testCalls.WarmUp(row.GrainId);
            }
        }

        st.Stop();
        DisplayHelper.WriteLine($"Warm up time - {numberOfGrains} grains = {st.ElapsedMilliseconds}ms", ConsoleColor.Yellow);

        #endregion


        #region "Reset Grains"

        DisplayHelper.WriteLine($"Resetting grains {minGrainNumber}-{maxGrainNumber}...", ConsoleColor.Yellow);
        List<Task> resetTaskList = new List<Task>();

        st.Reset();
        st.Start();


        // Reset data
        if (useMultiThreading)
        {
            foreach (var row in numberAndGrainPosts)
            {
                resetTaskList.Add(testCalls.Reset(row.GrainId));
            }
            Task.WaitAll(resetTaskList.ToArray());
        }
        else
        {
            foreach (var row in numberAndGrainPosts)
            {
                testCalls.Reset(row.GrainId);
            }
        }

        st.Stop();
        DisplayHelper.WriteLine($"Reset time - {numberOfGrains} grains = {st.ElapsedMilliseconds}ms", ConsoleColor.Yellow);

        #endregion



        if (!hasAlreadyRun)
        {
            DisplayHelper.WriteLine("Skip waiting for other consoles? Y = Go now, anything else = wait for 0/30s mark");
            shouldWaitForOtherConsoles = Console.ReadKey().Key != ConsoleKey.Y;
            Console.WriteLine();
        }


        if (shouldWaitForOtherConsoles)
        {
            Console.WriteLine();
            DisplayHelper.WriteLine("Press enter when all consoles are ready to proceed (will wait max 30s)");
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
        }


        // Send the data
        DisplayHelper.WriteLine("Sending data (not logging for speed)...", ConsoleColor.Cyan);
        DateTime startTimeUtc = DateTime.UtcNow;
        st.Reset();
        st.Start();
        // Data starts sending before WaitAll
        if (useMultiThreading)
        {
            foreach (var row in numberAndGrainPosts)
            {
                taskList.Add(testCalls.Post(row));
            }
            Task.WaitAll(taskList.ToArray());
        }
        else
        {
            foreach (var row in numberAndGrainPosts)
            {
                testCalls.Post(row);
            }
        }

        st.Stop();
        msTaken.Add(st.ElapsedMilliseconds);


        DisplayHelper.WriteLine($"Client Task time - {numberOfGrains} grains ={st.ElapsedMilliseconds}ms", ConsoleColor.Yellow);
        var clientRate = (decimal)(numberOfGrains) / (decimal)(st.ElapsedMilliseconds / (decimal)1000);
        var clientRateRounded = decimal.Round(clientRate, 2, MidpointRounding.AwayFromZero);
        DisplayHelper.WriteLine($"Client Task rate = {clientRateRounded}/s", ConsoleColor.Yellow);

        var minGrainNumToCheck = 0;
        var maxGrainNumToCheck = 0;

        if (shouldWaitForOtherConsoles)
        {

            DisplayHelper.WriteLine("Do you want to get an accurate reading for multiple consoles (Y/N)? Dont press enter!");

            if (Console.ReadKey().Key == ConsoleKey.Y)
            {
                while (minGrainNumToCheck == 0 && maxGrainNumToCheck == 0)
                {
                    Console.WriteLine();
                    DisplayHelper.WriteLine("Enter the min grain number and max grain number (e.g. '1-5000')");
                    DisplayHelper.WriteLine("make sure all consoles have finished before doing this!!!");

                    var input = Console.ReadLine() ?? "";

                    if (input == "exit" || input == "e")
                    {
                        return;
                    }

                    if (!input.Contains("-")) continue;

                    var splitVals = input.Split("-");
                    int.TryParse(splitVals[0], out minGrainNumToCheck);
                    int.TryParse(splitVals[1], out maxGrainNumToCheck);
                }


                DisplayHelper.WriteLine("Getting data (not logging as we go to speed things up)....", ConsoleColor.Yellow);
            }

        }

        // Just this console.
        if (minGrainNumToCheck == 0 && maxGrainNumToCheck == 0)
        {
            minGrainNumToCheck = minGrainNumber;
            maxGrainNumToCheck = maxGrainNumber;
        }

        DisplayHelper.WriteLine("Checking back through grains...", ConsoleColor.Yellow);

        NumberInfo minNumberInfo = null;
        NumberInfo maxNumberInfo = null;

        int gCheckTotal = 0;
        int gCheckFails = 0;

        for (int i = minGrainNumToCheck; i <= maxGrainNumToCheck; i++)
        {
            gCheckTotal++;
            var tmp = await testCalls.GetGrainData(i);

            if (tmp == null ||
                tmp.DateTimeReceived == new DateTime() ||
                tmp.DateTimeReceived < startTimeUtc)
            {
                gCheckFails++;
                continue;
            }

            if (minNumberInfo == null && maxNumberInfo == null)
            {
                minNumberInfo = tmp;
                maxNumberInfo = tmp;
            }
            else if (tmp.DateTimeReceived > maxNumberInfo.DateTimeReceived)
            {
                maxNumberInfo = tmp;
            }
            else if (tmp.DateTimeReceived < minNumberInfo.DateTimeReceived)
            {
                minNumberInfo = tmp;
            }
        }

        var minDateString = minNumberInfo.DateTimeReceived.ToString(dateTimeFormat, CultureInfo.InvariantCulture);
        var maxDateString = maxNumberInfo.DateTimeReceived.ToString(dateTimeFormat, CultureInfo.InvariantCulture);

        DisplayHelper.WriteLine($"Min: Grain {minNumberInfo.Number.ToString().PadRight(5)}: Date:{minDateString}");
        DisplayHelper.WriteLine($"Max: Grain {maxNumberInfo.Number.ToString().PadRight(5)}: Date:{maxDateString}");
        DisplayHelper.WriteLine($"Checked: {gCheckTotal} total, ({gCheckFails} fails)");

        TimeSpan spanMinToMax = maxNumberInfo.DateTimeReceived - minNumberInfo.DateTimeReceived;
        var msMinToMax = spanMinToMax.TotalMilliseconds;
        var msMinToMaxNumGrains = maxGrainNumToCheck - minGrainNumToCheck - 1;
        var ctMinToMaxPerSecond = (decimal)(msMinToMaxNumGrains) / ((decimal)msMinToMax / (decimal)1000);
        var ctMinToMaxPerSecondRound = decimal.Round(ctMinToMaxPerSecond, 2, MidpointRounding.AwayFromZero);

        DisplayHelper.WriteLine($"All consoles: Time from grain {minGrainNumToCheck} - {maxGrainNumToCheck} = {spanMinToMax.TotalMilliseconds}ms ({ctMinToMaxPerSecondRound}/s)");


        Console.WriteLine();
        Console.WriteLine();
        DisplayHelper.WriteLine("Press 'y' or 'Y' to redo the test, or any other key to start exiting.");
        consoleKey = Console.ReadKey().Key;
        hasAlreadyRun = true;
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




