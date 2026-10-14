namespace Mini_Project___Order_Processing_System.MyExceptions;

public class DataFileCorruptedException : Exception
{
    public DataFileCorruptedException(string fileName)
        : base($"Data file '{fileName}' is corrupted (invalid JSON).") { }
}
