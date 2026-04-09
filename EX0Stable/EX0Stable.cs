// EX0Stable - Fix VR controller hotplug crashes in Resonite.
// FrooxEngine-side patches (loaded via ResoniteModLoader).

using System.Reflection;
using HarmonyLib;
using ResoniteModLoader;

namespace EX0Stable;

public class EX0Stable : ResoniteMod
{
    public override string Name    => "EX0Stable";
    public override string Author  => "Sharkmare";
    public override string Version => "1.0.0";
    public override string Link    => "";

    private const string HARMONY_ID = "dev.ex0.stable";

    public override void OnEngineInit()
    {
        Msg("Patching VR input crash paths...");
        new Harmony(HARMONY_ID).PatchAll(Assembly.GetExecutingAssembly());
        Msg("EX0Stable active.");
    }
}
