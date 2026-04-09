// EX0Stable.Renderer - Fix controller hotplug crashes in the renderer process.
// Loaded via Unity Doorstop before the game starts.
// Defers Harmony patching until Assembly-CSharp.dll is loaded by Unity.

using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace Doorstop
{
    public static class Entrypoint
    {
        private const string HARMONY_ID = "dev.ex0.stable.renderer";
        internal static string LogPath = "";
        private static bool _patched = false;

        public static void Start()
        {
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LogPath = Path.Combine(dir, "EX0Stable.Renderer.log");

                if (File.Exists(LogPath)) File.Delete(LogPath);

                Log("Loaded via Doorstop. Waiting for Assembly-CSharp...");
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            }
            catch (Exception e)
            {
                Log($"Start failed: {e}");
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (_patched) return;
            if (args.LoadedAssembly.GetName().Name != "Assembly-CSharp") return;

            _patched = true;
            Log("Assembly-CSharp loaded. Applying patches...");

            try
            {
                var driverType = args.LoadedAssembly.GetType("SteamVRDriver");
                if (driverType == null)
                {
                    Log("ERROR: SteamVRDriver type not found in Assembly-CSharp");
                    return;
                }

                var method = driverType.GetMethod("OnDeviceConnected",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static);

                if (method == null)
                {
                    Log("ERROR: OnDeviceConnected not found. Available methods:");
                    foreach (var m in driverType.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.DeclaredOnly))
                    {
                        var parms = Array.ConvertAll(m.GetParameters(),
                            p => p.ParameterType.Name + " " + p.Name);
                        Log($"  {m.Name}({string.Join(", ", parms)})");
                    }
                    return;
                }

                Log($"Found: {driverType.FullName}.{method.Name}");

                var harmony = new Harmony(HARMONY_ID);

                var prefix = typeof(EX0Stable.Renderer.Patches).GetMethod(
                    "OnDeviceConnected_Prefix", BindingFlags.Static | BindingFlags.Public);
                var finalizer = typeof(EX0Stable.Renderer.Patches).GetMethod(
                    "OnDeviceConnected_Finalizer", BindingFlags.Static | BindingFlags.Public);

                harmony.Patch(method,
                    prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                    finalizer: finalizer != null ? new HarmonyMethod(finalizer) : null);

                Log("Patches applied successfully.");
            }
            catch (Exception e)
            {
                Log($"Patching failed: {e}");
            }
        }

        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }
    }
}
