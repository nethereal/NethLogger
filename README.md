# NethLogger for SimplePlanes 2
**Version 7.3.0 - Performance Optimized**

![Dashboard Preview](pics/dashboard_preview.png)

A high-performance, telemetry extraction suite and aerospace dashboard for SimplePlanes 2, designed for synthetic environment integration and advanced flight logging.

## 🚀 Features
- **Telemetry Engine**: Fast extraction pipeline with zero-allocation optimizations.
- **Advanced Aerodynamics**: Real-time extraction of Mach Number, Dynamic Pressure (Q), TAS, and Speed of Sound.
- **Precision Instrumentation**: High-fidelity data for IAS, MSL/AGL Altitude, VSpeed, G-Force, AoA, and Slip.
- **Performance Optimized**: Reflection caching and memory management to ensure zero impact on game simulation.
- **Full System State**: Monitoring for Engine RPMs, Activation Groups, Fuel Flow, Mass, and Damage states.
- **Omniscient Dashboard**: PowerShell-based real-time telemetry visualizer with packet loss tracking.

## 🛠️ Installation

### 1. Requirements
- **BepInEx 6.0 (Mono)**: Required for plugin loading.
- **PowerShell 5.1+**: Required for the dashboard and build scripts.
- **Unity Doorstop**: The DLL proxy (`winhttp.dll`) required for non-intrusive loading.

### 2. Compilation
Open a command prompt in your **SimplePlanes 2 application directory** and run the build script:
```powershell
.\NethLogger\build.ps1
```
This will generate `NethTelemetry.dll` and place it in the `NethLogger\BepInEx\plugins` folder automatically.

### 3. Setup (Non-Intrusive)
To enable the loader without modifying original game files:
1. Copy the `doorstop_config.ini` from the `NethLogger` folder into your game root directory (or update the `dll_search_path_override` in your existing config to include `NethLogger\BepInEx\core`).
2. Ensure `winhttp.dll` (Doorstop) is present in the game root directory.
3. The mod will now automatically load from the `NethLogger` subfolder on next launch.

### 4. Usage
1. Launch **SimplePlanes 2**.
2. Enter the Flight Scene.
3. Run the dashboard from your game root:
```powershell
.\NethLogger\listener.ps1
```

## 📡 Telemetry Protocol
Data is broadcast over UDP to `127.0.0.1:5555` as a pipe-delimited (`|`) string containing 47 fields:

1. **Header**: PacketID, Name, Timestamp
2. **Core Flight**: IAS, TAS, Mach, Alt, AGL, VSpd, Pitch, Roll, Hdg, G, AoA, Slip
3. **Physics Vectors**: Position (X,Y,Z), Velocity (X,Y,Z), Acceleration (X,Y,Z)
4. **Rotational Physics**: AngularVelocity (X,Y,Z), AngularAcceleration (X,Y,Z), P/R/Y Rates
5. **Inputs**: Throttle, Pitch, Roll, Yaw, Brake, Flaps, Trim, Gear, PBrake, VTOL
6. **Systems**: AG Bitmask (8-char), Fuel, FuelCapacity, FuelFlow, Mass, Damage, CritDmg
7. **Engines**: RPM 1-4
8. **Environment**: AirDensity, Temperature, SOS, DynamicPressure, TotalDrag

---

## 📚 Credits & Sources
- **BepInEx 6.0**: Plugin framework. [GitHub Repository](https://github.com/BepInEx/BepInEx)
- **Unity Doorstop**: DLL proxy for non-intrusive loading. [GitHub Repository](https://github.com/NeighTools/UnityDoorstop)