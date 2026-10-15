using Mini_Project___Order_Processing_System.Runners;

namespace Mini_Project___Order_Processing_System;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // one cancellation source for the whole run
        using var cts = new CancellationTokenSource();

        // Ctrl+C cancels the run instead of killing the program
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // --cancel-after <ms>
        int index = Array.IndexOf(args, "--cancel-after");
        if (index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int ms))
        {
            cts.CancelAfter(ms);
        }

        string phase = "phase2";
        if (args.Length > 0 && !args[0].StartsWith("--")) phase = args[0];

        if (phase == "phase1")
        {
            await new Phase1Runner().RunAsync(cts.Token);
        }
        else if (phase == "phase2")
        {
            await new Phase2Runner().RunAsync(cts.Token);
        }
        else
        {
            Console.WriteLine("Unknown phase: " + phase);
        }
    }
}
