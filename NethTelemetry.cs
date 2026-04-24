using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace Neth.SimplePlanes2
{
    [BepInPlugin("com.neth.telemetry", "Neth Telemetry", "2.7.1")]
    public class TelemetryPlugin : BaseUnityPlugin
    {
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private float _lastLogTime = 0f;
        private float _lastHookTime = 0f;
        private const float LogInterval = 0.04f; // 25Hz
        
        private GameObject _cachedCraft;
        private Rigidbody _cachedRb;
        
        private float _xmlMass = 0f;
        private Vector3 _lastVelocity = Vector3.zero;
        private float _gForce = 1.0f;
        
        private Component _atmosphere;
        private PropertyInfo _aglProp;
        private PropertyInfo _tasProp;

        void Awake()
        {
            Application.logMessageReceived += HandleLog;
            try
            {
                _udpClient = new UdpClient();
                _remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 13434);
                Logger.LogInfo("NETH TELEMETRY 2.7.1 - HIGH PRECISION ACTIVE");
            }
            catch (Exception e)
            {
                Logger.LogError("UDP Error: " + e.Message);
            }
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (condition.Contains("loading craft"))
            {
                try {
                    string[] parts = condition.Split('\'');
                    if (parts.Length >= 4) LoadMassFromXml(parts[3]);
                } catch {}
            }
        }

        private void LoadMassFromXml(string craftName)
        {
            try {
                string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Jundroo", "SimplePlanes 2", "Crafts");
                string xmlPath = Path.Combine(localLow, craftName + ".xml");
                if (File.Exists(xmlPath)) {
                    XDocument doc = XDocument.Load(xmlPath);
                    var specs = doc.Descendants("Specifications").FirstOrDefault();
                    if (specs != null && specs.Attribute("LoadedWeight") != null) {
                        _xmlMass = float.Parse(specs.Attribute("LoadedWeight").Value);
                    }
                }
            } catch {}
        }

        void Update()
        {
            if (Time.time - _lastLogTime >= LogInterval)
            {
                _lastLogTime = Time.time;
                StreamTelemetry();
            }

            if (_cachedCraft != null && Time.time - _lastHookTime > 5.0f)
            {
                _lastHookTime = Time.time;
                HookSystems();
            }
        }

        private void StreamTelemetry()
        {
            if (_udpClient == null) return;

            if (_cachedCraft == null || !_cachedCraft.activeInHierarchy)
            {
                _cachedCraft = GameObject.Find("RootNWHVehicleController");
                if (_cachedCraft != null) {
                    _cachedRb = _cachedCraft.GetComponentInParent<Rigidbody>();
                    HookSystems();
                }
            }

            if (_cachedCraft != null && _cachedRb != null)
            {
                Vector3 pos = _cachedCraft.transform.position;
                Vector3 rot = _cachedCraft.transform.eulerAngles;
                Vector3 vel = _cachedRb.velocity;
                
                Vector3 fwd = _cachedCraft.transform.forward;
                Vector3 up = _cachedCraft.transform.up;
                Vector3 right = _cachedCraft.transform.right;

                float speed = vel.magnitude * 2.23694f;
                float alt = pos.y * 3.28084f;
                if (_atmosphere != null) {
                    try {
                        speed = (float)_tasProp.GetValue(_atmosphere, null) * 2.23694f;
                        alt = (float)_aglProp.GetValue(_atmosphere, null) * 3.28084f;
                    } catch {}
                }

                Vector3 accel = (vel - _lastVelocity) / LogInterval;
                _lastVelocity = vel;
                _gForce = (accel.magnitude / 9.81f) + 1.0f;

                // Increased precision to F2
                string data = string.Format("NAV|{0:F2},{1:F2},{2:F2}|{3:F2},{4:F2},{5:F2}|{6:F2},{7:F2}|{8:F2},{9:F2},{10:F2};{11:F2},{12:F2},{13:F2};{14:F2},{15:F2},{16:F2}|{17:F2}|{18:F2}", 
                    pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, speed, alt,
                    fwd.x, fwd.y, fwd.z, up.x, up.y, up.z, right.x, right.y, right.z,
                    _xmlMass, _gForce
                );
                
                SendData(data);
            }
        }

        private void HookSystems()
        {
            if (_cachedCraft == null) return;
            var comps = _cachedCraft.GetComponentsInChildren<Component>(true);
            foreach (var c in comps) {
                if (c == null) continue;
                if (c.GetType().FullName.Contains("AtmosphereScript")) {
                    _atmosphere = c;
                    _aglProp = c.GetType().GetProperty("AltitudeAboveGroundLevel", BindingFlags.Public | BindingFlags.Instance);
                    _tasProp = c.GetType().GetProperty("TrueAirspeed", BindingFlags.Public | BindingFlags.Instance);
                }
            }
        }

        private void SendData(string data)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                _udpClient.Send(bytes, bytes.Length, _remoteEndPoint);
            }
            catch {}
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
            if (_udpClient != null) _udpClient.Close();
        }
    }
}
