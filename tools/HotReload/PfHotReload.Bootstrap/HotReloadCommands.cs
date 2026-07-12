using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using PfHotReload.Contract;
using AcadRuntime = Autodesk.AutoCAD.Runtime;

namespace PfHotReload.Bootstrap;

public sealed class HotReloadCommands
{
    private const string ProbeDllPath = @"D:\PlantFlow\PlantFlow_Support\tools\HotReload\PfHotReload.Probe\bin\x64\Debug\PfHotReload.Probe.dll";
    private static LoadedSession? _current;

    [AcadRuntime.CommandMethod("PFLOAD", AcadRuntime.CommandFlags.Session)]
    public void LoadProbe()
    {
        Editor editor = GetEditor();

        try
        {
            if (_current is not null)
            {
                editor.WriteMessage("\nPFLOAD skipped: session already loaded. Run PFUNLOAD first.");
                return;
            }

            string pluginPath = ProbeDllPath;
            if (!File.Exists(pluginPath))
            {
                editor.WriteMessage($"\nPFLOAD failed: plugin DLL not found: {pluginPath}");
                return;
            }

            string pluginDirectory = Path.GetDirectoryName(pluginPath) ?? throw new InvalidOperationException("Plugin path has no directory.");
            string pdbPath = Path.ChangeExtension(pluginPath, ".pdb");
            byte[] assemblyBytes = File.ReadAllBytes(pluginPath);
            byte[]? pdbBytes = File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null;

            var alc = new PluginLoadContext(pluginDirectory);
            Assembly assembly;
            using (var assemblyStream = new MemoryStream(assemblyBytes))
            {
                if (pdbBytes is null)
                {
                    assembly = alc.LoadFromStream(assemblyStream);
                }
                else
                {
                    using var pdbStream = new MemoryStream(pdbBytes);
                    assembly = alc.LoadFromStream(assemblyStream, pdbStream);
                }
            }

            IPluginSession session = CreateSession(assembly);
            session.Initialize();

            _current = new LoadedSession(session, alc);
            editor.WriteMessage($"\nPFLOAD loaded asm={assembly.GetName().Name} alc={alc.Id}");
        }
        catch (IOException ex)
        {
            editor.WriteMessage($"\nPFLOAD I/O failed: {ex.Message}");
            _current = null;
        }
        catch (Exception ex)
        {
            editor.WriteMessage($"\nPFLOAD failed: {ex}");
            _current = null;
        }
    }

    [AcadRuntime.CommandMethod("PFRUN", AcadRuntime.CommandFlags.Session)]
    public void RunProbe()
    {
        Editor editor = GetEditor();

        try
        {
            if (_current?.Session is null)
            {
                editor.WriteMessage("\nPFRUN failed: no loaded session. Run PFLOAD first.");
                return;
            }

            PromptResult commandResult = editor.GetString("\nCommand name: ");
            if (commandResult.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(commandResult.StringResult))
            {
                editor.WriteMessage("\nPFRUN cancelled.");
                return;
            }

            PromptResult argsResult = editor.GetString("\nArgs <empty>: ");
            string args = argsResult.Status == PromptStatus.OK ? argsResult.StringResult ?? string.Empty : string.Empty;
            string command = commandResult.StringResult.Trim();
            string result = _current.Session.Execute(command, args);
            editor.WriteMessage($"\nPFRUN {command}: {result}");
        }
        catch (Exception ex)
        {
            editor.WriteMessage($"\nPFRUN failed: {ex}");
        }
    }

    [AcadRuntime.CommandMethod("PFUNLOAD", AcadRuntime.CommandFlags.Session)]
    public void UnloadProbe()
    {
        Editor editor = GetEditor();

        try
        {
            if (_current is null)
            {
                editor.WriteMessage("\nPFUNLOAD skipped: no loaded session.");
                return;
            }

            WeakReference alcReference = BeginUnload(_current);
            _current = null;

            bool collected = WaitForUnload(alcReference);
            editor.WriteMessage($"\nPFUNLOAD collected={collected} (IsAlive={alcReference.IsAlive})");
        }
        catch (Exception ex)
        {
            editor.WriteMessage($"\nPFUNLOAD failed: {ex}");
        }
    }

    private static IPluginSession CreateSession(Assembly assembly)
    {
        Type? sessionType = assembly.GetTypes()
            .FirstOrDefault(type => typeof(IPluginSession).IsAssignableFrom(type)
                && type is { IsAbstract: false, IsInterface: false });

        if (sessionType is null)
        {
            throw new InvalidOperationException($"No {nameof(IPluginSession)} implementation found in {assembly.FullName}.");
        }

        object? instance = Activator.CreateInstance(sessionType);
        return instance as IPluginSession
            ?? throw new InvalidCastException($"Type {sessionType.FullName} is not assignable to {nameof(IPluginSession)}.");
    }

    private static bool WaitForUnload(WeakReference alcReference)
    {
        for (int i = 0; alcReference.IsAlive && i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return !alcReference.IsAlive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BeginUnload(LoadedSession loadedSession)
    {
        return loadedSession.Unload();
    }

    private static Editor GetEditor()
    {
        Document? document = Application.DocumentManager.MdiActiveDocument;
        return document?.Editor ?? throw new InvalidOperationException("No active AutoCAD document.");
    }
}
