using Mini_Project___Order_Processing_System.Data;
using Mini_Project___Order_Processing_System.MyExceptions;

namespace Mini_Project___Order_Processing_System.Services;

public class PricingService
{
    private IEnumerable<int> failShippingForOrderIds;
    public PricingService(IEnumerable<int> failShippingForOrderIds)
    {
        this.failShippingForOrderIds = failShippingForOrderIds;
    }

    private static JsonFileStore _store = new JsonFileStore();
    private static CustomerRepository customers = new CustomerRepository();
    private static CouponRepository coupons = new CouponRepository();

    public Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
    {
        return Simulate.DbQueryAsync<decimal>("PricingService", $"SubTotal of Order #{order.Id}", async token =>
        {
            decimal subTotal = 0;
            var products = await _store.ReadAllAsync<Product>(DataFiles.Products, token);
            foreach (var item in order.Items)
            {
                Product product = products.First(x => x.Id == item.ProductId);
                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }

            subTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
            Log.Success("PricingService", $"Order #{order.Id} SubTotal = {subTotal}");
            return subTotal;
        }, ct);
    }
    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
    {
        await Simulate.ExternalApiAsync("PricingService", $"discount for Order #{order.Id}", ct);

        decimal percent = 0;

        // loyalty
        Customer? customer = await customers.GetByIdAsync(order.CustomerId, ct);
        if (customer == null)
        {
            Log.Error("PricingService", $"Customer #{order.CustomerId} not found");
        }
        else if (customer.LoyaltyTier == LoyaltyTier.Silver) percent += 5;
        else if (customer.LoyaltyTier == LoyaltyTier.Gold) percent += 10;

        // coupon
        if (order.CouponCode != null)
        {
            var coupon = await coupons.GetByCodeAsync(order.CouponCode, ct);
            if (coupon != null) percent += coupon.DiscountPercent;
        }

        if (percent > 30) percent = 30;

        decimal discount = Math.Round(order.SubTotal * percent / 100, 2, MidpointRounding.AwayFromZero);
        Log.Success("PricingService", $"Order #{order.Id} Discount = {discount} ({percent}%)");
        return discount;
    }
    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
    {
        await Simulate.StepAsync("PricingService", "DNS lookup and handshake...", 150, ct);

        // controlled failure
        if (failShippingForOrderIds.Contains(order.Id))
        {
            var ex = new ShippingProviderUnavailableException();
            Log.Error("PricingService", $"{ex.Message} for order #{order.Id}");
            throw ex;
        }

        await Simulate.StepAsync("PricingService", $"Sending request (shipping for Order #{order.Id})...", 300, ct);
        await Simulate.StepAsync("PricingService", "Waiting for response...", Random.Shared.Next(500, 1500 + 1), ct);

        var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

        // free shipping
        if (order.SubTotal >= 1000) return 0;

        int totalQuantity = 0;
        decimal totalWeight = 0;
        foreach (var item in order.Items)
        {
            Product product = products.First(x => x.Id == item.ProductId);
            totalQuantity += item.Quantity;
            totalWeight += product.WeightKg * item.Quantity;
        }

        decimal shipping = 5m + 1.5m * totalQuantity + 2m * totalWeight;
        shipping = Math.Round(shipping, 2, MidpointRounding.AwayFromZero);
        Log.Success("PricingService", $"Order #{order.Id} Shipping = {shipping}");
        return shipping;
    }
}
