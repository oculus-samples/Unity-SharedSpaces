#if !UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID || UNITY_WSA)
#define WEBRTC_AUDIO_DSP_SUPPORTED_PLATFORM
#endif

#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
#define WEBRTC_AUDIO_DSP_SUPPORTED_EDITOR
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Voice.Unity
{
    [RequireComponent(typeof(Recorder))]
    [DisallowMultipleComponent]
    public class WebRtcAudioDsp : VoiceComponent
    {
        #region Private Fields

        [SerializeField]
        private bool aec = true;
        
        [SerializeField]
        private bool aecHighPass;

        [SerializeField]
        private bool agc = true;

        [SerializeField]
        private int agcCompressionGain = 9;

        [SerializeField]
        private bool vad = true;

        [SerializeField]
        private bool highPass;

        [SerializeField]
        private bool bypass;

        [SerializeField]
        private bool noiseSuppression;

        [SerializeField]
        private int reverseStreamDelayMs = 120;

        private int reverseChannels;
        private WebRTCAudioProcessor proc;

        private AudioOutCapture ac;
        private bool started;

        private static readonly Dictionary<AudioSpeakerMode, int> channelsMap = new Dictionary<AudioSpeakerMode, int>
        {
            #if !UNITY_2019_2_OR_NEWER
            {AudioSpeakerMode.Raw, 0},
            #endif
            {AudioSpeakerMode.Mono, 1},
            {AudioSpeakerMode.Stereo, 2},
            {AudioSpeakerMode.Quad, 4},
            {AudioSpeakerMode.Surround, 5},
            {AudioSpeakerMode.Mode5point1, 6},
            {AudioSpeakerMode.Mode7point1, 8},
            {AudioSpeakerMode.Prologic, 2}
        };

        private LocalVoice localVoice;
        private int outputSampleRate;

        private Recorder recorder;

        [SerializeField]
        private bool forceNormalAecInMobile;

        #endregion

        #region Properties

        public bool AEC
        {
            get { return this.aec; }
            set
            {
                if (value == this.aec)
                {
                    return;
                }
                this.aec = value;
                if (this.proc != null)
                {
                    this.proc.AEC = this.aec;
                    this.SetOutputListener();
                }
            }
        }

        [Obsolete("Use AEC instead on all platforms, internally according AEC will be used either mobile or not.")]
        public bool AECMobile // echo control mobile
        {
            get { return this.AEC; }
            set
            {
                this.AEC = value;
            }
        }

        [Obsolete("Obsolete as it's not recommended to set this to true. https://forum.photonengine.com/discussion/comment/48017/#Comment_48017")]
        public bool AECMobileComfortNoise;

        public bool AecHighPass
        {
            get { return this.aecHighPass; }
            set
            {
                if (value == this.aecHighPass)
                {
                    return;
                }
                this.aecHighPass = value;
                if (this.proc != null)
                {
                    this.proc.AECHighPass = this.aecHighPass;
                }
            }
        }

        public int ReverseStreamDelayMs
        {
            get { return this.reverseStreamDelayMs; }
            set
            {
                if (this.reverseStreamDelayMs == value)
                {
                    return;
                }
                this.reverseStreamDelayMs = value;
                if (this.proc != null)
                {
                    this.proc.AECStreamDelayMs = this.reverseStreamDelayMs;
                } 
            }
        }

        public bool NoiseSuppression
        {
            get { return this.noiseSuppression; }
            set
            {
                if (value == this.noiseSuppression)
                {
                    return;
                }
                this.noiseSuppression = value;
                if (this.proc != null)
                {
                    this.proc.NoiseSuppression = this.noiseSuppression;
                }
            }
        }

        public bool HighPass
        {
            get { return this.highPass; }
            set
            {
                if (value == this.highPass)
                {
                    return;
                }
                this.highPass = value;
                if (this.proc != null)
                {
                    this.proc.HighPass = this.highPass;
                }
            }
        }

        public bool Bypass
        {
            get { return this.bypass; }
            set
            {
                if (value == this.bypass)
                {
                    return;
                }
                this.bypass = value;
                if (this.proc != null)
                {
                    this.proc.Bypass = this.bypass;
                }
            }
        }

        public bool AGC
        {
            get { return this.agc; }
            set
            {
                if (value == this.agc)
                {
                    return;
                }
                this.agc = value;
                if (this.proc != null)
                {
                    this.proc.AGC = this.agc;
                }
            }
        }

        public int AgcCompressionGain
        {
            get
            {
                return this.agcCompressionGain;
            }
            set
            {
                if (this.agcCompressionGain == value)
                {
                    return;
                }
                if (value < 0 || value > 90)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("AgcCompressionGain value {0} not in range [0..90]", value);
                    }
                    return;
                }
                this.agcCompressionGain = value;
                if (this.proc != null)
                {
                    this.proc.AGCCompressionGain = this.agcCompressionGain;
                }
            }
        }

        public bool VAD
        {
            get { return this.vad; }
            set
            {
                if (value == this.vad)
                {
                    return;
                }
                this.vad = value;
                if (this.proc != null)
                {
                    this.proc.VAD = this.vad;
                }
            }
        }

        public bool ForceNormalAecInMobile
        {
            get { return this.forceNormalAecInMobile; }
            set { this.forceNormalAecInMobile = value; }
        }

        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            if (this.SupportedPlatformCheck())
            {
                this.recorder = this.GetComponent<Recorder>();
                if (this.recorder == null)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("A Recorder component needs to be attached to the same GameObject");
                    }
                    this.enabled = false;
                    return;
                }
                if (!this.IgnoreGlobalLogLevel)
                {
                    this.LogLevel = this.recorder.LogLevel;
                }
                AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
                AudioListener audioListener = null;
                for(int i=0; i < audioListeners.Length; i++)
                {
                    if (audioListeners[i].gameObject.activeInHierarchy && audioListeners[i].enabled)
                    {
                        audioListener = audioListeners[i];
                        break;
                    }
                }
                if (!this.SetOrSwitchAudioListener(audioListener, false))
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("AudioListener and AudioOutCapture components are required");
                    }
                    this.enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            if (this.SupportedPlatformCheck() && this.recorder.IsRecording && this.proc == null)
            {
                if (this.Logger.IsWarningEnabled)
                {
                    this.Logger.LogWarning("WebRtcAudioDsp is added after recording has started, restarting recording to take effect");
                }
                this.recorder.RestartRecording(true);
                this.SetOutputListener();
            }
        }

        private void OnDisable()
        {
            this.SetOutputListener(false);
        }

        private bool SupportedPlatformCheck()
        {
            #if WEBRTC_AUDIO_DSP_SUPPORTED_PLATFORM
            return true;
            #elif WEBRTC_AUDIO_DSP_SUPPORTED_EDITOR
            if (this.Logger.IsWarningEnabled)
            {
                this.Logger.LogWarning("WebRtcAudioDsp is not supported on this target platform {0}. The component will be disabled in build.", CurrentPlatform);
            }
            return true;
            #else
            if (this.Logger.IsErrorEnabled)
            {
                this.Logger.LogError("WebRtcAudioDsp is not supported on this platform {0}. The component will be disabled.", CurrentPlatform);
            }
            this.enabled = false;
            return false;
            #endif
        }

        private void SetOutputListener()
        {
            this.SetOutputListener(this.AEC);
        }

        private void SetOutputListener(bool on)
        {
            if (this.ac != null && this.started != on && this.proc != null)
            {
                if (on)
                {
                    this.started = true;
                    this.ac.OnAudioFrame += this.OnAudioOutFrameFloat;
                }
                else
                {
                    this.started = false;
                    this.ac.OnAudioFrame -= this.OnAudioOutFrameFloat;
                }
            }
        }

        private void OnAudioOutFrameFloat(float[] data, int outChannels)
        {
            if (outChannels != this.reverseChannels)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("OnAudioOutFrame channel count {0} != initialized {1}.  Switching channels and restarting.", outChannels, this.reverseChannels);
                }
                this.reverseChannels = outChannels;
                this.Restart();
            }
            this.proc.OnAudioOutFrameFloat(data);
        }

        // Message sent by Recorder
        private void PhotonVoiceCreated(PhotonVoiceCreatedParams p)
        {
            if (!this.enabled)
            {
                return;
            }
            if (this.recorder != null && this.recorder.SourceType != Recorder.InputSourceType.Microphone)
            {
                if (this.Logger.IsWarningEnabled)
                {
                    this.Logger.LogWarning("WebRtcAudioDsp is better suited to be used with Microphone as Recorder Input Source Type.");
                }
            }
            this.localVoice = p.Voice;
            if (this.localVoice.Info.Channels != 1)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("Only mono audio signals supported.");
                }
                this.enabled = false;
                return;
            }
            if (!(this.localVoice is LocalVoiceAudioShort))
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("Only short audio voice supported.");
                }
                this.enabled = false;
                return;
            }

            // can't access the AudioSettings properties in InitAEC if it's called from not main thread
            this.reverseChannels = channelsMap[AudioSettings.speakerMode];
            this.outputSampleRate = AudioSettings.outputSampleRate;
            this.Init();
            LocalVoiceAudioShort v = this.localVoice as LocalVoiceAudioShort;
            v.AddPostProcessor(this.proc);
            this.SetOutputListener();
            if (this.Logger.IsInfoEnabled)
            {
                this.Logger.LogInfo("Initialized");
            }
        }

        private void PhotonVoiceRemoved()
        {
            this.Reset();
        }

        private void OnDestroy()
        {
            this.Reset();
        }

        private void Reset()
        {
            this.SetOutputListener(false);
            if (this.proc != null)
            {
                this.proc.Dispose();
                this.proc = null;
            }
        }

        private void Restart()
        {
            this.Reset();
            this.Init();
            this.SetOutputListener();
        }

        private void Init()
        {
            this.proc = new WebRTCAudioProcessor(this.Logger, this.localVoice.Info.FrameSize, this.localVoice.Info.SamplingRate,
                this.localVoice.Info.Channels, this.outputSampleRate, this.reverseChannels);
            #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            this.proc.AEC = this.AEC && this.ForceNormalAecInMobile;
            this.proc.AECMobile = this.AEC && !this.ForceNormalAecInMobile;
            #else
            this.proc.AEC = this.AEC;
            this.proc.AECMobile = false;
            #endif
            this.proc.AECStreamDelayMs = this.ReverseStreamDelayMs;
            this.proc.AECHighPass = this.AecHighPass;
            this.proc.HighPass = this.HighPass;
            this.proc.NoiseSuppression = this.NoiseSuppression;
            this.proc.AGC = this.AGC;
            this.proc.AGCCompressionGain = this.AgcCompressionGain;
            this.proc.VAD = this.VAD;
            this.proc.Bypass = this.Bypass;
        }

        private bool SetOrSwitchAudioListener(AudioListener audioListener, bool extraChecks)
        {
            if (audioListener == null)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("audioListener passed is null or is being destroyed");
                }
                return false;
            }
            if (extraChecks)
            {
                if (!audioListener.gameObject.activeInHierarchy)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The GameObject to which the audioListener is attached is not active in hierarchy");
                    }
                    return false;
                }
                if (!audioListener.enabled)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("audioListener passed is disabled");
                    }
                    return false;
                }
            }
            AudioOutCapture audioOutCapture = audioListener.GetComponent<AudioOutCapture>();
            if (audioOutCapture == null)
            {
                audioOutCapture = audioListener.gameObject.AddComponent<AudioOutCapture>();
            }
            return this.SetOrSwitchAudioOutCapture(audioOutCapture, false);
        }


        private bool SetOrSwitchAudioOutCapture(AudioOutCapture audioOutCapture, bool extraChecks)
        {
            if (audioOutCapture == null)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("audioOutCapture passed is null or is being destroyed");
                }
                return false;
            }
            if (!audioOutCapture.enabled)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("audioOutCapture passed is disabled");
                }
                return false;
            }
            if (extraChecks)
            {
                if (!audioOutCapture.gameObject.activeInHierarchy)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The GameObject to which the audioOutCapture is attached is not active in hierarchy");
                    }
                    return false;
                }
                AudioListener audioListener = audioOutCapture.GetComponent<AudioListener>();
                if (audioListener == null)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The AudioListener attached to the same GameObject as the audioOutCapture is null or being destroyed");
                    }
                    return false;
                }
                if (!audioListener.enabled)
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The AudioListener attached to the same GameObject as the audioOutCapture is disabled");
                    }
                    return false;
                }
            }
            if (this.ac != null)
            {
                if (this.ac != audioOutCapture)
                {
                    if (this.started)
                    {
                        this.ac.OnAudioFrame -= this.OnAudioOutFrameFloat;
                    }
                }
                else
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The same audioOutCapture is being used already");
                    }
                    return false;
                }
            }
            this.ac = audioOutCapture;
            if (this.started)
            {
                this.ac.OnAudioFrame += this.OnAudioOutFrameFloat;
            }
            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the AudioListener to be used with this WebRtcAudioDsp
        /// </summary>
        /// <param name="audioListener">The audioListener to be used</param>
        /// <returns>Success or failure</returns>
        public bool SetOrSwitchAudioListener(AudioListener audioListener)
        {
            return this.SetOrSwitchAudioListener(audioListener, true);
        }

        /// <summary>
        /// Set the AudioOutCapture to be used with this WebRtcAudioDsp
        /// </summary>
        /// <param name="audioOutCapture">The audioOutCapture to be used</param>
        /// <returns>Success or failure</returns>
        public bool SetOrSwitchAudioOutCapture(AudioOutCapture audioOutCapture)
        {
            return this.SetOrSwitchAudioOutCapture(audioOutCapture, true);
        }

        #endregion
    }
}