# NethLogger for SimplePlanes 2
Version 7.3.0

![Dashboard Preview](pics/dashboard_preview.png)

A telemetry extraction suite and dashboard for SimplePlanes 2, designed for flight logging and systems integration.

## Features
- **Telemetry Engine**: Extraction pipeline with reflection caching and optimized memory management.
- **Aerodynamic Data**: Extraction of Mach Number, Dynamic Pressure (Q), True Airspeed (TAS), and local Speed of Sound.
- **Flight Instrumentation**: Data for IAS, MSL/AGL Altitude, Vertical Speed, G-Force, Angle of Attack, and Slip.
- **System Monitoring**: Access to Engine RPM, Activation Group states, Fuel Flow, Mass, and Damage status.
- **Data Dashboard**: PowerShell-based utility for real-time data visualization and packet loss tracking.

## Installation

### 1. Requirements
- **BepInEx 6.0 (Mono)**: Required for plugin loading.
- **PowerShell 5.1+**: Required for the dashboard and build scripts.
- **Unity Doorstop**: The DLL proxy (`winhttp.dll`) required for non-intrusive loading.

### 2. Compilation
Open a command prompt in the **SimplePlanes 2 application directory** and run the build script:
```powershell
.\NethLogger\build.ps1
```
This will generate `NethTelemetry.dll` and place it in the `NethLogger\BepInEx\plugins` directory.

### 3. Setup (Non-Intrusive)
To enable the loader:
1. Copy `doorstop_config.ini` from the `NethLogger` folder to the game root directory (or update the `dll_search_path_override` in an existing configuration to include `NethLogger\BepInEx\core`).
2. Ensure `winhttp.dll` (Doorstop) is present in the game root directory.
3. The mod will load from the `NethLogger` subfolder upon application launch.

### 4. Usage
1. Launch **SimplePlanes 2**.
2. Enter the Flight Scene.
3. Run a dashboard from the game root:
   * **PowerShell**: `.\NethLogger\listener.ps1`
   * **Python**: `python .\NethLogger\listener.py`

## Telemetry Protocol
Data is broadcast over UDP to `127.0.0.1:5555` as a pipe-delimited (`|`) string containing 47 fields:

1. **Header**: PacketID, Name, Timestamp
2. **Core Flight**: IAS, TAS, Mach, Alt, AGL, VSpd, Pitch, Roll, Hdg, G, AoA, Slip
3. **Physics Vectors**: Position (X,Y,Z), Velocity (X,Y,Z), Acceleration (X,Y,Z)
4. **Rotational Physics**: AngularVelocity (X,Y,Z), AngularAcceleration (X,Y,Z), P/R/Y Rates
5. **Inputs**: Throttle, Pitch, Roll, Yaw, Brake, Flaps, Trim, Gear, PBrake, VTOL
6. **Systems**: AG Bitmask (8-char), Fuel, FuelCapacity, FuelFlow, Mass, Damage, CritDmg
7. **Engines**: RPM 1-4
8. **Environment**: AirDensity, Temperature, Speed of Sound, Dynamic Pressure, Total Drag

## Credits
- **BepInEx 6.0**: Plugin framework. [GitHub Repository](https://github.com/BepInEx/BepInEx)
- **Unity Doorstop**: DLL proxy for non-intrusive loading. [GitHub Repository](https://github.com/NeighTools/UnityDoorstop)