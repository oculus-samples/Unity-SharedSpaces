#if PHOTON_VOICE_FMOD_ENABLE
using System;
using System.Collections.Generic;
using FMODLib = FMOD;

namespace Photon.Voice.FMOD
{
    public class AudioInEnumerator : IDeviceEnumerator
    {
        const int NAME_MAX_LENGTH = 1000;
        const string LOG_PREFIX = "[PV] [FMOD] AudioInEnumerator: ";

        private DeviceInfo[] devices = new DeviceInfo[0];
        ILogger logger;

        public AudioInEnumerator(ILogger logger)
        {
            this.logger = logger;
            Refresh();
        }

        public bool IsSupported => true;

        public IEnumerable<DeviceInfo> Devices
        {
            get
            {
                return devices;
            }
        }

        public string Error { get; private set; }

        public void Refresh()
        {            
            FMODLib.RESULT res = FMODUnity.RuntimeManager.CoreSystem.getRecordNumDrivers(out int numDriv, out int numCon);
            if (res != FMODLib.RESULT.OK)
            {
                Error = "failed to getRecordNumDrivers: " + res;
                logger.LogError(LOG_PREFIX + Error);
                return;
            }

            devices = new DeviceInfo[numDriv];
            for (int id = 0; id < numDriv; id++)
            {
                res = FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(id, out string name, NAME_MAX_LENGTH, out Guid guid, out int systemRate, out FMODLib.SPEAKERMODE speakerMode, out int speakerModeChannels, out FMODLib.DRIVER_STATE state);
                if (res != FMODLib.RESULT.OK)
                {
                    Error = "failed to getRecordDriverInfo: " + res;
                    logger.LogError(LOG_PREFIX + Error);
                    return;
                }
                devices[id] = new DeviceInfo(id, name);
            }
        }

        public void Dispose()
        {
        }
    }
}
#endif