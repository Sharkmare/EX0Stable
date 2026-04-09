# EX0Stable

Fixes Resonite crashes when controllers are hotplugged, especially over Steam Link wireless.

## The Problem

Resonite's renderer process crashes when:
- Controllers are turned on after Resonite has launched
- Swapping between hand tracking and controllers
- Using Steam Link wireless (latency causes the role assignment to arrive late)

## Root Cause

Controllers arriving over Steam Link initially report `Role: Invalid` to the renderer's `SteamVRDriver.OnDeviceConnected`. The renderer tries to register them immediately and crashes because the controller type is not yet known. The corrected role arrives in a subsequent `TrackedDeviceRoleChanged` event, but the renderer is already dead.

## Install

1. Download `winhttp.dll` (x64) from [Unity Doorstop releases](https://github.com/NeighTools/UnityDoorstop/releases)
2. Copy `winhttp.dll` and `doorstop_config.ini` to `Resonite/Renderer/`
3. Create `Resonite/Renderer/EX0Stable/` folder
4. Copy `EX0Stable.Renderer.dll` and `0Harmony.dll` into that folder

```
Resonite/
  Renderer/
    winhttp.dll
    doorstop_config.ini
    EX0Stable/
      EX0Stable.Renderer.dll
      0Harmony.dll
```

## What It Does

Patches `SteamVRDriver.OnDeviceConnected` at renderer startup via Harmony:

- **Prefix**: Skips controllers that arrive with `Role=Invalid`. They get processed when the role-changed event fires with the real role.
- **Finalizer**: Catches any other unhandled crash in the method so the renderer process survives.

## Build

Requires .NET SDK. Resonite must be installed at the default Steam path.

```bash
cd EX0Stable.Renderer
dotnet build -c Release
```

Output: `bin/Release/EX0Stable.Renderer.dll` + `bin/Release/0Harmony.dll`

## Logs

`Resonite/Renderer/EX0Stable/EX0Stable.Renderer.log`

## License

MIT
