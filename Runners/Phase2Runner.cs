using System.Diagnostics;
using Mini_Project___Order_Processing_System.Data;
using Mini_Project___Order_Processing_System.Services;

namespace Mini_Project___Order_Processing_System.Runners;

public class Phase2Runner
{
    private readonly IncomingOrderRepository _incoming = new();
    private readonly OrderRepository _orders = new();
    private readonly OrderValidator _validator = new();
    private readonly OrderProcessorV2 _processor = new();
    private readonly ReportGenerator _reportGenerator = new();

    public async Task RunAsync(CancellationToken ct)
    {
        int finished = 0;
        int cancelled = 0;

        try
        {
            // ===== Step 1: reset, race demo, reset, pricing comparison =====
            await DataSeeder.ResetAsync(ct);
            await RaceConditionDemo.RunAsync(ct);

            await DataSeeder.ResetAsync(ct);
            var orders = await _incoming.GetAllAsync(ct);

            await ComparePricingAsync(orders, ct);

            // ===== Step 2: validate all 20 orders at the same time (the validator saves each one) =====
            Console.WriteLine("\n--- Validating all orders ---");
            var validateTasks = new List<Task<Order>>();
            foreach (var order in orders)
            {
                if (ct.IsCancellationRequested) break;
                validateTasks.Add(_validator.ValidateOrderAsync(order, ct));
            }

            for (int i = 0; i < validateTasks.Count; i++)
            {
                try
                {
                    await validateTasks[i];
                    if (orders[i].Status == OrderStatus.Rejected) finished++;
                }
                catch (OperationCanceledException)
                {
                    await MarkCancelledAsync(orders[i]);
                    cancelled++;
                }
            }

            if (ct.IsCancellationRequested)
            {
                // validated orders were not processed yet, so they are cancelled too
                foreach (var order in orders)
                {
                    if (order.Status == OrderStatus.Validated)
                    {
                        await MarkCancelledAsync(order);
                        cancelled++;
                    }
                }
                PrintCancelled(finished, cancelled);
                return;
            }

            // ===== Step 3: start the processing of every Validated order =====
            var validated = orders.Where(o => o.Status == OrderStatus.Validated).ToList();
            Console.WriteLine($"\n--- Processing {validated.Count} validated orders ---");

            var watch = Stopwatch.StartNew();
            var tasks = new List<Task<bool>>();
            foreach (var order in validated)
            {
                if (ct.IsCancellationRequested) break;
                tasks.Add(_processor.ProcessValidatedOrderAsync(order, ct));
            }

            if (tasks.Count > 0)
            {
                Console.WriteLine($"Status of task[0] right after creation: {tasks[0].Status}");
            }

            // ===== Step 4: wait 1 second, print the status of every task =====
            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                // keep going, step 5 handles the cancelled orders
            }

            Console.WriteLine("\nTask statuses after 1 second:");
            for (int i = 0; i < tasks.Count; i++)
            {
                Console.WriteLine($"  Order #{validated[i].Id}: {tasks[i].Status}");
            }

            // ===== Step 5: await each task, one failing order must not stop the others =====
            for (int i = 0; i < tasks.Count; i++)
            {
                var order = validated[i];
                try
                {
                    await tasks[i];
                    finished++;
                }
                catch (OperationCanceledException)
                {
                    await _processor.ReleaseStockAsync(order, CancellationToken.None);
                    await MarkCancelledAsync(order);
                    cancelled++;
                }
                catch (Exception ex)
                {
                    Log.Error("Phase2Runner", $"Order #{order.Id} task status: {tasks[i].Status}, error: {ex.Message}");

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = ex.Message;

                    // cleanup must finish even if the run is cancelled, so no token here
                    await _processor.ReleaseStockAsync(order, CancellationToken.None);
                    await _orders.SaveAsync(order, CancellationToken.None);
                    finished++;
                }
            }
            watch.Stop();

            if (ct.IsCancellationRequested)
            {
                PrintCancelled(finished, cancelled);
                return;
            }

            // ===== Step 6: final statuses and total time =====
            Console.WriteLine("\nFinal task statuses:");
            for (int i = 0; i < tasks.Count; i++)
            {
                Console.WriteLine($"  Order #{validated[i].Id}: {tasks[i].Status}");
            }
            Console.WriteLine($"Total processing time: {watch.Elapsed.TotalSeconds:F1} seconds");

            // ===== Step 7: read orders.json back =====
            var all = await _orders.GetAllAsync(ct);
            Console.WriteLine();
            Console.WriteLine($"orders.json now contains {all.Count} records.");
            Console.WriteLine($"  Completed: {all.Count(o => o.Status == OrderStatus.Completed)}");
            Console.WriteLine($"  Rejected:  {all.Count(o => o.Status == OrderStatus.Rejected)}");
            Console.WriteLine($"  Failed:    {all.Count(o => o.Status == OrderStatus.Failed)}");

            // ===== Report: start the task, print a message while it runs, then await it =====
            Console.WriteLine("\n--- Report ---");
            var reportTask = _reportGenerator.GenerateAsync(ct);
            Console.WriteLine("Generating report...");
            var report = await reportTask;
            PrintReport(report);

            // ===== the .Result demo (Phase 2 question 3) =====
            new BlockingDemo().Run();
        }
        catch (OperationCanceledException)
        {
            PrintCancelled(finished, cancelled);
        }
    }

    private async Task ComparePricingAsync(List<Order> orders, CancellationToken ct)
    {
        var order1 = orders.First(o => o.Id == 1);
        Console.WriteLine("\n--- Pricing comparison on Order #1 (not saved) ---");

        var watch = Stopwatch.StartNew();
        await OrderPricer.PriceOrderSequentialAsync(order1, ct);
        watch.Stop();
        Console.WriteLine($"Sequential: {watch.Elapsed.TotalSeconds:F1} seconds");
        PrintPrice(order1);

        watch.Restart();
        await OrderPricer.PriceOrderAsync(order1, ct);
        watch.Stop();
        Console.WriteLine($"Concurrent: {watch.Elapsed.TotalSeconds:F1} seconds");
        PrintPrice(order1);
    }

    private void PrintPrice(Order order)
    {
        Console.WriteLine($"  SubTotal {order.SubTotal:F2} / Discount {order.Discount:F2} / Shipping {order.ShippingCost:F2} / Total {order.TotalAmount:F2}");
    }

    private void PrintReport(Report report)
    {
        Console.WriteLine();
        Console.WriteLine("===== Report =====");
        Console.WriteLine($"Total orders:      {report.TotalOrders}");
        Console.WriteLine($"Completed:         {report.CompletedCount}");
        Console.WriteLine($"Rejected:          {report.RejectedCount}");
        Console.WriteLine($"Failed:            {report.FailedCount}");
        Console.WriteLine($"Payment failed:    {report.PaymentFailedCount}");
        Console.WriteLine($"Total revenue:     {report.TotalRevenue:F2}");
        Console.WriteLine($"Average order:     {report.AverageOrderValue:F2}");
        Console.WriteLine($"Top selling:       {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
    }

    // a cancelled order goes back to Pending with the reason "Cancelled"
    private async Task MarkCancelledAsync(Order order)
    {
        order.Status = OrderStatus.Pending;
        order.FailureReason = "Cancelled";
        await _orders.SaveAsync(order, CancellationToken.None);
    }

    private void PrintCancelled(int finished, int cancelled)
    {
        Console.WriteLine($"\nRun cancelled — {finished} orders finished, {cancelled} cancelled.");
    }
}
