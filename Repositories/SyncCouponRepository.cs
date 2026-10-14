using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Repositories;

public class SyncCouponRepository
{
    private const string Name = "SyncCouponRepository";
    private readonly JsonFileStore _store = new();

    public Coupon? GetByCode(string code)
    {
        return Simulate.DbQuery<Coupon?>(Name, $"Coupon {code}", () =>
        {
            var coupons = _store.ReadAll<Coupon>(DataFiles.Coupons);
            var coupon = coupons.FirstOrDefault(c => c.Code.ToLower() == code.ToLower());

            if (coupon == null) Log.Error(Name, $"Coupon {code} not found");
            else Log.Success(Name, $"Coupon {code} found ({coupon.DiscountPercent}%)");

            return coupon;
        });
    }
}
