using System.Reflection;
using System.Runtime.Loader;

namespace PfHotReload.Bootstrap;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private static int _nextId;
    private readonly string _pluginDirectory;

    public PluginLoadContext(string pluginDirectory)
        : base($"PfHotReload.Plugin.{Interlocked.Increment(ref _nextId)}", isCollectible: true)
    {
        _pluginDirectory = pluginDirectory ?? throw new ArgumentNullException(nameof(pluginDirectory));
        Id = Name ?? "PfHotReload.Plugin";
    }

    public string Id { get; }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (ShouldShareWithDefaultContext(assemblyName.Name))
        {
            return null;
        }

        string candidatePath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        if (!File.Exists(candidatePath))
        {
            return null;
        }

        try
        {
            return LoadFromAssemblyPath(candidatePath);
        }
        catch (Exception ex) when (ex is FileLoadException or BadImageFormatException)
        {
            throw new InvalidOperationException($"Failed to load plugin dependency: {candidatePath}", ex);
        }
    }

    private static bool ShouldShareWithDefaultContext(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return true;
        }

        return assemblyName.StartsWith("Autodesk", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Ac", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Pnp", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("PnP", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("PfHotReload.Contract", StringComparison.OrdinalIgnoreCase);
    }
}
