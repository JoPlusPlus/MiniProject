using System.Collections.Concurrent;
using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Services;

public class OrderProcessorV2
{
    private const string Name = "OrderProcessorV2";

    private readonly ProductRepository _products = new();
    private readonly OrderRepository _orders = new();
    private readonly InvoiceGenerator _invoiceGenerator = new();

    // remembers which items are reserved for each order (key = order id)
    private readonly ConcurrentDictionary<int, List<OrderItem>> _reserved = new();

    // Does not catch exceptions, the runner does.
    public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
    {
        Log.Info(Name, $"Processing Order #{order.Id}");

        var reservedItems = new List<OrderItem>();
        _reserved[order.Id] = reservedItems;

        // 1. reserve stock for each item, in item order
        foreach (var item in order.Items)
        {
            bool ok = await _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);
            if (ok == false)
            {
                // give back what we already reserved
                await ReleaseStockAsync(order, ct);

                var product = await _products.GetByIdAsync(item.ProductId, ct);
                string productName = "Product #" + item.ProductId;
                if (product != null) productName = product.Name;

                order.Status = OrderStatus.Rejected;
                order.FailureReason = "Insufficient stock for '" + productName + "'";
                await _orders.SaveAsync(order, ct);

                Log.Error(Name, $"Order #{order.Id} rejected: {order.FailureReason}");
                return false;
            }

            reservedItems.Add(item);
        }

        // 2. price the order
        await OrderPricer.PriceOrderAsync(order, ct);

        // 3. invoice
        await _invoiceGenerator.GenerateAsync(order, ct);

        // 4. done
        order.Status = OrderStatus.Completed;
        await _orders.SaveAsync(order, ct);
        _reserved.TryRemove(order.Id, out _);

        Log.Success(Name, $"Order #{order.Id} completed (Total {order.TotalAmount})");
        return true;
    }

    // Gives back the stock this order still has reserved
    public async Task ReleaseStockAsync(Order order, CancellationToken ct)
    {
        if (_reserved.TryGetValue(order.Id, out var items) == false) return;

        while (items.Count > 0)
        {
            var item = items[0];
            await _products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
            items.RemoveAt(0);
        }
    }
}
