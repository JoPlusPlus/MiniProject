using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Repositories;

public class SyncProductRepository
{
    private const string Name = "SyncProductRepository";
    private readonly JsonFileStore _store = new();

    public Product? GetById(int id)
    {
        return Simulate.DbQuery<Product?>(Name, $"Product #{id}", () =>
        {
            var products = _store.ReadAll<Product>(DataFiles.Products);
            var product = products.FirstOrDefault(p => p.Id == id);

            if (product == null) Log.Error(Name, $"Product #{id} not found");
            else Log.Success(Name, $"Product #{id} found ({product.Name})");

            return product;
        });
    }
}
