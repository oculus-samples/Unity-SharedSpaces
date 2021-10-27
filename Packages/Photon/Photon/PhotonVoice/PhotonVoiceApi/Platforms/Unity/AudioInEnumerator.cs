using UnityEngine;
using System;
using System.Linq;

namespace Photon.Voice.Unity
{
    public class AudioInEnumerator : IDeviceEnumerator
    {
        private DeviceInfo[] devices = new DeviceInfo[0];
        
        public AudioInEnumerator(ILogger logger)
        {
            Refresh();
        }

        public System.Collections.Generic.IEnumerable<DeviceInfo> Devices
        {
            get
            {
                return devices;
            }
        }

        public void Refresh()
        {
            var unityDevs = UnityMicrophone.devices;
            devices = new DeviceInfo[unityDevs.Length];
            for (int i = 0; i < devices.Length; i++)
            {
                var d = unityDevs[i];
                devices[i] = new DeviceInfo(d, d);
            }
        }

#if UNITY_WEBGL
        public bool IsSupported => false;
        
        public string Error { get { return "Current platform " + Application.platform + " is not supported by AudioInEnumerator."; } }
#else
        public bool IsSupported => true;

        public string Error { get { return null; } }
        #endif

        public void Dispose()
        {
        }
    }
}