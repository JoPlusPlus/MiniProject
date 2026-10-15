using System.Text.Json;
using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Services;

public class ReportGenerator
{
    private const string Name = "ReportGenerator";
    private readonly OrderRepository _orders = new();
    private readonly ProductRepository _products = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Report> GenerateAsync(CancellationToken ct)
    {
        Log.Info(Name, "Building the report...");

        var orders = await _orders.GetAllAsync(ct);
        var products = await _products.GetAllAsync(ct);

        var report = new Report();
        report.TotalOrders = orders.Count;
        report.CompletedCount = orders.Count(o => o.Status == OrderStatus.Completed);
        report.RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected);
        report.FailedCount = orders.Count(o => o.Status == OrderStatus.Failed);
        report.PaymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed);

        // revenue from Completed orders only
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        report.TotalRevenue = completed.Sum(o => o.TotalAmount);
        if (completed.Count > 0)
        {
            report.AverageOrderValue = Math.Round(report.TotalRevenue / completed.Count, 2, MidpointRounding.AwayFromZero);
        }

        // how many pieces of each product were sold
        var sold = new Dictionary<int, int>();
        foreach (var order in completed)
        {
            foreach (var item in order.Items)
            {
                if (sold.ContainsKey(item.ProductId) == false) sold[item.ProductId] = 0;
                sold[item.ProductId] += item.Quantity;
            }
        }

        // highest quantity wins, a tie goes to the lowest product id
        int bestId = 0;
        int bestQuantity = 0;
        foreach (var pair in sold)
        {
            if (pair.Value > bestQuantity || (pair.Value == bestQuantity && pair.Key < bestId))
            {
                bestId = pair.Key;
                bestQuantity = pair.Value;
            }
        }

        var bestProduct = products.FirstOrDefault(p => p.Id == bestId);
        if (bestProduct != null) report.TopSellingProduct = bestProduct.Name;
        report.TopSellingQuantity = bestQuantity;
        report.GeneratedAt = DateTime.Now;

        // save the report file
        string fileName = $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(DataFiles.ReportsFolder, fileName);

        await Simulate.DbWriteAsync(Name, $"Saving {fileName}", async token =>
        {
            Directory.CreateDirectory(DataFiles.ReportsFolder);
            string json = JsonSerializer.Serialize(report, Options);
            await File.WriteAllTextAsync(path, json, token);
        }, ct);

        Log.Success(Name, $"Report saved: {fileName}");
        return report;
    }
}
