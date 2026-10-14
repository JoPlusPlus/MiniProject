using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class CouponRepository
{
    private const string Name = "CouponRepository";
    private readonly JsonFileStore _store = new();

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return Simulate.DbQueryAsync<Coupon?>(Name, $"Coupon {code}", async token =>
        {
            var coupons = await _store.ReadAllAsync<Coupon>(DataFiles.Coupons, token);
            var coupon = coupons.FirstOrDefault(c => c.Code.ToLower() == code.ToLower());

            if (coupon == null) Log.Error(Name, $"Coupon {code} not found");
            else Log.Success(Name, $"Coupon {code} found ({coupon.DiscountPercent}%)");

            return coupon;
        }, ct);
    }
}
