using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Neth.SimplePlanes2
{
    [BepInPlugin("com.neth.telemetry", "Neth Telemetry", "7.3.0")]
    public class TelemetryPlugin : BaseUnityPlugin
    {
        private float _nextUpdate = 0;
        private const float UPDATE_INTERVAL = 0.01f; // 100Hz
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;

        private Component _cachedAircraftCore;
        private string _cachedName;
        private float _nextSearch = 0;
        private Vector3 _lastAngularVel = Vector3.zero;
        private ulong _packetId = 0;
        private float _lastFuel = 0f;

        // Cached references (avoid per-frame lookups)
        private Rigidbody _cachedRigidbody;

        // Reflection cache: Type -> (memberName -> delegate)
        // We cache MemberInfo per (Type, name) to avoid repeated GetProperty/GetField calls
        private Dictionary<Type, Dictionary<string, MemberInfo>> _memberCache = new Dictionary<Type, Dictionary<string, MemberInfo>>();

        // Pre-allocated StringBuilder to avoid GC pressure
        private StringBuilder _sb = new StringBuilder(512);

        // Pre-allocated char array for AG bitmask
        private char[] _agChars = new char[] { '0','0','0','0','0','0','0','0' };

        void Awake()
        {
            try {
                _udpClient = new UdpClient();
                _remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 5555);
                Logger.LogInfo("NETH TELEMETRY 7.3.0 - PERF OPTIMIZED");
            } catch (Exception e) {
                Logger.LogError("UDP Init Error: " + e.Message);
            }
        }

        void Update()
        {
            if (Time.time >= _nextUpdate) {
                _nextUpdate = Time.time + UPDATE_INTERVAL;
                ProcessTelemetry();
            }
        }

        private void ProcessTelemetry()
        {
            try {
                if (_cachedAircraftCore == null || Time.time >= _nextSearch) {
                    FindLocalAircraft();
                    _nextSearch = Time.time + 2.0f;
                }

                if (_cachedAircraftCore != null) {
                    var controls = GetPropObj(_cachedAircraftCore, "Controls");

                    _sb.Length = 0; // Reset without reallocation

                    // [0] PacketID, [1] Name, [2] Timestamp
                    _sb.Append(_packetId++).Append("|").Append(_cachedName).Append("|").Append(Time.time.ToString("F3")).Append("|");

                    // [3-10] Core Flight
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "IAS"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Altitude"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "PitchAngle"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "RollAngle"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Heading"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "GForce"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "AngleOfAttack"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "AngleOfSlip"))).Append("|");

                    // [11-13] Linear Vectors
                    Vector3 pos = GetPropV3(_cachedAircraftCore, "Position");
                    Vector3 vel = GetPropV3(_cachedAircraftCore, "Velocity");
                    Vector3 acc = GetPropV3(_cachedAircraftCore, "Acceleration");
                    _sb.Append(FmtV3(pos)).Append("|");
                    _sb.Append(FmtV3(vel)).Append("|");
                    _sb.Append(FmtV3(acc)).Append("|");

                    // [14-16] Rotational Physics
                    Vector3 angVel = GetPropV3(_cachedAircraftCore, "AngularVelocity");
                    Vector3 angAcc = (angVel - _lastAngularVel) / UPDATE_INTERVAL;
                    _lastAngularVel = angVel;
                    _sb.Append(FmtV3P(angVel)).Append("|");
                    _sb.Append(FmtV3P(angAcc)).Append("|");
                    _sb.AppendFormat("{0:F1},{1:F1},{2:F1}",
                        GetPropF(_cachedAircraftCore, "PitchRate"),
                        GetPropF(_cachedAircraftCore, "RollRate"),
                        GetPropF(_cachedAircraftCore, "YawRate")).Append("|");

                    // [17-23] Inputs
                    _sb.Append(FmtF2(GetPropF(controls, "Throttle"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Pitch"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Roll"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Yaw"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Brake"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Flaps"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(controls, "Trim"))).Append("|");

                    // [24] Activation Groups (8-char bitmask, zero-alloc)
                    if (controls != null) {
                        for (int i = 0; i < 8; i++) {
                            _agChars[i] = GetPropBool(controls, "Activate" + (i + 1)) ? '1' : '0';
                        }
                    } else {
                        for (int i = 0; i < 8; i++) _agChars[i] = '0';
                    }
                    _sb.Append(_agChars).Append("|");

                    // [25-29] Vehicle Systems (Fuel, Mass, Damage, Drag)
                    _sb.Append(FmtF2(GetPropF(_cachedAircraftCore, "Fuel"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(_cachedAircraftCore, "FuelCapacity"))).Append("|");
                    if (_cachedRigidbody != null) {
                        _sb.Append(FmtF(_cachedRigidbody.mass)).Append("|");
                    } else {
                        var com = GetPropObj(_cachedAircraftCore, "CenterOfMass");
                        _sb.Append(com != null ? FmtF(GetPropF(com, "LoadedMass") * 1000f) : "0.000").Append("|");
                    }
                    _sb.Append(FmtF2(GetPropF(_cachedAircraftCore, "Damage"))).Append("|");
                    _sb.Append(FmtF2(GetPropF(_cachedAircraftCore, "TotalDragForceMagnitude"))).Append("|");

                    // [30-33] Engines
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Rpm1"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Rpm2"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Rpm3"))).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "Rpm4"))).Append("|");

                    // [34-35] Environment
                    var atmo = GetPropObj(_cachedAircraftCore, "AtmosphereSample");
                    _sb.Append(atmo != null ? FmtF2(GetPropF(atmo, "AirDensity")) : "0.000").Append("|");
                    _sb.Append(atmo != null ? FmtF2(GetPropF(atmo, "Temperature")) : "0.000").Append("|");

                    // [36-38] Extra Controls
                    if (controls != null) {
                        _sb.Append(GetPropBool(controls, "LandingGearDown") ? "1|" : "0|");
                        _sb.Append(GetPropBool(controls, "ParkingBrake") ? "1|" : "0|");
                        _sb.Append(FmtF2(GetPropF(controls, "Vtol"))).Append("|");
                    } else {
                        _sb.Append("0|0|0.000|");
                    }

                    // [39-46] Advanced Telemetry (TAS, Mach, AGL, VSpd, FuelFlow, CritDmg, SOS, DynPress)
                    float tas = vel.magnitude;
                    float sos = atmo != null ? GetPropF(atmo, "SpeedOfSound") : 343f;
                    float airDens = atmo != null ? GetPropF(atmo, "AirDensity") : 1.225f;
                    float mach = sos > 0 ? tas / sos : 0f;
                    float vSpeed = vel.y;
                    float dynPress = 0.5f * airDens * tas * tas;
                    float currentFuel = GetPropF(_cachedAircraftCore, "Fuel");
                    float fuelFlow = (currentFuel < _lastFuel) ? (_lastFuel - currentFuel) / UPDATE_INTERVAL : 0f;
                    _lastFuel = currentFuel;

                    _sb.Append(FmtF(tas)).Append("|");
                    _sb.Append(FmtF(mach)).Append("|");
                    _sb.Append(FmtF(GetPropF(_cachedAircraftCore, "AltitudeAgl"))).Append("|");
                    _sb.Append(FmtF(vSpeed)).Append("|");
                    _sb.Append(FmtF(fuelFlow)).Append("|");
                    _sb.Append(GetPropBool(_cachedAircraftCore, "CriticallyDamaged") ? "1|" : "0|");
                    _sb.Append(FmtF(sos)).Append("|");
                    _sb.Append(FmtF(dynPress));

                    SendUdp(_sb.ToString());
                }
            } catch {
                _cachedAircraftCore = null;
                _cachedRigidbody = null;
                _memberCache.Clear();
            }
        }

        // Formatters
        private string FmtF(float v) { return v.ToString("F3"); }
        private string FmtF2(float v) { return v.ToString("F3"); }
        private string FmtV3(Vector3 v) { return string.Format("{0:F3},{1:F3},{2:F3}", v.x, v.y, v.z); }
        private string FmtV3P(Vector3 v) { return string.Format("{0:F3},{1:F3},{2:F3}", v.x, v.y, v.z); }

        private void FindLocalAircraft()
        {
            try {
                var allComps = Resources.FindObjectsOfTypeAll<Component>();
                var netAircrafts = allComps.Where(c => c != null && c.GetType().FullName == "Assets.Scripts.Multiplayer.NetworkAircraftScript").ToList();
                foreach (var netAc in netAircrafts) {
                    if (netAc == null || netAc.gameObject == null) continue;
                    if (!netAc.gameObject.name.Contains("LocalPlayer") && netAircrafts.Count > 1) continue;
                    var aircraftScriptProp = netAc.GetType().GetProperty("AircraftScript");
                    if (aircraftScriptProp == null) continue;
                    var aircraftScript = aircraftScriptProp.GetValue(netAc) as Component;
                    if (aircraftScript != null) {
                        _cachedAircraftCore = aircraftScript;
                        _cachedName = netAc.gameObject.name;
                        _cachedRigidbody = _cachedAircraftCore.GetComponentInParent<Rigidbody>();
                        _memberCache.Clear(); // New aircraft, clear reflection cache
                        return;
                    }
                }
            } catch { }
        }

        // --- Cached reflection accessors ---

        private MemberInfo GetCachedMember(object comp, string name)
        {
            if (comp == null) return null;
            Type t = comp.GetType();

            Dictionary<string, MemberInfo> typeCache;
            if (!_memberCache.TryGetValue(t, out typeCache)) {
                typeCache = new Dictionary<string, MemberInfo>();
                _memberCache[t] = typeCache;
            }

            MemberInfo mi;
            if (!typeCache.TryGetValue(name, out mi)) {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null) { mi = p; }
                else {
                    var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) { mi = f; }
                }
                typeCache[name] = mi; // Cache even if null (means "not found")
            }
            return mi;
        }

        private object GetMemberValue(MemberInfo mi, object comp)
        {
            if (mi is PropertyInfo) return ((PropertyInfo)mi).GetValue(comp, null);
            if (mi is FieldInfo)    return ((FieldInfo)mi).GetValue(comp);
            return null;
        }

        private float GetPropF(object comp, string name) {
            if (comp == null) return 0f;
            try {
                var mi = GetCachedMember(comp, name);
                if (mi != null) return Convert.ToSingle(GetMemberValue(mi, comp));
            } catch { }
            return 0f;
        }

        private Vector3 GetPropV3(object comp, string name) {
            try {
                var mi = GetCachedMember(comp, name);
                if (mi != null) return (Vector3)GetMemberValue(mi, comp);
            } catch { }
            return Vector3.zero;
        }

        private object GetPropObj(object comp, string name) {
            try {
                var mi = GetCachedMember(comp, name);
                if (mi != null) return GetMemberValue(mi, comp);
            } catch { }
            return null;
        }

        private bool GetPropBool(object comp, string name) {
            if (comp == null) return false;
            try {
                var mi = GetCachedMember(comp, name);
                if (mi != null) return (bool)GetMemberValue(mi, comp);
            } catch { }
            return false;
        }

        private void SendUdp(string line) {
            try {
                byte[] data = Encoding.UTF8.GetBytes(line);
                _udpClient.Send(data, data.Length, _remoteEndPoint);
            } catch { }
        }

        void OnDestroy() {
            if (_udpClient != null) _udpClient.Close();
        }
    }
}
