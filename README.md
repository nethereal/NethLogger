# NethLogger for SimplePlanes 2
**Version 2.7.1**

A high-precision, 25Hz UDP telemetry system and aerospace dashboard for SimplePlanes 2.

## 🚀 Features
- **High-Fidelity Telemetry**: 25Hz stream of position, attitude, and physics data.
- **XML Mass Master**: Synchronizes with your craft's design blueprints for perfect weight accuracy.
- **Navigator Dashboard**: Real-time PowerShell instrumentation showing TAS, AGL, Heading, and G-Load.
- **Physics Insight**: Visualizes raw Forward, Up, and Right orientation vectors.

## 🛠️ Installation

### 1. Requirements
- **Doorstop Loader**: Included in the `Neth` folder for non-intrusive loading.
- **PowerShell**: To run the dashboard and build scripts.

### 2. Compilation
Run the provided build script to compile the DLL:
```powershell
.\build.ps1
```
This will generate `NethTelemetry.dll` and place it in the `NethLogger\BepInEx\plugins` folder.

### 3. Setup (Non-Intrusive)
To enable the loader without modifying game files:
1. Copy the `doorstop_config.ini` from `NethLogger` to the game root.
2. Ensure `winhttp.dll` (Doorstop) is present in the game root.
3. The mod will now load from the `NethLogger` subfolder automatically.

### 4. Usage
1. Launch **SimplePlanes 2**.
2. Enter the Flight Scene and load your aircraft.
3. Run the dashboard:
```powershell
.\listener.ps1
```

## 📡 Telemetry Protocol
Data is broadcast over UDP to `127.0.0.1:13434` in the following format:
`NAV|POS|ATT|NAV_DATA|VECTORS|MASS|G`

- **POS**: World coordinates (X, Y, Z)
- **ATT**: Euler angles (Pitch, Yaw, Roll)
- **NAV_DATA**: True Airspeed (MPH) and Altitude (AGL)
- **VECTORS**: Forward, Up, and Right unit vectors
- **MASS**: Total aircraft mass (kg) from XML specs
- **G**: Real-time G-Load

---
*Created by Antigravity for Nethereal.*

## 📚 Credits & Sources
This project utilizes the following open-source tools:
- **BepInEx 6.0 (Mono)**: The plugin framework used to hook into Unity. [GitHub Repository](https://github.com/BepInEx/BepInEx)
- **Unity Doorstop**: The DLL proxy used for non-intrusive loading (`winhttp.dll`). [GitHub Repository](https://github.com/NeighTools/UnityDoorstop)
