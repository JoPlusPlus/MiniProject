namespace Mini_Project___Order_Processing_System.Services
{
    public class OrderPricer
    {
        private static PricingService pricingService = new PricingService(new[] {1,4,7,10,13,16,19});
        public async static Task PriceOrderAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = await pricingService.CalculateSubTotalAsync(order, ct);
            Task<decimal> t1 = pricingService.GetDiscountAsync(order, ct);
            Task<decimal> t2 = pricingService.CalculateShippingCostAsync(order, ct);

            await Task.WhenAll(t1, t2);

            decimal discount = t1.Result;
            decimal shippingCost = t2.Result;
        }
        public async static Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = await pricingService.CalculateSubTotalAsync(order, ct);

            decimal discount = await pricingService.GetDiscountAsync(order,ct);
            decimal shippingCost = await pricingService.CalculateShippingCostAsync(order, ct);
        }
    }
}
