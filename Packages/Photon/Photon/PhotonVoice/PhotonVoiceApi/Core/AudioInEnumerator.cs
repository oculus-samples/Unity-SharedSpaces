
#if (UNITY_IOS && !UNITY_EDITOR) || __IOS__
#define DLL_IMPORT_INTERNAL
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Photon.Voice
{
    public struct DeviceInfo
    {
        public DeviceInfo(int id, string name)
        {
            IDInt = id;
            IDString = "";
            Name = name;
            useStringID = false;
        }
        public DeviceInfo(string id, string name)
        {
            IDInt = 0;
            IDString = id;
            Name = name;
            useStringID = true;
        }
        public int IDInt { get; private set; }
        public string IDString { get; private set; }
        public string Name { get; private set; }
        private bool useStringID;

        public static bool operator ==(DeviceInfo d1, DeviceInfo d2)
        {
            return d1.Equals(d2);
        }
        public static bool operator !=(DeviceInfo d1, DeviceInfo d2)
        {
            return !d1.Equals(d2);
        }

        // trivial implementation to avoid warnings CS0660 and CS0661 about missing overrides when == and != defined 
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (useStringID)
            {
                return string.Format("{0} ({1})", Name, IDString);
            }
            else 
            {
                return string.Format("{0} ({1})", Name, IDInt);
            }
        }

        // default device id may differ on different platform, use this platform value instead of Default.Int
        public static readonly DeviceInfo Default = new DeviceInfo(-128, "[Default Device]");
    }

    public interface IDeviceEnumerator : IDisposable
    {
        bool IsSupported { get; }
        IEnumerable<DeviceInfo> Devices { get; }
        void Refresh();
        string Error { get; }
    }

    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        private Type type;
        public MonoPInvokeCallbackAttribute(Type t) { type = t; }
    }
    /// <summary>Enumerates microphones available on device.
    /// </summary>
    public class AudioInEnumerator : IDeviceEnumerator
    {
#if WINDOWS_UWP || ENABLE_WINMD_SUPPORT
        private DeviceInfo[] devices = new DeviceInfo[0];

        public bool IsSupported => true;

        public AudioInEnumerator(ILogger logger)
        {
            Refresh();
            if (Error != null)
            {
                logger.LogError("[PV] AudioInEnumerator: " + Error);
            }
        }

        public IEnumerable<DeviceInfo> Devices
        {
            get
            {
                return devices;
            }
        }
        public void Refresh()
        {
            var op = Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.AudioCapture);
            op.AsTask().Wait();
            if (op.Status == Windows.Foundation.AsyncStatus.Error)
            {
                Error = op.ErrorCode.Message;
                return;
            }            
            var r = op.GetResults();
            devices = new DeviceInfo[r.Count];
            for (int i = 0; i < r.Count; i++)
            {
                devices[i] = new DeviceInfo(r[i].Id, r[i].Name);
            }
        }
        public string Error { get; private set; }

        public void Dispose()
        {
        }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

#if DLL_IMPORT_INTERNAL
	    const string lib_name = "__Internal";
#else
        const string lib_name = "AudioIn";
#endif
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Audio_In_CreateMicEnumerator();
        [DllImport(lib_name)]
        private static extern void Photon_Audio_In_DestroyMicEnumerator(IntPtr handle);
        [DllImport(lib_name)]
        private static extern int Photon_Audio_In_MicEnumerator_Count(IntPtr handle);
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Audio_In_MicEnumerator_NameAtIndex(IntPtr handle, int idx);
        [DllImport(lib_name)]
        private static extern int Photon_Audio_In_MicEnumerator_IDAtIndex(IntPtr handle, int idx);

        IntPtr handle;
        private DeviceInfo[] devices = new DeviceInfo[0];
        public AudioInEnumerator(ILogger logger)
        {
            Refresh();
            if (Error != null)
            {
                logger.LogError("[PV] AudioInEnumerator: " + Error);
            }
        }

        /// <summary>Refreshes the microphones list.
        /// </summary>
        public void Refresh()
        {
            Dispose();
            try
            {
                handle = Photon_Audio_In_CreateMicEnumerator();
                var count = Photon_Audio_In_MicEnumerator_Count(handle);
                devices = new DeviceInfo[count];
                for (int i = 0; i < count; i++)
                {
                    devices[i] = new DeviceInfo(Photon_Audio_In_MicEnumerator_IDAtIndex(handle, i), Marshal.PtrToStringAuto(Photon_Audio_In_MicEnumerator_NameAtIndex(handle, i)));
                }
                Error = null;
            }
            catch(Exception e)
            {
                Error = e.ToString();
                if (Error == null) // should never happen but since Error used as validity flag, make sure that it's not null
                {
                    Error = "Exception in AudioInEnumerator.Refresh()";
                }
            }
        }

        /// <summary>True if enumeration supported for the current platform.</summary>
        public bool IsSupported => true;

        /// <summary>If not null, the enumerator is in invalid state.</summary>
        public string Error { get; private set; }

        /// <summary>Returns the list of (name, id) pairs for available microphones.</summary>
        public IEnumerable<DeviceInfo> Devices
        {
            get
            {
                return devices;
            }            
        }

        /// <summary>Disposes enumerator.
        /// Call it to free native resources.
        /// </summary>
        public void Dispose()
        {
            if (handle != IntPtr.Zero && Error == null)
            {
                Photon_Audio_In_DestroyMicEnumerator(handle);
                handle = IntPtr.Zero;
            }
        }
#else
        public bool IsSupported => false;

        public AudioInEnumerator(ILogger logger)
        {
        }

        public IEnumerable<DeviceInfo> Devices
        {
            get { return System.Linq.Enumerable.Empty<DeviceInfo>(); }
        }

        public void Refresh()
        {
        }

        public string Error { get { return "Current platform " + UnityEngine.Application.platform + " is not supported by AudioInEnumerator."; } }

        public void Dispose()
        {
        }
#endif
    }

    public class AudioInChangeNotifier : IDisposable
    {
#if DLL_IMPORT_INTERNAL
        const string lib_name = "__Internal";
#else
        const string lib_name = "AudioIn";
#endif
#if (UNITY_IOS && !UNITY_EDITOR)
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Audio_In_CreateChangeNotifier(int instanceID, Action<int> callback);
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Audio_In_DestroyChangeNotifier(IntPtr handle);

        private delegate void CallbackDelegate(int instanceID);

        IntPtr handle;
        int instanceID;
        Action callback;

        public AudioInChangeNotifier(Action callback, ILogger logger)
        {
            this.callback = callback;
            var handle = Photon_Audio_In_CreateChangeNotifier(instanceCnt, nativeCallback);
            lock (instancePerHandle)
            {
                this.handle = handle;
                this.instanceID = instanceCnt;
                instancePerHandle.Add(instanceCnt++, this);
            }
        }

        // IL2CPP does not support marshaling delegates that point to instance methods to native code.
        // Using static method and per instance table.
        static int instanceCnt;
        private static Dictionary<int, AudioInChangeNotifier> instancePerHandle = new Dictionary<int, AudioInChangeNotifier>();
        [MonoPInvokeCallbackAttribute(typeof(CallbackDelegate))]
        private static void nativeCallback(int instanceID)
        {
            AudioInChangeNotifier instance;
            bool ok;
            lock (instancePerHandle)
            {
                ok = instancePerHandle.TryGetValue(instanceID, out instance);
            }
            if (ok)
            {
                instance.callback();
            }
        }

        /// <summary>True if enumeration supported for the current platform.</summary>
        public readonly bool IsSupported = true;

        /// <summary>If not null, the enumerator is in invalid state.</summary>
        public string Error { get; private set; }

        /// <summary>Disposes enumerator.
        /// Call it to free native resources.
        /// </summary>
        public void Dispose()
        {
            lock (instancePerHandle)
            {
                instancePerHandle.Remove(instanceID);
            }
            if (handle != IntPtr.Zero)
            {
                Photon_Audio_In_DestroyChangeNotifier(handle);
                handle = IntPtr.Zero;
            }
        }
#else
        public readonly bool IsSupported = false;

        public AudioInChangeNotifier(Action callback, ILogger logger)
        {
        }

        public string Error { get { return "Current platform " + "is not supported by AudioInChangeNotifier."; } }

        public void Dispose()
        {
        }
#endif
    }
}
