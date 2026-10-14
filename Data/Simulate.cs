namespace Mini_Project___Order_Processing_System.Data;

public static class Simulate
{

    // ========================================= ASYNC Version ========================================

    public static async Task StepAsync(string component, string message, int delayMs, CancellationToken ct)
    {
        Log.Info(component, message);
        await Task.Delay(delayMs, ct);
    }

    public static async Task<T> DbQueryAsync<T>(string component, string description,
        Func<CancellationToken, Task<T>> read, CancellationToken ct)
    {
        await StepAsync(component, "Opening connection...", 200, ct);
        await StepAsync(component, $"Executing query for {description}...", Random.Shared.Next(300, 600 + 1), ct);
        return await read(ct);
    }

    public static async Task DbWriteAsync(string component, string description,
        Func<CancellationToken, Task> write, CancellationToken ct)
    {
        await StepAsync(component, "Opening connection...", 200, ct);
        await StepAsync(component, "Beginning transaction...", 100, ct);
        Log.Info(component, $"{description}...");
        await Task.Delay(400, ct);
        await write(ct);
        await StepAsync(component, "Committing transaction...", 200, ct);
    }

    public static async Task ExternalApiAsync(string component, string description, CancellationToken ct)
    {
        await StepAsync(component, "DNS lookup and handshake...", 150, ct);
        await StepAsync(component, $"Sending request ({description})...", 300, ct);
        await StepAsync(component, "Waiting for response...", Random.Shared.Next(500, 1500 + 1), ct);
    }

    // ========================================= SYNC Version ========================================
    public static void Step(string component, string message, int delayMs)
    {
        Log.Info(component, message);
        Thread.Sleep(delayMs);
    }

    public static T DbQuery<T>(string component, string description, Func<T> read)
    {
        Step(component, "Opening connection...", 200);
        Step(component, $"Executing query for {description}...", Random.Shared.Next(300, 600 + 1));
        return read();
    }

    public static void DbWrite(string component, string description, Action write)
    {
        Step(component, "Opening connection...", 200);
        Step(component, "Beginning transaction...", 100);
        Log.Info(component, $"{description}...");
        Thread.Sleep(400);
        write();
        Step(component, "Committing transaction...", 200);
    }

    public static void ExternalApi(string component, string description)
    {
        Step(component, "DNS lookup and handshake...", 150);
        Step(component, $"Sending request ({description})...", 300);
        Step(component, "Waiting for response...", Random.Shared.Next(500, 1500 + 1));
    }

}
