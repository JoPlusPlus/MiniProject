using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class IncomingOrderRepository
{
    private const string Name = "IncomingOrderRepository";
    private readonly JsonFileStore _store = new();

    public Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        return Simulate.DbQueryAsync<List<Order>>(Name, "all incoming orders", async token =>
        {
            var orders = await _store.ReadAllAsync<Order>(DataFiles.IncomingOrders, token);
            Log.Success(Name, $"Found {orders.Count} incoming order(s)");
            return orders;
        }, ct);
    }
}
