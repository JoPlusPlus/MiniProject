using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System;

public class CustomerRepository
{
    private const string Name = "CustomerRepository";
    private readonly JsonFileStore _store = new();

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
    {
        return Simulate.DbQueryAsync<Customer?>(Name, $"Customer #{id}", async token =>
        {
            var customers = await _store.ReadAllAsync<Customer>(DataFiles.Customers, token);
            var customer = customers.FirstOrDefault(c => c.Id == id);

            if (customer == null) Log.Error(Name, $"Customer #{id} not found");
            else Log.Success(Name, $"Customer #{id} found ({customer.Name})");

            return customer;
        }, ct);
    }
}
