using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Repositories;

public class SyncOrderValidator
{
    private const string Name = "SyncOrderValidator";

    private readonly SyncCustomerRepository _customers = new();
    private readonly SyncProductRepository _products = new();
    private readonly SyncCouponRepository _coupons = new();

    public Order ValidateOrder(Order order)
    {
        Log.Info(Name, $"Validating order #{order.Id}");

        var customer = _customers.GetById(order.CustomerId);
        if (customer == null)
            return Reject(order, $"Customer #{order.CustomerId} not found");
        if (!customer.IsActive)
            return Reject(order, "Customer is inactive");

        var products = new List<Product>();
        foreach (var item in order.Items)
        {
            var product = _products.GetById(item.ProductId);
            if (product == null)
                return Reject(order, $"Product #{item.ProductId} not found");
            products.Add(product);
        }

        for (int i = 0; i < order.Items.Count; i++)
        {
            if (products[i].StockQuantity < order.Items[i].Quantity)
                return Reject(order, $"Insufficient stock for '{products[i].Name}'");
        }

        if (order.CouponCode != null)
        {
            var coupon = _coupons.GetByCode(order.CouponCode);
            if (coupon == null)
                return Reject(order, $"Coupon '{order.CouponCode}' does not exist");
            if (coupon.ExpiryDate < DateTime.Today)
                return Reject(order, $"Coupon '{order.CouponCode}' has expired");
        }

        order.Status = OrderStatus.Validated;
        Log.Success(Name, $"Order #{order.Id} validated");
        return order;
    }

    private Order Reject(Order order, string reason)
    {
        order.Status = OrderStatus.Rejected;
        order.FailureReason = reason;
        Log.Error(Name, $"Order #{order.Id} rejected: {reason}");
        return order;
    }
}
