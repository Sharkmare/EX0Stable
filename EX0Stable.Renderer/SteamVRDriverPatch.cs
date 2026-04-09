// Patch: SteamVRDriver.OnDeviceConnected (renderer process)
//
// Controllers arriving over Steam Link initially report Role=Invalid.
// The renderer tries to register them immediately and crashes because
// the controller type is not yet known. The corrected role arrives in
// a subsequent TrackedDeviceRoleChanged event.
//
// Prefix: skip controllers with Invalid role. They will be processed
// when the role-changed event fires with the real role.
// Finalizer: catch any other crash so the renderer process survives.

using System;
using Valve.VR;

namespace EX0Stable.Renderer
{
    public static class Patches
    {
        public static bool OnDeviceConnected_Prefix(int index, bool connected)
        {
            if (!connected) return true;

            try
            {
                var system = OpenVR.System;
                if (system == null) return true;

                var deviceClass = system.GetTrackedDeviceClass((uint)index);

                if (deviceClass == ETrackedDeviceClass.Controller)
                {
                    var role = system.GetControllerRoleForTrackedDeviceIndex((uint)index);
                    if (role == ETrackedControllerRole.Invalid)
                    {
                        Doorstop.Entrypoint.Log(
                            $"Blocked device {index}: Controller with Role=Invalid, deferring.");
                        return false;
                    }
                }
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
