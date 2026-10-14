using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class OrderValidator
{
    private const string Name = "OrderValidator";

    private readonly CustomerRepository _customers = new();
    private readonly ProductRepository _products = new();
    private readonly CouponRepository _coupons = new();
    private readonly OrderRepository _orders = new();

    public async Task<Order> ValidateOrderAsync(Order order, CancellationToken ct)
    {
        Log.Info(Name, $"Validating order #{order.Id}");

        // Customer Validation
        var customer = await _customers.GetByIdAsync(order.CustomerId, ct);
        if (customer == null)
            return await RejectAsync(order, $"Customer #{order.CustomerId} not found", ct);
        if (!customer.IsActive)
            return await RejectAsync(order, "Customer is inactive", ct);

        // Product Validation
        var products = new List<Product>();
        foreach (var item in order.Items)
        {
            var product = await _products.GetByIdAsync(item.ProductId, ct);
            if (product == null)
                return await RejectAsync(order, $"Product #{item.ProductId} not found", ct);
            products.Add(product);
        }

        // Stock
        for (int i = 0; i < order.Items.Count; i++)
        {
            if (products[i].StockQuantity < order.Items[i].Quantity)
                return await RejectAsync(order, $"Insufficient stock for '{products[i].Name}'", ct);
        }

        // Copoun
        if (order.CouponCode != null)
        {
            var coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
            if (coupon == null)
                return await RejectAsync(order, $"Coupon '{order.CouponCode}' does not exist", ct);
            if (coupon.ExpiryDate < DateTime.Today)
                return await RejectAsync(order, $"Coupon '{order.CouponCode}' has expired", ct);
        }

        order.Status = OrderStatus.Validated;
        await _orders.SaveAsync(order, ct);
        Log.Success(Name, $"Order #{order.Id} validated");
        return order;
    }

    private async Task<Order> RejectAsync(Order order, string reason, CancellationToken ct)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
        await _orders.SaveAsync(order, ct);
        Log.Error(Name, $"Order #{order.Id} rejected: {reason}");
        return order;
    }
}
