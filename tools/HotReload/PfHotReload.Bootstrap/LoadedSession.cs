using System.Runtime.Loader;
using PfHotReload.Contract;

namespace PfHotReload.Bootstrap;

internal sealed class LoadedSession
{
    private readonly AssemblyLoadContext _loadContext;

    public LoadedSession(IPluginSession session, AssemblyLoadContext loadContext)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        _loadContext = loadContext ?? throw new ArgumentNullException(nameof(loadContext));
    }

    public IPluginSession Session { get; private set; }

    public WeakReference Unload()
    {
        WeakReference loadContextReference = new(_loadContext, trackResurrection: false);
        Session.Dispose();
        Session = null!;
        _loadContext.Unload();
        return loadContextReference;
    }
}
