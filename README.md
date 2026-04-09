# EX0Stable

Fixes Resonite renderer crashes when controllers are hotplugged, especially over Steam Link wireless.

## The Problem

Resonite's renderer process crashes when controllers connect via vrlink with `Role: Invalid` (common over Steam Link due to latency). The native `GetSerialNumber()` call on a not-yet-ready vrlink device corrupts the heap, killing the renderer.

## Root Cause

`SteamVRDriver.OnDeviceConnected` defers controllers with `Role: Invalid` to a list for later processing. But the deferral line calls `GetSerialNumber(index)`, which queries OpenVR native properties on a device that isn't ready yet. This corrupts the native heap and crashes the renderer with `STATUS_HEAP_CORRUPTION` (0xC0000374).

Resonite 2026.4.9.1303 moved two other property queries out of the Invalid path but missed this one.

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

- **Prefix**: When a controller arrives with `Role=Invalid`, skips the original method entirely. Adds the device to the deferred list with a placeholder serial instead of calling `GetSerialNumber()`. When the role becomes valid, `OnNewPoses` re-triggers `OnDeviceConnected` and the original code runs the native queries safely.
- **Finalizer**: Catches any other unhandled exception so the renderer process survives.

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
