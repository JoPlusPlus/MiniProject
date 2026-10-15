namespace Mini_Project___Order_Processing_System.Services
{
    public static class OrderPricer
    {
        
        private static PricingService pricingService = new PricingService(new[] { 7 });
        
        public async static Task PriceOrderAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await pricingService.CalculateSubTotalAsync(order, ct);

            Task<decimal> discountTask = pricingService.GetDiscountAsync(order, ct);
            Task<decimal> shippingTask = pricingService.CalculateShippingCostAsync(order, ct);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        }
        public async static Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await pricingService.CalculateSubTotalAsync(order, ct);

            order.Discount = await pricingService.GetDiscountAsync(order, ct);
            order.ShippingCost = await pricingService.CalculateShippingCostAsync(order, ct);

            order.TotalAmount = Math.Round(order.SubTotal - order.Discount + order.ShippingCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}
