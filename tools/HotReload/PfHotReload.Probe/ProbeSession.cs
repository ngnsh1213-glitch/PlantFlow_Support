using PfHotReload.Contract;

namespace PfHotReload.Probe;

public sealed class ProbeSession : IPluginSession
{
    private bool _initialized;

    public void Initialize()
    {
        _initialized = true;
    }

    public string Execute(string command, string args)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("ProbeSession is not initialized.");
        }

        return command.Equals("Ping", StringComparison.OrdinalIgnoreCase)
            ? $"PONG v1 @{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}"
            : $"UNKNOWN command={command}";
    }

    public void Dispose()
    {
        _initialized = false;
    }
}
