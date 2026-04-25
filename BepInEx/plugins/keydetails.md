# SimplePlanes 2 — Craft Telemetry Key Details

> **Purpose:** Master reference for extracting real-time operational data from a player-controlled craft.
> Intended consumer: a synthetic-environment feed that needs the complete physical, logical, and input state of the aircraft every frame.

---

## 1. Runtime Environment

| Attribute | Value |
|---|---|
| Engine | Unity 6000.0.59f2 (Mono) |
| Injection | BepInEx 6.0.0-be.697 |
| Networking | FishNet (FishNetworking) |
| Input System | Rewired 1.1.59.1.U6000 |
| Compiler Limit | C# 5 (Framework `csc.exe`) |

---

## 2. Object Hierarchy — Finding the Player Craft

The player's aircraft lives under a fixed scene path:

```
/FlightScene/AircraftContainer/NetworkAircraft(Clone)
```

### Discovery Chain

```
Resources.FindObjectsOfTypeAll<Component>()
  -> filter: c.GetType().FullName == "Assets.Scripts.Multiplayer.NetworkAircraftScript"
  -> filter: gameObject.name.Contains("LocalPlayer")
```

### Layer Stack (all on the same GameObject)

| Layer | Class | Role |
|---|---|---|
| **Network Wrapper** | `Assets.Scripts.Multiplayer.NetworkAircraftScript` | FishNet sync, spawn ID, player link |
| **Core Flight** | `Assets.Scripts.Craft.AircraftScript` | Physics, telemetry, parts, controls |
| **Update Loop** | `Assets.Scripts.Craft.CraftUpdateScript` | Per-frame update scheduling |
| **Variable System** | `Assets.Scripts.Craft.VariableSystemScript` | Funky Trees user variables |
| **Powertrain** | `Assets.Scripts.Craft.CraftPowertrainScript` | Engine/motor management |

### Getting from Network to Core

```csharp
// NetworkAircraftScript has a property "AircraftScript" that returns AircraftScript
var prop = netAc.GetType().GetProperty("AircraftScript");
Component aircraftScript = prop.GetValue(netAc) as Component;
```

**PERFORMANCE WARNING:** `Resources.FindObjectsOfTypeAll<Component>()` is extremely expensive (scans all loaded objects). Cache the result and only re-search every 2+ seconds or when the reference becomes null.

---

## 3. Core Flight Telemetry — AircraftScript Properties

All values are accessed via reflection on the `AircraftScript` instance.
Source file: `CoreAircraftStructure.txt`

### 3.1 Flight Instruments (Single/float)

| Property | Type | Unit | Notes |
|---|---|---|---|
| `IAS` | Single | m/s | Indicated Airspeed |
| `TAS` | Single | m/s | True Airspeed |
| `AirSpeed` | Single | m/s | Raw airspeed (may differ from IAS) |
| `GS` | Single | m/s | Ground Speed |
| `Altitude` | Single | m | MSL altitude |
| `AltitudeAgl` | Single | m | Above Ground Level |
| `PitchAngle` | Single | degrees | Nose up/down |
| `RollAngle` | Single | degrees | Bank angle |
| `Heading` | Single | degrees | Compass heading (0-360) |
| `AngleOfAttack` | Single | degrees | AoA |
| `AngleOfSlip` | Single | degrees | Sideslip |
| `GForce` | Single | G | Total G-force |
| `VerticalG` | Single | G | Vertical component only |
| `PitchRate` | Single | deg/s | Pitch rotation rate |
| `RollRate` | Single | deg/s | Roll rotation rate |
| `YawRate` | Single | deg/s | Yaw rotation rate |
| `TimeSinceStart` | Single | s | Elapsed flight time |
| `Latitude` | Single | degrees | Geographic latitude |
| `Longitude` | Single | degrees | Geographic longitude |

### 3.2 Physics Vectors (Vector3)

| Property | Type | Notes |
|---|---|---|
| `Position` | Vector3 | World position (floating-origin adjusted) |
| `GlobalPosition` | Vector3 | Absolute world position |
| `Velocity` | Vector3 | Linear velocity vector |
| `Acceleration` | Vector3 | Linear acceleration vector |
| `AngularVelocity` | Vector3 | Rotational velocity (rad/s) |
| `Rotation` | Vector3 | Euler rotation |
| `WindVelocity` | Vector3 | Local wind vector |

**NOTE:** Angular Acceleration is not directly exposed. Calculate via `(angVel_current - angVel_previous) / dt` at your sample rate.

### 3.3 Physics Fields (private, accessed via reflection)

| Field | Type | Notes |
|---|---|---|
| `_acceleration` | Vector3 | Raw acceleration cache |
| `_aerodynamicCenter` | Vector3 | Aero center position |
| `_centerOfMassCalculatedForDebug` | Vector3 | Computed CoM |
| `_lastVelocity` | Vector3 | Previous-frame velocity |
| `_windSpeed` | Vector3 | Wind vector |
| `_currentWingSurfaceArea` | Single | Live wing area |
| `_initialWingSurfaceArea` | Single | Baseline wing area |

### 3.4 Fuel System

| Property/Field | Type | Notes |
|---|---|---|
| `Fuel` | Single | Current fuel amount |
| `FuelCapacity` | Single | Maximum fuel capacity |
| `FuelProportion` | Single | Fuel remaining as 0-1 fraction |
| `InitialFuel` | Single | Fuel at spawn |
| `InitialFuelCapacity` | Single | Capacity at spawn |
| `_fuelUsedInFrame` | Single | Fuel consumed this frame |
| `_fuelGainedInFrame` | Single | Fuel gained this frame (refueling) |
| `_fuelTanks` | List | Individual fuel tank objects |
| `_numActiveTanks` | Int32 | Count of active tanks |

### 3.5 Engine / Powertrain

| Property | Type | Notes |
|---|---|---|
| `Engines` | IReadOnlyList | List of engine objects |
| `Powertrain` | CraftPowertrainScript | Central engine management |
| `Rpm1` through `Rpm4` | Single | RPM for engines 1-4 |
| `InletAir` | InletAir | Air intake data |

### 3.6 Aerodynamic Structure

| Property/Field | Type | Notes |
|---|---|---|
| `Wings` | List | All wing/aerofoil objects |
| `TotalDragForceMagnitude` | Single | Total drag force magnitude |
| `CenterOfMass` | GroupCenterOfMass | Center of mass object |
| `InertiaTensorRecalculationEnabled` | Boolean | Whether inertia tensor is live |
| `_recalculateDrag` | Boolean | Drag recalc pending flag |

### 3.7 Damage and Structural

| Property | Type | Notes |
|---|---|---|
| `Damage` | Single | Damage fraction |
| `CriticallyDamaged` | Boolean | Critical damage flag |
| `CriticalDamageThreshold` | Single | Threshold for critical |
| `Parts` | List | All part objects |
| `Bodies` | List | Physics body groups |
| `WheelParts` | List | Wheel objects |

### 3.8 Targeting and Combat

| Property | Type | Notes |
|---|---|---|
| `SelectedWeaponName` | String | Current weapon name |
| `TargetLocked` | Boolean | Lock-on achieved |
| `TargetLocking` | Boolean | Lock-on in progress |
| `TargetSelected` | Boolean | Target designated |
| `TargetDistance` | Single | Distance to target (m) |
| `TargetElevation` | Single | Target elevation angle |
| `TargetHeading` | Single | Target bearing |
| `IRSignature` | Single | Infrared signature |
| `RadarSignature` | Single | Radar cross-section |
| `DisableBombs/Cannons/Guns/Missiles/Rockets` | Boolean | Weapon category toggles |

### 3.9 Status Flags

| Property | Type | Notes |
|---|---|---|
| `IsPrimaryLocalPlayer` | Boolean | Is this the player's main craft |
| `IsNonFlyableAircraft` | Boolean | Ground vehicle / boat |
| `IsConnectedToCatapult` | Boolean | Carrier catapult state |
| `RemoteAircraft` | Boolean | Is this a remote/AI craft |
| `IsInitialized` | Boolean | Craft fully loaded |
| `GenerationComplete` | Boolean | Mesh generation done |
| `RequiresFlapsSlider` | Boolean | Has flaps |
| `RequiresTrimSlider` | Boolean | Has trim |
| `RequiresVtolSlider` | Boolean | Has VTOL |
| `HasInputOverrides` | Boolean | Custom input overrides active |

---

## 4. Controls / Inputs — AircraftControls

Accessed via: `AircraftScript.Controls` (property, type `AircraftControls`)

```csharp
var controls = GetPropObj(aircraftScript, "Controls");
```

**WARNING:** `AircraftControls` is **not** a MonoBehaviour/Component -- it is a plain C# object. Use `object` type, not `Component`, when casting. All properties must be accessed via reflection on its `GetType()`.

### 4.1 Known Input Axes

These are the Funky Trees input names and the property names on `AircraftControls` to look for:

| Funky Trees Name | Expected Property | Range | Notes |
|---|---|---|---|
| `Throttle` | `Throttle` | 0 - 1 | Engine power |
| `Pitch` | `Pitch` | -1 - 1 | Pitch stick |
| `Roll` | `Roll` | -1 - 1 | Roll stick |
| `Yaw` | `Yaw` | -1 - 1 | Rudder pedals |
| `Brake` | `Brake` | 0 - 1 | Wheel brakes |
| `Trim` | `Trim` | -1 - 1 | Pitch trim |
| `VTOL` | `VTOL` | -1 - 1 | VTOL nozzle angle |
| `Flaps` | `Flaps` | 0 - 1 | Flap deployment |
| `LandingGear` | `LandingGear` | 0 / 1 | Gear state |
| `FireGuns` | `FireGuns` | 0 / 1 | Guns trigger |
| `FireWeapons` | `FireWeapons` | 0 / 1 | Weapons trigger |
| `LaunchCountermeasures` | `LaunchCountermeasures` | 0 / 1 | Countermeasures |

**NOTE:** A full structural dump of `AircraftControls` is still needed. Version 6.6.3 of the plugin was built to perform this dump -- check `LogOutput.log` for the `=== CONTROLS STRUCTURE DUMP ===` section after a fresh flight to discover the exact field/property names.

---

## 5. Activation Groups — The Unsolved Problem

### 5.1 What We Know

Activation Groups (`Activate1` through `Activate8`) are **not** stored in the `VariableSystemScript._nameLookup` dictionary. The key dump confirmed the dictionary only contains user-defined Funky Trees variables (like `RotorRPM`, `PitchPID`, etc.).

### 5.2 Where They Actually Live

Activation Groups are accessed through the **Funky Trees Expression Context** (`ExpressionContext`), which is a built-in evaluation engine. The variables `Activate1` through `Activate8` are hardcoded into this context, not stored as named variables in the dictionary.

#### Potential Extraction Paths (in priority order):

1. **ExpressionContext** -- `AircraftScript._expressionContext` (field, type `Context`)
   - This object has a method to evaluate expressions like `"Activate1"`.
   - Reflection approach: call the expression evaluator with the string `"Activate1"` and read the result.

2. **ActivationPanelScript** -- `Assets.Scripts.Flight.UI.ActivationPanelScript`
   - Found at scene path: `/UserInterface/FlightUI/FlightUIRoot/Empty(Clone)/Empty(Clone)`
   - This is the UI panel that displays the AG buttons. It likely holds the toggle state.
   - Structural dump needed to identify fields.

3. **Part-Level Scanning** -- Each part in `AircraftScript.Parts` may expose its activation group membership as a property.

### 5.3 Recommended Next Step

Deploy a plugin version that:
```csharp
// Option A: Use the expression context to evaluate AG variables
var ctx = GetPropObj(aircraftScript, "ExpressionContext");
// Then call ctx.Evaluate("Activate1") or similar

// Option B: Dump the ActivationPanelScript structure
var panels = Resources.FindObjectsOfTypeAll<Component>()
    .Where(c => c.GetType().FullName == "Assets.Scripts.Flight.UI.ActivationPanelScript");
// Dump all fields/properties

// Option C: Check AircraftControls for AG data
// The Controls dump (v6.6.3) should reveal if AG state lives here
```

---

## 6. Variable System — Funky Trees Custom Variables

Accessed via: `AircraftScript.VariableSystem` (property, type `VariableSystemScript`)

### 6.1 Internal Architecture

```
VariableSystemScript
  |-- _nameLookup : Dictionary<string, int>   <-- key to index map
  |-- Variables   : List<Variable>            <-- indexed variable objects
  |-- Setters     : List                      <-- variable setter definitions
  |-- OutputScripts : List                    <-- output script bindings
```

### 6.2 Reading a Variable by Name

```csharp
FieldInfo lookupField = sys.GetType().GetField("_nameLookup", BindingFlags.NonPublic | BindingFlags.Instance);
System.Collections.IDictionary lookup = (System.Collections.IDictionary)lookupField.GetValue(sys);

if (lookup.Contains("MyVarName")) {
    int index = (int)lookup["MyVarName"];
    PropertyInfo varsProp = sys.GetType().GetProperty("Variables", BindingFlags.Public | BindingFlags.Instance);
    System.Collections.IList varsList = (System.Collections.IList)varsProp.GetValue(sys);
    object variableObj = varsList[index];
    
    // Try "Value" or "_value" field on the Variable object
    FieldInfo valField = variableObj.GetType().GetField("Value", BindingFlags.Public | BindingFlags.Instance);
    if (valField == null)
        valField = variableObj.GetType().GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
    float result = (float)valField.GetValue(variableObj);
}
```

### 6.3 Known Variable Keys (from craft with PID controller)

These are **user-defined** Funky Trees variables, not built-in flight data:

| Key | Purpose |
|---|---|
| `RotorRPM` | Rotor speed variable |
| `PitchP`, `PitchI` | PID proportional/integral for pitch |
| `PitchError`, `PitchIntegral` | PID error tracking for pitch |
| `PitchPID` | Computed PID output for pitch |
| `RollP`, `RollI` | PID proportional/integral for roll |
| `RollError`, `RollIntegral` | PID error tracking for roll |
| `RollPID` | Computed PID output for roll |
| `HoverPitch` | Hover mode pitch target |
| `PitchFR/FL/BR/BL` | Per-motor pitch mixing |

**NOTE:** These keys vary per craft. A generic telemetry extractor should enumerate all keys via `_nameLookup.Keys`.

---

## 7. Atmosphere and Environment

Accessed via: `AircraftScript.AtmosphereSample` (property, type `AtmosphereSample`)

This object likely contains:
- Air density
- Air pressure
- Temperature
- Speed of sound (for Mach calculation)

**IMPORTANT:** Structural dump needed. Deploy a version that dumps `AtmosphereSample.GetType().GetProperties()` and `GetFields()`.

Also available:
- `AircraftScript.WindVelocity` (Vector3) -- local wind vector
- `_windSpeed` (field, Vector3) -- raw wind speed

---

## 8. Parts System

Accessed via: `AircraftScript.Parts` (property, type `List<PartScript>`)

Each part is a `PartScript` instance. The `Parts` list contains every component of the craft (wings, engines, fuselage, wheels, weapons, etc.).

### Key Part-Level Data to Extract (requires PartScript structural dump)

- Part type / name
- Part health / damage state
- Part position and rotation relative to the craft
- Activation group membership
- Whether the part is connected or disconnected

### Related Collections

| Property | Type | Notes |
|---|---|---|
| `Parts` | List | All parts |
| `Bodies` | List | Physics body groups (breakable sections) |
| `WheelParts` | List | Wheels only |
| `Wings` | List | Aerofoils only |
| `Engines` | IReadOnlyList | Engines only |
| `_fuelTanks` | List (field) | Fuel tanks only |

---

## 9. Network Layer — NetworkAircraftScript

Source file: `AircraftStructure.txt`

### Key Properties

| Property | Type | Notes |
|---|---|---|
| `AircraftScript` | AircraftScript | Core flight object |
| `Player` | FlightScenePlayer | Player who owns this craft |
| `PlayerId` | Int32 | Network player ID |
| `CraftXml` | XElement | Full craft XML definition |
| `IsInitialized` | Boolean | Fully loaded |
| `IsUnloaded` | Boolean | Craft despawned |
| `NetworkSpawnId` | UInt16 | Network object ID |
| `IsOwner` | Boolean | Local player owns this |
| `IsController` | Boolean | Local player controls this |

### Identifying the Local Player

```csharp
// Method 1: Name-based (reliable)
gameObject.name.Contains("LocalPlayer")

// Method 2: Property-based (more robust)
bool isOwner = GetPropBool(netAc, "IsOwner");
bool isPrimary = GetPropBool(aircraftScript, "IsPrimaryLocalPlayer");
```

---

## 10. Recommended Telemetry Packet — Complete Synthetic Environment Feed

Below is the recommended data structure for a complete craft state packet, covering all operational facts needed for a synthetic environment:

### Packet Layout

```
Section 1: Identity
  - CraftName (string)
  - TeamId (uint16)
  - TimeSinceStart (float)

Section 2: Flight Instruments
  - IAS, TAS, GS (m/s)
  - Altitude, AltitudeAgl (m)
  - PitchAngle, RollAngle, Heading (degrees)
  - AngleOfAttack, AngleOfSlip (degrees)
  - GForce, VerticalG (G)
  - PitchRate, RollRate, YawRate (deg/s)
  - Latitude, Longitude (degrees)

Section 3: Physics Vectors
  - Position (Vector3, world)
  - Velocity (Vector3, m/s)
  - Acceleration (Vector3, m/s2)
  - AngularVelocity (Vector3, rad/s)
  - AngularAcceleration (Vector3, rad/s2 -- derived)
  - Rotation (Vector3, euler degrees)
  - WindVelocity (Vector3, m/s)

Section 4: Input State
  - Throttle (0-1)
  - Pitch, Roll, Yaw (-1 to 1)
  - Brake (0-1)
  - Flaps (0-1)
  - Trim (-1 to 1)
  - VTOL (-1 to 1)
  - LandingGear (0/1)
  - FireGuns, FireWeapons (0/1)

Section 5: Activation Groups
  - AG1-AG8 (0/1 each)
  - (Requires ExpressionContext or ActivationPanelScript extraction)

Section 6: Fuel and Propulsion
  - Fuel, FuelCapacity, FuelProportion
  - Rpm1-Rpm4
  - _fuelUsedInFrame

Section 7: Targeting and Combat
  - SelectedWeaponName
  - TargetLocked, TargetLocking, TargetSelected
  - TargetDistance, TargetElevation, TargetHeading
  - IRSignature, RadarSignature

Section 8: Structural
  - Damage (float)
  - CriticallyDamaged (bool)
  - TotalDragForceMagnitude
  - Parts.Count
  - Bodies.Count

Section 9: Funky Trees Custom Variables
  - All keys from _nameLookup (dynamic per craft)
```

---

## 11. Still-Needed Structural Dumps

The following objects need their fields/properties dumped to complete the telemetry surface:

| Object | Access Path | Why |
|---|---|---|
| **AircraftControls** | `AircraftScript.Controls` | Exact input property names, AG storage |
| **AtmosphereSample** | `AircraftScript.AtmosphereSample` | Air density, pressure, temperature, Mach |
| **InstrumentData** | `AircraftScript.InstrumentData` | May contain pre-computed HUD values |
| **PartScript** (1 instance) | `AircraftScript.Parts[0]` | Part-level data (type, health, AG membership) |
| **ExpressionContext** | `AircraftScript._expressionContext` | Evaluate Activate1 through Activate8 |
| **ActivationPanelScript** | Scene search | AG toggle state |
| **CraftPowertrainScript** | `AircraftScript.Powertrain` | Detailed engine state |
| **GroupCenterOfMass** | `AircraftScript.CenterOfMass` | CoM position, mass data |

---

## 12. Extraction Patterns — C# Reflection Cookbook

### Read a float property
```csharp
float GetPropF(object comp, string name) {
    var p = comp.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (p != null) return Convert.ToSingle(p.GetValue(comp));
    var f = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) return Convert.ToSingle(f.GetValue(comp));
    return 0f;
}
```

### Read a Vector3
```csharp
Vector3 GetPropV3(object comp, string name) {
    var p = comp.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (p != null) return (Vector3)p.GetValue(comp);
    return Vector3.zero;
}
```

### Read a nested object
```csharp
object GetPropObj(object comp, string name) {
    var p = comp.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (p != null) return p.GetValue(comp);
    var f = comp.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (f != null) return f.GetValue(comp);
    return null;
}
```

### Cache PropertyInfo for performance
```csharp
// Cache once at discovery time
PropertyInfo _iasProp = aircraftScript.GetType().GetProperty("IAS");

// Then in hot loop:
float ias = (float)_iasProp.GetValue(aircraftScript);
```

**TIP:** For 100Hz extraction, cache all `PropertyInfo` objects at discovery time instead of calling `GetProperty()` every frame. This eliminates the overhead of repeated type reflection.

---

## 13. Known Issues and Gotchas

1. **Floating Origin**: SimplePlanes 2 uses a floating origin system. `Position` values jump when the origin shifts. Use `GlobalPosition` for absolute coordinates or track the floating origin offset.

2. **C# 5 Compiler**: The framework `csc.exe` only supports C# 5. No null-conditional (`?.`), no string interpolation, no expression-bodied members.

3. **Non-generic Collections**: When casting from reflection, use `System.Collections.IDictionary` and `System.Collections.IList` (non-generic), not the generic versions, because of namespace conflicts with `using System.Collections.Generic`.

4. **Controls is not a Component**: `AircraftControls` is a plain C# class, not a Unity MonoBehaviour. Cast to `object`, not `Component`.

5. **UDP Port**: The telemetry system broadcasts on `localhost:5555` via UDP. If a firewall blocks this, the listener will receive nothing.

6. **Scene Lifecycle**: The aircraft is not available until the flight scene loads and the craft is fully initialized (`IsInitialized == true`). Search loops should handle null gracefully.
