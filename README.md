# EX0Stable

Resonite mod that fixes controller hotplug crashes, especially over Steam Link wireless.

## The Problem

Resonite crashes when:
- Controllers are turned on after Resonite has launched
- Swapping between hand tracking and controllers
- Using Steam Link wireless streaming (higher latency causes race conditions)

## Root Cause

Three bugs across two processes:

**Renderer process** (`Renderite.Renderer.exe`):
- `SteamVRDriver.OnDeviceConnected` receives controllers with `Role: Invalid` before the role assignment event arrives. It tries to register them immediately and crashes.

**Engine process** (`FrooxEngine`):
- `ViveHandTrackingDriver.UpdateInputs` dereferences `GetBodyNode(Head)` without a null check. During hotplug the head node can be null.
- `VR_Manager.Update` has no exception handling. A single bad frame of input data crashes the application.
- `VR_Manager.HandleHand` indexes into segment arrays without bounds checks. During hand/controller transitions the arrays can be short or null.

## Install

### Engine patch (ResoniteModLoader)

Requires [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).

Copy `EX0Stable.dll` to `Resonite/rml_mods/`.

### Renderer patch (Unity Doorstop)

1. Copy `winhttp.dll` and `doorstop_config.ini` to `Resonite/Renderer/`
2. Create `Resonite/Renderer/EX0Stable/` folder
3. Copy `EX0Stable.Renderer.dll` and `0Harmony.dll` into that folder

Get `winhttp.dll` from [Unity Doorstop releases](https://github.com/NeighTools/UnityDoorstop/releases) (x64 build).

### File layout after install

```
Resonite/
  rml_mods/
    EX0Stable.dll
  Renderer/
    winhttp.dll
    doorstop_config.ini
    EX0Stable/
      EX0Stable.Renderer.dll
      0Harmony.dll
```

## Build

Requires .NET SDK. Resonite must be installed at the default Steam path.

```bash
# Engine patch
cd EX0Stable
dotnet build -c Release

# Renderer patch
cd EX0Stable.Renderer
dotnet build -c Release
```

The engine patch auto-deploys to `rml_mods/` on build.
The renderer patch outputs to `bin/Release/` and must be copied manually.

## Patches

| Patch | Process | Target | Fix |
|-------|---------|--------|-----|
| ViveHandTrackingPatch | Engine | `ViveHandTrackingDriver.UpdateInputs` | Skip when Head body node is null |
| VRManagerUpdatePatch | Engine | `VR_Manager.Update` | Catch-all finalizer prevents crash |
| VRManagerHandleHandPatch | Engine | `VR_Manager.HandleHand` | Validate segment array bounds |
| OnDeviceConnectedPatch | Renderer | `SteamVRDriver.OnDeviceConnected` | Block controllers with Role=Invalid |

## Logs

- Engine: check Resonite's main log for `[EX0Stable]` entries
- Renderer: `Resonite/Renderer/EX0Stable/EX0Stable.Renderer.log`

## License

MIT
