using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class ProductRepository
{
    private const string Name = "ProductRepository";
    private readonly JsonFileStore _store = new();

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct)
    {
        return Simulate.DbQueryAsync<Product?>(Name, $"Product #{id}", async token =>
        {
            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, token);
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product == null) Log.Error(Name, $"Product #{id} not found");
            else Log.Success(Name, $"Product #{id} found ({product.Name})");

            return product;
        }, ct);
    }

    public Task<List<Product>> GetAllAsync(CancellationToken ct)
    {
        return Simulate.DbQueryAsync<List<Product>>(Name, "all products", async token =>
        {
            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, token);
            Log.Success(Name, $"Found {products.Count} product(s)");
            return products;
        }, ct);
    }

    public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
    {
        bool reserved = false;

        await Simulate.DbWriteAsync(Name, $"Reserving {qty} x Product #{productId}", async token =>
        {
            await _store.UpdateAsync<Product>(DataFiles.Products, list =>
            {
                var product = list.FirstOrDefault(p => p.Id == productId);
                if (product != null && product.StockQuantity >= qty)
                {
                    product.StockQuantity -= qty;
                    reserved = true;
                }
            }, token);
        }, ct);

        if (reserved) Log.Success(Name, $"Reserved {qty} x Product #{productId}");
        else Log.Error(Name, $"Could not reserve Product #{productId}");

        return reserved;
    }
    public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken ct)
    {
        // read the stock
        var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
        var product = products.FirstOrDefault(p => p.Id == productId);
        if (product == null || product.StockQuantity < qty)
        {
            Log.Error(Name, $"Not enough stock for Product #{productId}");
            return false;
        }
        Log.Info(Name, $"Product #{productId} stock is {product.StockQuantity}");

        // wait
        Log.Info(Name, "Opening connection...");
        await Task.Delay(200, ct);
        Log.Info(Name, "Beginning transaction...");
        await Task.Delay(100, ct);
        Log.Info(Name, $"Updating stock of Product #{productId}...");
        await Task.Delay(400, ct);

        // write the new stock
        product.StockQuantity -= qty;
        await _store.WriteAllAsync(DataFiles.Products, products, ct);

        Log.Success(Name, $"Reserved {qty} x Product #{productId}");
        return true;
    }



    public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct)
    {
        Log.Info(Name, "Opening connection...");
        await Task.Delay(200, ct);
        Log.Info(Name, "Beginning transaction...");
        await Task.Delay(100, ct);
        Log.Info(Name, $"Releasing {qty} x Product #{productId}...");
        await Task.Delay(400, ct);

        await _store.UpdateAsync<Product>(DataFiles.Products, list =>
        {
            var p = list.FirstOrDefault(x => x.Id == productId);
            if (p != null) p.StockQuantity += qty;
        }, ct);

        Log.Info(Name, "Committing transaction...");
        await Task.Delay(200, ct);

        Log.Success(Name, $"Released {qty} x Product #{productId}");
    }
}
