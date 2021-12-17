#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID || UNITY_WSA
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

        private AudioListener audioListener;
        private AudioOutCapture audioOutCapture;
        private bool aecStarted;
        private bool autoDestroyAudioOutCapture;

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

        [SerializeField]
        private bool aecOnlyWhenEnabled = true;

        #endregion

        #region Properties

        public bool AEC
        {
            get
            {
                if (this.IsInitialized && (!this.AecOnlyWhenEnabled || this.isActiveAndEnabled))
                {
                    return this.aecStarted;
                }
                return this.aec;
            }
            set
            {
                if (value == this.aec)
                {
                    return;
                }
                this.aec = value;
                this.ToggleAec();
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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
                if (this.IsInitialized)
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

        public bool IsInitialized
        {
            get
            {
                return this.proc != null;
            }
        }

        public bool AecOnlyWhenEnabled
        {
            get
            {
                return this.aecOnlyWhenEnabled;
            }
            set
            {
                if (this.aecOnlyWhenEnabled != value)
                {
                    this.aecOnlyWhenEnabled = value;
                    this.ToggleAec();
                }
            }
        }

        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            if (this.SupportedPlatformCheck())
            {
                this.recorder = this.GetComponent<Recorder>();
                if (ReferenceEquals(null, this.recorder) || !this.recorder)
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
            }
        }

        private void OnEnable()
        {
            if (this.SupportedPlatformCheck())
            {
                if (this.IsInitialized)
                {
                    this.ToggleAec();
                } 
                else if (this.recorder.IsRecording)
                {
                    if (this.Logger.IsWarningEnabled)
                    {
                        this.Logger.LogWarning("WebRtcAudioDsp is added after recording has started, restarting recording to take effect");
                    }
                    this.recorder.RestartRecording(true);
                }
            }
        }

        private void OnDisable()
        {
            if (this.AecOnlyWhenEnabled && this.aecStarted)
            {
               this.ToggleAecOutputListener(false);
            }
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

        private void ToggleAec()
        {
            if (this.IsInitialized && (!this.AecOnlyWhenEnabled || this.isActiveAndEnabled) && this.aec != this.aecStarted)
            {
                if (this.Logger.IsDebugEnabled)
                {
                    this.Logger.LogDebug("Toggling AEC to {0}", this.aec);
                }
                if (!this.ToggleAecOutputListener(this.aec))
                {
                    if (this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("AEC failed to be toggled to {0}", this.aec);
                    }
                }
                else if (this.Logger.IsDebugEnabled)
                {
                    this.Logger.LogDebug("AEC successfully toggled to {0}", this.aec);
                }
            }
        }

        private bool ToggleAecOutputListener(bool on)
        {
            if (on != this.aecStarted)
            {
                if (on)
                {
                    if (this.AecOnlyWhenEnabled && !this.isActiveAndEnabled)
                    {
                        if (this.Logger.IsErrorEnabled)
                        {
                            this.Logger.LogError("Could not start AEC because AecOnlyWhenEnabled is true and isActiveAndEnabled is false");
                        }
                        return false;
                    }
                    if (ReferenceEquals(null, this.audioOutCapture) || !this.audioOutCapture)
                    {
                        if (!this.InitAudioOutCapture())
                        {
                            if (this.Logger.IsErrorEnabled)
                            {
                                this.Logger.LogError("Could not start AEC OutputListener because a valid AudioOutCapture could not be set.");
                            }
                            return false;
                        }
                    }
                    else
                    {
                        if (!this.AudioOutCaptureChecks(this.audioOutCapture, true))
                        {
                            if (this.Logger.IsErrorEnabled)
                            {
                                this.Logger.LogError("Could not start AEC OutputListener because AudioOutCapture provided is not valid.");
                            }
                            return false;
                        }
                        AudioListener listener = this.audioOutCapture.GetComponent<AudioListener>();
                        if (this.audioListener != listener)
                        {
                            if (this.Logger.IsWarningEnabled)
                            {
                                this.Logger.LogWarning("Unexpected: AudioListener changed but AudioOutCapture did not.");
                            }
                            this.audioListener = listener;
                        }
                    }
                    if (this.IsInitialized) 
                    {
                        this.proc.AECStreamDelayMs = this.ReverseStreamDelayMs;
                        this.proc.AECHighPass = this.AecHighPass;
                        #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                        this.proc.AEC = this.ForceNormalAecInMobile;
                        this.proc.AECMobile = !this.ForceNormalAecInMobile;
                        #else
                        this.proc.AEC = true;
                        this.proc.AECMobile = false;
                        #endif
                        this.aecStarted = true;
                        this.audioOutCapture.OnAudioFrame += this.OnAudioOutFrameFloat;
                        if (this.Logger.IsDebugEnabled)
                        {
                            this.Logger.LogDebug("AEC OutputListener started.");
                        }
                    }
                }
                else 
                {
                    if (this.UnsubscribeFromAudioOutCapture(this.autoDestroyAudioOutCapture))
                    {
                        if (this.Logger.IsDebugEnabled)
                        {
                            this.Logger.LogDebug("AEC OutputListener stopped.");
                        }
                    }
                    else if (this.Logger.IsWarningEnabled)
                    {
                        this.Logger.LogWarning("Unexpected: AudioOutCapture is null but aecStarted == true");
                    }
                    if (this.IsInitialized)
                    {
                        this.proc.AEC = false;
                        this.proc.AECMobile = false;
                    }
                    else if (this.Logger.IsWarningEnabled)
                    {
                        this.Logger.LogWarning("Unexpected: proc is null but aecStarted was true.");
                    }
                    this.aecStarted = false;
                }
                return true;
            }
            return false;
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
            this.ToggleAec();
        }

        private void PhotonVoiceRemoved()
        {
            this.StopAllProcessing();
        }

        private void OnDestroy()
        {
            this.StopAllProcessing();
        }

        private void StopAllProcessing()
        {
            this.ToggleAecOutputListener(false);
            if (this.IsInitialized)
            {
                this.proc.Dispose();
                this.proc = null;
            }
        }

        private void Restart()
        {
            this.StopAllProcessing();
            this.Init();
            this.ToggleAec();
        }

        private void Init()
        {
            this.proc = new WebRTCAudioProcessor(this.Logger, this.localVoice.Info.FrameSize, this.localVoice.Info.SamplingRate,
                this.localVoice.Info.Channels, this.outputSampleRate, this.reverseChannels);
            this.proc.HighPass = this.HighPass;
            this.proc.NoiseSuppression = this.NoiseSuppression;
            this.proc.AGC = this.AGC;
            this.proc.AGCCompressionGain = this.AgcCompressionGain;
            this.proc.VAD = this.VAD;
            this.proc.Bypass = this.Bypass;
            if (this.Logger.IsInfoEnabled)
            {
                this.Logger.LogInfo("Initialized");
            }
        }

        private bool SetOrSwitchAudioListener(AudioListener listener, bool extraChecks, bool log = true)
        {
            if (extraChecks && !this.AudioListenerChecks(listener))
            {
                return false;
            }
            // multiple AudioOutCapture could be added to same GameObject
            AudioOutCapture[] captures = listener.GetComponents<AudioOutCapture>();
            for (int i = 0; i < captures.Length; i++)
            {
                if (this.SetOrSwitchAudioOutCapture(captures[i], false, false))
                {
                    this.autoDestroyAudioOutCapture = false;
                    return true;
                }
            }
            // in case we fail to set any available AudioOutCapture, let's add a new one
            AudioOutCapture capture = listener.gameObject.AddComponent<AudioOutCapture>();
            if (this.SetOrSwitchAudioOutCapture(capture, false, log))
            {
                if (this.Logger.IsDebugEnabled)
                {
                    this.Logger.LogDebug("AudioOutCapture component added to same GameObject as AudioListener.");
                }
                this.autoDestroyAudioOutCapture = true;
                return true;
            }
            Destroy(capture);
            return false;
        }

        private bool SetOrSwitchAudioOutCapture(AudioOutCapture capture, bool extraChecks, bool log = true)
        {
            if (!this.AudioOutCaptureChecks(capture, extraChecks, log))
            {
                return false;
            }
            bool aecWasStarted = this.aecStarted;
            bool audioOutSwitched = false;
            if (!ReferenceEquals(null, this.audioOutCapture) && this.audioOutCapture)
            {
                if (this.audioOutCapture != capture)
                {
                    this.UnsubscribeFromAudioOutCapture(this.autoDestroyAudioOutCapture);
                    audioOutSwitched = true;
                }
                else if (extraChecks)
                {
                    if (log && this.Logger.IsErrorEnabled)
                    {
                        this.Logger.LogError("The same AudioOutCapture is being used already");
                    }
                    return false;
                }
            }
            this.audioOutCapture = capture;
            this.audioListener = capture.GetComponent<AudioListener>();
            if (aecWasStarted && audioOutSwitched)
            {
                this.audioOutCapture.OnAudioFrame += this.OnAudioOutFrameFloat;
            }
            return true;
        }

        private bool InitAudioOutCapture()
        {
            if (!ReferenceEquals(null, this.audioOutCapture) && this.audioOutCapture)
            {
                if (this.Logger.IsErrorEnabled)
                {
                    this.Logger.LogError("AudioOutCapture is already initialized.");
                }
                return false;
            }
            if (this.audioListener == null)
            {
                AudioOutCapture[] audioOutCaptures = FindObjectsOfType<AudioOutCapture>();
                for(int i=0; i < audioOutCaptures.Length; i++)
                {
                    AudioOutCapture capture = audioOutCaptures[i];
                    if (this.SetOrSwitchAudioOutCapture(capture, true, false))
                    {
                        this.autoDestroyAudioOutCapture = false;
                        return true;
                    }
                }
                AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
                for(int i=0; i < audioListeners.Length; i++)
                {
                    AudioListener listener = audioListeners[i];
                    if (this.SetOrSwitchAudioListener(listener, false))
                    {
                        return true;
                    }
                }
                if (this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioListener and AudioOutCapture components are required for AEC to work.");
                }
                return false;
            }
            return this.SetOrSwitchAudioListener(this.audioListener, true);
        }

        private bool UnsubscribeFromAudioOutCapture(bool destroy)
        {
            if (!ReferenceEquals(null, this.audioOutCapture))
            {
                if (this.aecStarted)
                {
                    this.audioOutCapture.OnAudioFrame -= this.OnAudioOutFrameFloat;
                }
                if (destroy)
                {
                    Destroy(this.audioOutCapture);
                    if (this.Logger.IsDebugEnabled)
                    {
                        this.Logger.LogDebug("AudioOutCapture component destroyed.");
                    }
                    this.audioOutCapture = null;
                }
                return true;
            }
            return false;
        }
        
        private bool AudioListenerChecks(AudioListener listener, bool log = true)
        {
            if (ReferenceEquals(listener, null))
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioListener is null.");
                }
                return false;
            }
            if (!listener)
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioListener is destroyed.");
                }
                return false;
            }
            if (!listener.gameObject.activeInHierarchy) 
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("The GameObject to which the AudioListener is attached is not active in hierarchy.");
                }
                return false;
            }
            if (!listener.enabled) 
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioListener is disabled.");
                }
                return false;
            }
            return true;
        }

        private bool AudioOutCaptureChecks(AudioOutCapture capture, bool listenerChecks, bool log = true)
        {
            if (ReferenceEquals(capture, null))
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioOutCapture is null.");
                }
                return false;
            }
            if (!capture)
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioOutCapture is destroyed.");
                }
                return false;
            }
            if (!listenerChecks && !capture.gameObject.activeInHierarchy) 
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("The GameObject to which the AudioOutCapture is attached is not active in hierarchy.");
                }
                return false;
            }
            if (!capture.enabled) 
            {
                if (log && this.Logger.IsErrorEnabled) 
                {
                    this.Logger.LogError("AudioOutCapture is disabled.");
                }
                return false;
            }
            return !listenerChecks || this.AudioListenerChecks(capture.GetComponent<AudioListener>(), log);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the AudioListener to be used with this WebRtcAudioDsp. Needed for Acoustic Echo Cancellation.
        /// </summary>
        /// <param name="listener">The audioListener to be used</param>
        /// <returns>Success or failure</returns>
        public bool SetOrSwitchAudioListener(AudioListener listener)
        {
            return this.SetOrSwitchAudioListener(listener, true);
        }

        /// <summary>
        /// Set the AudioOutCapture to be used with this WebRtcAudioDsp. Needed for Acoustic Echo Cancellation.
        /// </summary>
        /// <param name="capture">The audioOutCapture to be used</param>
        /// <returns>Success or failure</returns>
        public bool SetOrSwitchAudioOutCapture(AudioOutCapture capture)
        {
            if (this.SetOrSwitchAudioOutCapture(capture, true))
            {
                this.autoDestroyAudioOutCapture = false;
                return true;
            }
            return false;
        }

        #endregion
    }
}