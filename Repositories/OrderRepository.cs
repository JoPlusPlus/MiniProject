using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class OrderRepository
{
    private const string Name = "OrderRepository";
    private readonly JsonFileStore _store = new();

    public async Task SaveAsync(Order order, CancellationToken ct)
    {
        await Simulate.DbWriteAsync(Name, $"Saving Order #{order.Id}", async token =>
        {
            await _store.UpdateAsync<Order>(DataFiles.Orders, list =>
            {
                int index = list.FindIndex(o => o.Id == order.Id);
                if (index >= 0) list[index] = order;
                else list.Add(order);
            }, token);
        }, ct);

        Log.Success(Name, $"Order #{order.Id} saved ({order.Status})");
    }

    public Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        return Simulate.DbQueryAsync<List<Order>>(Name, "all orders", async token =>
        {
            var orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, token);
            Log.Success(Name, $"Found {orders.Count} order(s)");
            return orders;
        }, ct);
    }

    public Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct)
    {
        return Simulate.DbQueryAsync<List<Order>>(Name, $"orders with status {status}", async token =>
        {
            var orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, token);
            var result = orders.Where(o => o.Status == status).ToList();
            Log.Success(Name, $"Found {result.Count} order(s) with status {status}");
            return result;
        }, ct);
    }
}
