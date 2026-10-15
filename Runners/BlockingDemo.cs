using Mini_Project___Order_Processing_System.Data;

namespace Mini_Project___Order_Processing_System.Runners;

// Phase 2 question 3. This is the ONLY place where .Result is allowed.
public class BlockingDemo
{
    public void Run()
    {
        Console.WriteLine("\n--- .Result demo ---");
        Log.Info("BlockingDemo", $"Thread {Environment.CurrentManagedThreadId} starts the task");

        Task<int> task = GetNumberAsync(CancellationToken.None);

        Log.Info("BlockingDemo", "Calling .Result: this thread is BLOCKED until the task finishes");
        int number = task.Result;

        Log.Info("BlockingDemo", $"Got {number} on thread {Environment.CurrentManagedThreadId}");
    }

    private async Task<int> GetNumberAsync(CancellationToken ct)
    {
        await Task.Delay(2000, ct);
        return 42;
    }
}
