namespace Mini_Project___Order_Processing_System.Data;
public static class DataSeeder
{
    private static readonly JsonFileStore Store = new();

    public static async Task ResetAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(DataFiles.Folder))
            Directory.Delete(DataFiles.Folder, recursive: true);

        Directory.CreateDirectory(DataFiles.Folder);
        Directory.CreateDirectory(DataFiles.InvoicesFolder);
        Directory.CreateDirectory(DataFiles.ReportsFolder);

        await Seed(DataFiles.Customers, CreateCustomers(), ct);
        await Seed(DataFiles.Products, CreateProducts(), ct);
        await Seed(DataFiles.Coupons, CreateCoupons(), ct);
        await Seed(DataFiles.IncomingOrders, CreateIncomingOrders(), ct);

        await Seed(DataFiles.Orders, new List<Order>(), ct);
        await Seed(DataFiles.Payments, new List<PaymentTransaction>(), ct);
        await Seed(DataFiles.Emails, new List<EmailMessage>(), ct);
        await Seed(DataFiles.EmailRetry, new List<EmailMessage>(), ct);
    }

    private static async Task Seed<T>(string fileName, List<T> items, CancellationToken ct)
    {
        await Store.WriteAllAsync(fileName, items, ct);
        Console.WriteLine($"[Seeder] {fileName} -> {items.Count} records.");
    }

    private static Customer C(int id, string first, string last, bool active, LoyaltyTier tier) => new()
    {
        Id = id,
        Name = $"{first} {last}",
        Email = $"{first}.{last}@example.com".ToLower(),
        IsActive = active,
        LoyaltyTier = tier
    };

    private static List<Customer> CreateCustomers() => new()
    {
        C(1, "Ahmed", "Ali", true, LoyaltyTier.Gold),
        C(2, "Sara", "Mohamed", true, LoyaltyTier.Silver),
        C(3, "Omar", "Hassan", true, LoyaltyTier.Bronze),
        C(4, "Mona", "Ibrahim", false, LoyaltyTier.Silver),
        C(5, "Youssef", "Adel", true, LoyaltyTier.Gold),
        C(6, "Nour", "Khaled", true, LoyaltyTier.Bronze),
        C(7, "Karim", "Samir", true, LoyaltyTier.Silver),
        C(8, "Laila", "Mahmoud", true, LoyaltyTier.Gold),
        C(9, "Hassan", "Tarek", true, LoyaltyTier.Bronze),
        C(10, "Dina", "Fathy", true, LoyaltyTier.Silver)
    };

    private static Product P(int id, string name, decimal price, int stock, decimal weight) => new()
    {
        Id = id, Name = name, Price = price, StockQuantity = stock, WeightKg = weight
    };

    private static List<Product> CreateProducts() => new()
    {
        P(1, "Laptop", 1200.00m, 10, 2.2m),
        P(2, "Smartphone", 800.00m, 15, 0.4m),
        P(3, "Wireless Mouse", 25.00m, 2, 0.1m),
        P(4, "Mechanical Keyboard", 90.00m, 1, 0.9m),
        P(5, "USB-C Hub", 45.00m, 30, 0.2m),
        P(6, "Headphones", 150.00m, 12, 0.3m),
        P(7, "27\" Monitor", 350.00m, 6, 5.0m),
        P(8, "Webcam", 70.00m, 20, 0.2m)
    };

    private static List<Coupon> CreateCoupons() => new()
    {
        new Coupon { Code = "SAVE10",    DiscountPercent = 10, ExpiryDate = DateTime.Today.AddDays(180) },
        new Coupon { Code = "WELCOME15", DiscountPercent = 15, ExpiryDate = DateTime.Today.AddDays(90) },
        new Coupon { Code = "FLASH20",   DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(7) },
        new Coupon { Code = "OLD20",     DiscountPercent = 20, ExpiryDate = DateTime.Today.AddDays(-30) }
    };

    private static Order O(int id, int customerId, string? coupon, params (int product, int qty)[] items) => new()
    {
        Id = id,
        CustomerId = customerId,
        CouponCode = coupon,
        Items = items.Select(i => new OrderItem { ProductId = i.product, Quantity = i.qty, UnitPrice = 0m }).ToList(),
        SubTotal = 0m, Discount = 0m, ShippingCost = 0m, TotalAmount = 0m,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.Now,
        FailureReason = null,
        TransactionId = null
    };

    private static List<Order> CreateIncomingOrders() => new()
    {
        O(1, 1, "SAVE10", (1, 1), (3, 1)),
        O(2, 2, null, (2, 1)),
        O(3, 3, "WELCOME15", (5, 2), (8, 1)),
        O(4, 4, null, (6, 1)),
        O(5, 5, null, (7, 1)),
        O(6, 6, "FLASH20", (5, 1)),
        O(7, 7, null, (6, 1), (8, 1)),
        O(8, 8, "SAVE10", (1, 1)),
        O(9, 9, null, (3, 5)),
        O(10, 10, null, (4, 1)),
        O(11, 1, null, (4, 1)),
        O(12, 2, "OLD20", (2, 1)),
        O(13, 3, null, (6, 2)),
        O(14, 5, "WELCOME15", (8, 2), (5, 1)),
        O(15, 99, null, (2, 1)),
        O(16, 6, null, (7, 1), (5, 1)),
        O(17, 7, "SAVE10", (2, 1)),
        O(18, 8, "FAKE50", (6, 1)),
        O(19, 9, null, (8, 1)),
        O(20, 10, "FLASH20", (1, 1), (6, 1))
    };
}
