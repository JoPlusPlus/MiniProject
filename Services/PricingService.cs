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
    public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
    {
        decimal subTotal = 0;
        var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
        foreach(var item in order.Items)
        {
            Product p = products.FirstOrDefault(p =>  p.Id == item.ProductId)!;
            item.UnitPrice = p.Price;
            subTotal += item.UnitPrice * item.Quantity;
        }
        return subTotal;
    }
    public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct) {
        decimal discount = 0;
        CustomerRepository customers = new CustomerRepository();
        CouponRepository coupons = new CouponRepository();

        Customer? customer = await customers.GetByIdAsync(order.CustomerId, ct);
        if(customer == null)
        {
            Log.Error("PricingService", $"Customer #{order.CustomerId} not found");
        }
        // loyality
        if (customer.LoyaltyTier == LoyaltyTier.Silver) discount += 5; 
        else if (customer.LoyaltyTier == LoyaltyTier.Gold) discount += 10;

        // order has no coupon code
        if(order.CouponCode == null)
        {
            order.Discount = await this.CalculateSubTotalAsync(order, ct) * (discount / 100);
            return discount;
        }
        // order has invalid coupon code
        var coupon = await coupons.GetByCodeAsync(order.CouponCode,ct);
        if(coupon == null)
        {
            order.Discount = await this.CalculateSubTotalAsync(order, ct) * (discount / 100);
            return discount;
        }
        else
        {
            discount += coupon.DiscountPercent;
            if (discount > 30) discount = 30;
            order.Discount = await this.CalculateSubTotalAsync(order, ct) * (discount / 100);
            return discount;
        }

    }
    public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
    {
        var tryFindOrder = failShippingForOrderIds.FirstOrDefault(o => o == order.Id);
        if(tryFindOrder != 0)
        {
            try
            {
                throw new ShippingProviderUnavailableException();

            }catch (Exception ex)
            {
                Log.Error("PricingService", $"{ex.Message} for order #{order.Id}");
            }
        }

        var products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

        decimal subTotal = await this.CalculateSubTotalAsync(order, ct);
        if (subTotal >= 1000) return 0;

        decimal totalQuantity = 0;
        decimal totalWeight = 0;
        foreach(var item in order.Items)
        {
            totalQuantity += item.Quantity;
            var p = products.FirstOrDefault(p => p.Id == item.ProductId);
            if(p != null)
            {
                totalWeight += p.WeightKg;
            }
        }
        return 5m + 1.5m * subTotal + 2m * totalWeight;
    }
}
