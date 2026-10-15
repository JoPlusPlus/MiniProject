using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Services;

public class InvoiceGenerator
{
    private const string Name = "InvoiceGenerator";

    public async Task GenerateAsync(Order order, CancellationToken ct)
    {
        Log.Info(Name, $"Rendering invoice for Order #{order.Id}...");

        // CPU work (no waiting), so we run it on a thread pool thread with Task.Run
        await Task.Run(() => RenderInvoice(ct), ct);

        Log.Info(Name, $"Writing invoice file for Order #{order.Id}...");

        var lines = new List<string>();
        lines.Add("INVOICE for Order #" + order.Id);
        lines.Add("Customer: #" + order.CustomerId);
        lines.Add("");
        foreach (var item in order.Items)
        {
            lines.Add("Product #" + item.ProductId + " x " + item.Quantity + " @ " + item.UnitPrice);
        }
        lines.Add("");
        lines.Add("SubTotal:  " + order.SubTotal);
        lines.Add("Discount:  " + order.Discount);
        lines.Add("Shipping:  " + order.ShippingCost);
        lines.Add("Total:     " + order.TotalAmount);

        Directory.CreateDirectory(DataFiles.InvoicesFolder);
        string path = Path.Combine(DataFiles.InvoicesFolder, $"invoice_{order.Id}.txt");
        await File.WriteAllLinesAsync(path, lines, ct);

        Log.Success(Name, $"Invoice for Order #{order.Id} saved");
    }

    // pretend to render a PDF
    private long RenderInvoice(CancellationToken ct)
    {
        long sum = 0;
        for (int i = 0; i < 20_000_000; i++)
        {
            sum += i % 7;

            // check for cancel from time to time
            if (i % 1_000_000 == 0) ct.ThrowIfCancellationRequested();
        }
        return sum;
    }
}
