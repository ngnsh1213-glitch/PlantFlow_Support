namespace PfHotReload.Contract;

public interface IPluginSession : IDisposable
{
    void Initialize();

    string Execute(string command, string args);
}
