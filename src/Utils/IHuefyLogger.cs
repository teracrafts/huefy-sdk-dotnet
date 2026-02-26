namespace Huefy.Utils;

public enum HuefyLogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public interface IHuefyLogger
{
    void Log(HuefyLogLevel level, string message);
}

public class ConsoleLogger : IHuefyLogger
{
    public void Log(HuefyLogLevel level, string message)
    {
        Console.WriteLine($"[{level}] [Huefy] {message}");
    }
}

public class NullLogger : IHuefyLogger
{
    public void Log(HuefyLogLevel level, string message)
    {
        // intentionally empty
    }
}
