using System.Diagnostics;
using Mini_Project___Order_Processing_System.Data;
using Mini_Project___Order_Processing_System.Runners;
using Mini_Project___Order_Processing_System.Services;

namespace Mini_Project___Order_Processing_System;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await RunTask8_3();
    }
    public static async Task RunTask8_2()
    {
        var token = new CancellationToken();
        JsonFileStore store = new JsonFileStore();
        var orders = await store.ReadAllAsync<Order>(DataFiles.Orders, token);

        var watch = Stopwatch.StartNew();
        foreach (var order in orders)
        {
            try
            {
                await OrderPricer.PriceOrderAsync(order, token);
            }
            catch (Exception ex)
            {
            }

        }
        watch.Stop();
        double asyncSeconds = watch.Elapsed.TotalSeconds;
        watch.Restart();

        foreach (var order in orders)
        {
            try
            {
                await OrderPricer.PriceOrderSequentialAsync(order, token);
            }
            catch (Exception ex)
            {
            }

        }
        watch.Stop();
        double sequentialSeconds = watch.Elapsed.TotalSeconds;
        Console.WriteLine($"Async = {asyncSeconds} , seq = {sequentialSeconds}");
    }
    public static async Task RunTask8_3()
    {
        var token = new CancellationToken();

        await RaceConditionDemo.RunAsync(token);
    }
}

