using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mini_Project___Order_Processing_System.MyExceptions;

namespace Mini_Project___Order_Processing_System.Data;

public class JsonFileStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly UTF8Encoding Utf8 = new(false);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private static SemaphoreSlim GetLock(string fileName)
    {
        return Locks.GetOrAdd(DataFiles.PathOf(fileName), _ => new SemaphoreSlim(1, 1));
    }

    // ================= ASYNC =================

    public async Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct)
    {
        var gate = GetLock(fileName);
        await gate.WaitAsync(ct);
        try
        {
            return await ReadCoreAsync<T>(fileName, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task WriteAllAsync<T>(string fileName, List<T> items, CancellationToken ct)
    {
        var gate = GetLock(fileName);
        await gate.WaitAsync(ct);
        try
        {
            await WriteCoreAsync(fileName, items, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpdateAsync<T>(string fileName, Action<List<T>> change, CancellationToken ct)
    {
        var gate = GetLock(fileName);
        await gate.WaitAsync(ct);
        try
        {
            var items = await ReadCoreAsync<T>(fileName, ct);
            change(items);
            await WriteCoreAsync(fileName, items, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public Task AddAsync<T>(string fileName, T item, CancellationToken ct)
    {
        return UpdateAsync<T>(fileName, list => list.Add(item), ct);
    }

    // ================= SYNC =================

    public List<T> ReadAll<T>(string fileName)
    {
        var gate = GetLock(fileName);
        gate.Wait();
        try
        {
            return ReadCore<T>(fileName);
        }
        finally
        {
            gate.Release();
        }
    }

    public void WriteAll<T>(string fileName, List<T> items)
    {
        var gate = GetLock(fileName);
        gate.Wait();
        try
        {
            WriteCore(fileName, items);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Update<T>(string fileName, Action<List<T>> change)
    {
        var gate = GetLock(fileName);
        gate.Wait();
        try
        {
            var items = ReadCore<T>(fileName);
            change(items);
            WriteCore(fileName, items);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Add<T>(string fileName, T item)
    {
        Update<T>(fileName, list => list.Add(item));
    }

    // ================= Functuion used many times in this file =================

    private static List<T> Parse<T>(string fileName, string json)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<T>>(json, Options);
            if (items == null) throw new DataFileCorruptedException(fileName);
            return items;
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(fileName);
        }
    }

    private static async Task<List<T>> ReadCoreAsync<T>(string fileName, CancellationToken ct)
    {
        var path = DataFiles.PathOf(fileName);
        if (!File.Exists(path)) return new List<T>();
        var json = await File.ReadAllTextAsync(path, Utf8, ct);
        return Parse<T>(fileName, json);
    }

    private static List<T> ReadCore<T>(string fileName)
    {
        var path = DataFiles.PathOf(fileName);
        if (!File.Exists(path)) return new List<T>();
        var json = File.ReadAllText(path, Utf8);
        return Parse<T>(fileName, json);
    }

    private static async Task WriteCoreAsync<T>(string fileName, List<T> items, CancellationToken ct)
    {
        Directory.CreateDirectory(DataFiles.Folder);
        var path = DataFiles.PathOf(fileName);
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(items, Options);
        await File.WriteAllTextAsync(temp, json, Utf8, ct);
        File.Move(temp, path, overwrite: true);
    }

    private static void WriteCore<T>(string fileName, List<T> items)
    {
        Directory.CreateDirectory(DataFiles.Folder);
        var path = DataFiles.PathOf(fileName);
        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(items, Options);
        File.WriteAllText(temp, json, Utf8);
        File.Move(temp, path, overwrite: true);
    }
}
