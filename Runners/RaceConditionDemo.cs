using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Runners;

public class RaceConditionDemo
{
    private static JsonFileStore _store = new JsonFileStore();
    private static ProductRepository products = new ProductRepository();

    public async static Task RunAsync(CancellationToken ct)
    {
        // unsafe version
        Console.WriteLine("===== Unsafe =====");
        await SetKeyboardStockAsync(ct);

        Task<bool> order10 = products.ReserveStockUnsafeAsync(4, 1, ct);
        Task<bool> order11 = products.ReserveStockUnsafeAsync(4, 1, ct);

        // then await them one by one
        bool result10 = await order10;
        bool result11 = await order11;
        await PrintResultAsync(result10, result11, ct);

        // safe version
        Console.WriteLine("===== Safe =====");
        await SetKeyboardStockAsync(ct);

        order10 = products.ReserveStockAsync(4, 1, ct);
        order11 = products.ReserveStockAsync(4, 1, ct);

        result10 = await order10;
        result11 = await order11;
        await PrintResultAsync(result10, result11, ct);
    }

    private static async Task SetKeyboardStockAsync(CancellationToken ct)
    {
        await _store.UpdateAsync<Product>(DataFiles.Products, list =>
        {
            list.First(p => p.Id == 4).StockQuantity = 1;
        }, ct);
    }

    private static async Task PrintResultAsync(bool result10, bool result11, CancellationToken ct)
    {
        var all = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
        int stock = all.First(p => p.Id == 4).StockQuantity;

        Console.WriteLine($"Order #10 got the keyboard: {result10}");
        Console.WriteLine($"Order #11 got the keyboard: {result11}");
        Console.WriteLine($"Keyboard stock in products.json: {stock}");
    }
}
