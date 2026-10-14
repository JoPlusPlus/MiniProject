using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Repositories;

public class SyncCustomerRepository
{
    private const string Name = "SyncCustomerRepository";
    private readonly JsonFileStore _store = new();

    public Customer? GetById(int id)
    {
        return Simulate.DbQuery<Customer?>(Name, $"Customer #{id}", () =>
        {
            var customers = _store.ReadAll<Customer>(DataFiles.Customers);
            var customer = customers.FirstOrDefault(c => c.Id == id);

            if (customer == null) Log.Error(Name, $"Customer #{id} not found");
            else Log.Success(Name, $"Customer #{id} found ({customer.Name})");

            return customer;
        });
    }
}
