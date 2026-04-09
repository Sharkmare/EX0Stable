// Patch: ViveHandTrackingDriver.UpdateInputs
// GetBodyNode(Head) returns null during controller hotplug. The original
// code dereferences it unconditionally, causing NullReferenceException.
// This prefix skips the method when the head node is not ready.

using HarmonyLib;
using Elements.Core;
using Renderite.Shared;
using FrooxEngine;

namespace EX0Stable;

[HarmonyPatch(typeof(ViveHandTrackingDriver), "UpdateInputs")]
public static class ViveHandTrackingPatch
{
    static bool Prefix(ViveHandTrackingDriver __instance)
    {
        try
        {
            var inputField = AccessTools.Field(typeof(ViveHandTrackingDriver), "inputInterface");
            if (inputField == null) return true;

            var inputInterface = inputField.GetValue(__instance) as InputInterface;
            if (inputInterface == null) return false;

            var bodyNode = inputInterface.GetBodyNode(BodyNode.Head);
            if (bodyNode == null || !bodyNode.IsTracking)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }
}
