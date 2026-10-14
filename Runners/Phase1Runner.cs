using System.Diagnostics;
using Mini_Project___Order_Processing_System.Data;
using Mini_Project___Order_Processing_System.Repositories;

namespace Mini_Project___Order_Processing_System;

public class Phase1Runner
{
    private readonly IncomingOrderRepository _incoming = new();
    private readonly OrderRepository _orders = new();
    private readonly OrderValidator _asyncValidator = new();
    private readonly SyncOrderValidator _syncValidator = new();

    public async Task RunAsync(CancellationToken ct)
    {
        int finished = 0;
        int cancelled = 0;

        try
        {
            await DataSeeder.ResetAsync(ct);
            var orders = await _incoming.GetAllAsync(ct);

            Console.WriteLine("\n--- Sync run ---");
            var watch = Stopwatch.StartNew();
            foreach (var order in orders)
            {
                ct.ThrowIfCancellationRequested();
                _syncValidator.ValidateOrder(order);
            }
            watch.Stop();
            double syncSeconds = watch.Elapsed.TotalSeconds;

            await DataSeeder.ResetAsync(ct);
            orders = await _incoming.GetAllAsync(ct);

            Console.WriteLine("\n--- Async run ---");
            watch.Restart();
            foreach (var order in orders)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await _asyncValidator.ValidateOrderAsync(order, ct);
                    finished++;
                }
                catch (OperationCanceledException)
                {
                    order.Status = OrderStatus.Pending;
                    order.FailureReason = "Cancelled";
                    await _orders.SaveAsync(order, CancellationToken.None);
                    cancelled++;
                    break;
                }
            }
            watch.Stop();

            if (ct.IsCancellationRequested)
            {
                Console.WriteLine($"\nRun cancelled — {finished} orders finished, {cancelled} cancelled.");
                return;
            }

            await PrintSummaryAsync(syncSeconds, watch.Elapsed.TotalSeconds, ct);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"\nRun cancelled — {finished} orders finished, {cancelled} cancelled.");
        }
    }

    private async Task PrintSummaryAsync(double syncSeconds, double asyncSeconds, CancellationToken ct)
    {
        var all = await _orders.GetAllAsync(ct);
        var validated = await _orders.GetByStatusAsync(OrderStatus.Validated, ct);
        var rejected = await _orders.GetByStatusAsync(OrderStatus.Rejected, ct);

        Console.WriteLine();
        Console.WriteLine("===== Validation Summary =====");
        Console.WriteLine($"Total Orders: {all.Count}");
        Console.WriteLine($"Validated:    {validated.Count}");
        Console.WriteLine($"Rejected:     {rejected.Count}");
        foreach (var order in rejected.OrderBy(o => o.Id))
        {
            string id = $"#{order.Id}:".PadRight(5);
            Console.WriteLine($"  - Order {id}{order.FailureReason}");
        }
        Console.WriteLine($"Sync Time:  {syncSeconds:F1} seconds");
        Console.WriteLine($"Async Time: {asyncSeconds:F1} seconds");
        Console.WriteLine($"orders.json now contains {all.Count} records.");
    }
}
