// Patch: SteamVRDriver.OnDeviceConnected (renderer process)
//
// Heap corruption crash when controllers arrive via vrlink with Role=Invalid.
// Resonite 2026.4.9.1303 moved render model/serial queries out of the Invalid
// path, but left GetSerialNumber() in the invalidRoleControllers.Add() call.
// That native OpenVR property query on a not-yet-ready vrlink device corrupts
// the heap.
//
// Fix: skip all native property queries when role is Invalid. Store a placeholder
// serial derived from the device index. The serial is only used as a dictionary
// key when the device gets demoted to a tracker (both controller slots full), so
// a unique placeholder is safe. When re-processed with a valid role, the serial
// is fetched fresh by the original code path.

using System;
using System.Reflection;
using HarmonyLib;
using Valve.VR;

namespace EX0Stable.Renderer
{
    public static class Patches
    {
        public static bool OnDeviceConnected_Prefix(object __instance, int index, bool connected)
        {
            if (!connected) return true;

            try
            {
                var system = OpenVR.System;
                if (system == null) return true;

                var deviceClass = system.GetTrackedDeviceClass((uint)index);
                if (deviceClass != ETrackedDeviceClass.Controller) return true;

                var role = system.GetControllerRoleForTrackedDeviceIndex((uint)index);
                if (role != ETrackedControllerRole.Invalid) return true;

                // Check if already mapped
                var mappedField = __instance.GetType().GetField("mappedControllers",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (mappedField != null)
                {
                    var mapped = mappedField.GetValue(__instance);
                    var containsKey = mapped?.GetType().GetMethod("ContainsKey");
                    if (containsKey != null && (bool)containsKey.Invoke(mapped, new object[] { index }))
                        return false;
                }

                // Add to invalidRoleControllers with a placeholder serial.
                // Avoids calling GetSerialNumber which triggers heap corruption
                // on not-yet-ready vrlink devices.
                var invalidField = __instance.GetType().GetField("invalidRoleControllers",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (invalidField != null)
                {
                    var list = invalidField.GetValue(__instance);
                    var addMethod = list.GetType().GetMethod("Add");
                    var deviceType = __instance.GetType().GetNestedType("InvalidRoleDevice",
                        BindingFlags.NonPublic);
                    if (deviceType != null && addMethod != null)
                    {
                        var ctor = deviceType.GetConstructor(new[] { typeof(int), typeof(string) });
                        if (ctor != null)
                        {
                            var device = ctor.Invoke(new object[] { index, $"deferred_{index}" });
                            addMethod.Invoke(list, new[] { device });
                        }
                    }
                }

                Doorstop.Entrypoint.Log(
                    $"Deferred device {index}: Controller with Role=Invalid, skipped native property queries.");
                return false;
            }
            catch (Exception e)
            {
                Doorstop.Entrypoint.Log($"Prefix error device {index}: {e.Message}");
            }

            return true;
        }

        public static Exception OnDeviceConnected_Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Doorstop.Entrypoint.Log(
                    $"Caught crash in OnDeviceConnected: " +
                    $"{__exception.GetType().Name}: {__exception.Message}");
            }
            return null;
        }
    }
}
