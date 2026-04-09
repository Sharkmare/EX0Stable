// Patches for VR_Manager: catch-all finalizer on Update() and
// hand state validation on HandleHand().
//
// VR_Manager.Update has no exception handling. Any null ref, bad cast,
// or array bounds error from a single frame of bad VR input data kills
// the whole application. The finalizer catches and logs instead.
//
// HandleHand indexes into segmentPositions/segmentRotations without
// bounds checks. During hand/controller transitions the arrays can be
// short or null. The prefix validates before letting the original run.

using System;
using HarmonyLib;
using FrooxEngine;
using Renderite.Shared;

namespace EX0Stable;

[HarmonyPatch(typeof(VR_Manager), "Update")]
public static class VRManagerUpdatePatch
{
    static Exception? Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Elements.Core.UniLog.Warning(
                $"[EX0Stable] VR_Manager.Update recovered: " +
                $"{__exception.GetType().Name}: {__exception.Message}");
        }
        return null;
    }
}

[HarmonyPatch(typeof(VR_Manager), "HandleHand")]
public static class VRManagerHandleHandPatch
{
    static bool Prefix(HandState handState)
    {
        try
        {
            if (handState == null) return false;

            if (handState.isTracking)
            {
                if (handState.segmentPositions == null || handState.segmentPositions.Count < 20)
                    return false;
                if (handState.segmentRotations == null || handState.segmentRotations.Count < 20)
                    return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
}
