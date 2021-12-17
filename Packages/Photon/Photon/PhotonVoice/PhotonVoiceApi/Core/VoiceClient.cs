// -----------------------------------------------------------------------
// <copyright file="VoiceClient.cs" company="Exit Games GmbH">
//   Photon Voice API Framework for Photon - Copyright (C) 2017 Exit Games GmbH
// </copyright>
// <summary>
//   Photon data streaming support.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;

namespace Photon.Voice
{
    public interface ILogger
    {
        void LogError(string fmt, params object[] args);
        void LogWarning(string fmt, params object[] args);
        void LogInfo(string fmt, params object[] args);
        void LogDebug(string fmt, params object[] args);
    }

    interface IVoiceTransport
    {
        bool IsChannelJoined(int channelId);
        // targetPlayerId: to all if 0, to myself if -1
        void SendVoicesInfo(IEnumerable<LocalVoice> voices, int channelId, int targetPlayerId);
        // targetPlayerId: to all if 0, to myself if -1
        void SendVoiceRemove(LocalVoice voice, int channelId, int targetPlayerId);
        // targetPlayerId: to all if 0, to myself if -1
        void SendFrame(ArraySegment<byte> data, FrameFlags flags, byte evNumber, byte voiceId, int channelId, int targetPlayerId, bool reliable, LocalVoice localVoice);
        string ChannelIdStr(int channelId);
        string PlayerIdStr(int playerId);
    }

    /// <summary>
    /// Voice client interact with other clients on network via IVoiceTransport.
    /// </summary>        
    public class VoiceClient : IDisposable
    {
        internal IVoiceTransport transport;
        internal ILogger logger;

        /// <summary>Lost frames counter.</summary>
        public int FramesLost { get; internal set; }

        /// <summary>Received frames counter.</summary>
        public int FramesReceived { get; private set; }

        /// <summary>Sent frames counter.</summary>
        public int FramesSent { get { int x = 0; foreach (var v in this.localVoices) { x += v.Value.FramesSent; } return x; } }

        /// <summary>Sent frames bytes counter.</summary>
        public int FramesSentBytes { get { int x = 0; foreach (var v in this.localVoices) { x += v.Value.FramesSentBytes; } return x; } }

        /// <summary>Average time required voice packet to return to sender.</summary>
        public int RoundTripTime { get; private set; }

        /// <summary>Average round trip time variation.</summary>
        public int RoundTripTimeVariance { get; private set; }

        /// <summary>Do not log warning when duplicate info received.</summary>
        public bool SuppressInfoDuplicateWarning { get; set; }

        /// <summary>Remote voice info event delegate.</summary>        
        public delegate void RemoteVoiceInfoDelegate(int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options);

        /// <summary>
        /// Register a method to be called when remote voice info arrived (after join or new new remote voice creation).
        /// Metod parameters: (int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options);
        /// </summary>
        public RemoteVoiceInfoDelegate OnRemoteVoiceInfoAction { get; set; }

        /// <summary>Lost frames simulation ratio.</summary>
        public int DebugLostPercent { get; set; }

        private int prevRtt = 0;
        /// <summary>Iterates through copy of all local voices list.</summary>
        public IEnumerable<LocalVoice> LocalVoices
        {
            get
            {
                var res = new LocalVoice[this.localVoices.Count];
                this.localVoices.Values.CopyTo(res, 0);
                return res;
            }
        }

        /// <summary>Iterates through copy of all local voices list of given channel.</summary>
        public IEnumerable<LocalVoice> LocalVoicesInChannel(int channelId)
        {
            List<LocalVoice> channelVoices;
            if (this.localVoicesPerChannel.TryGetValue(channelId, out channelVoices))
            {
                var res = new LocalVoice[channelVoices.Count];
                channelVoices.CopyTo(res, 0);
                return res;
            }
            else
            {
                return new LocalVoice[0];
            }
        }

        /// <summary>Iterates through all remote voices infos.</summary>
        public IEnumerable<RemoteVoiceInfo> RemoteVoiceInfos
        {
            get
            {
                foreach (var playerVoices in this.remoteVoices)
                {
                    foreach (var voice in playerVoices.Value)
                    {
                        yield return new RemoteVoiceInfo(voice.Value.channelId, playerVoices.Key, voice.Key, voice.Value.Info);
                    }
                }
            }
        }

        public void LogSpacingProfiles()
        {
            foreach (var voice in this.localVoices)
            {
                voice.Value.SendSpacingProfileStart(); // in case it's not started yet
                this.logger.LogInfo(voice.Value.LogPrefix + " ev. prof.: " + voice.Value.SendSpacingProfileDump);
            }
            foreach (var playerVoices in this.remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.ReceiveSpacingProfileStart(); // in case it's not started yet
                    this.logger.LogInfo(voice.Value.LogPrefix + " ev. prof.: " + voice.Value.ReceiveSpacingProfileDump);
                }
            }
        }

        public void LogStats()
        {
            int dc = FrameBuffer.statDisposerCreated;
            int dd = FrameBuffer.statDisposerDisposed;
            int pp = FrameBuffer.statPinned;
            int pu = FrameBuffer.statUnpinned;
            this.logger.LogInfo("[PV] FrameBuffer stats Disposer: " + dc + " - " + dd + " = " + (dc - dd));
            this.logger.LogInfo("[PV] FrameBuffer stats Pinned: " + pp + " - " + pu + " = " + (pp - pu));
        }
        
        public void SetRemoteVoiceDelayFrames(Codec codec, int delayFrames)
        {
            remoteVoiceDelayFrames[codec] = delayFrames;
            foreach (var playerVoices in this.remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    if (codec == voice.Value.Info.Codec)
                    {
                        voice.Value.DelayFrames = delayFrames;
                    }
                }
            }
        }

        // store delay to apply on new remote voices
        private Dictionary<Codec, int> remoteVoiceDelayFrames = new Dictionary<Codec, int>();

        /// <summary>Creates VoiceClient instance</summary>
        internal VoiceClient(IVoiceTransport transport, ILogger logger)
        {
            this.transport = transport;
            this.logger = logger;
        }

        /// <summary>
        /// This method dispatches all available incoming commands and then sends this client's outgoing commands.
        /// Call this method regularly (2..20 times a second).
        /// </summary>
        public void Service()
        {
            foreach (var v in localVoices)
            {
                v.Value.service();
            }
        }

        private LocalVoice createLocalVoice(int channelId, Func<byte, int, LocalVoice> voiceFactory)
        {
            var newId = getNewVoiceId();
            if (newId != 0)
            {
                LocalVoice v = voiceFactory(newId, channelId);
                if (v != null)
                {
                    addVoice(newId, channelId, v);
                    this.logger.LogInfo(v.LogPrefix + " added enc: " + v.Info.ToString());
                    return v;
                }
            }

            return null;
        }
        /// <summary>
        /// Creates basic outgoing stream w/o data processing support. Provided encoder should generate output data stream.
        /// </summary>
        /// <param name="voiceInfo">Outgoing stream parameters. Set applicable fields to read them by encoder and by receiving client when voice created.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="encoder">Encoder producing the stream.</param>
        /// <returns>Outgoing stream handler.</returns>
        public LocalVoice CreateLocalVoice(VoiceInfo voiceInfo, int channelId = 0, IEncoder encoder = null)
        {
            return (LocalVoice)createLocalVoice(channelId, (vId, chId) => new LocalVoice(this, encoder, vId, voiceInfo, chId));
        }

        /// <summary>
        /// Creates outgoing stream consuming sequence of values passed in array buffers of arbitrary length which repacked in frames of constant length for further processing and encoding.
        /// </summary>
        /// <typeparam name="T">Type of data consumed by outgoing stream (element type of array buffers).</typeparam>
        /// <param name="voiceInfo">Outgoing stream parameters. Set applicable fields to read them by encoder and by receiving client when voice created.</param>
        /// <param name="frameSize">Size of buffer LocalVoiceFramed repacks input data stream to.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="encoder">Encoder compressing data stream in pipeline.</param>
        /// <returns>Outgoing stream handler.</returns>
        public LocalVoiceFramed<T> CreateLocalVoiceFramed<T>(VoiceInfo voiceInfo, int frameSize, int channelId = 0, IEncoder encoder = null)
        {
            return (LocalVoiceFramed<T>)createLocalVoice(channelId, (vId, chId) => new LocalVoiceFramed<T>(this, encoder, vId, voiceInfo, chId, frameSize));
        }

        private LocalVoiceAudio<T> CreateLocalVoiceAudio<T>(VoiceInfo voiceInfo, IAudioDesc audioSourceDesc, IEncoder encoder, int channelId)
        {
            return (LocalVoiceAudio<T>)createLocalVoice(channelId, (vId, chId) => LocalVoiceAudio<T>.Create(this, vId, encoder, voiceInfo, audioSourceDesc, chId));
        }

        /// <summary>
        /// Creates outgoing audio stream of type automatically assigned and adds procedures (callback or serviceable) for consuming given audio source data.
        /// Adds audio specific features (e.g. resampling, level meter) to processing pipeline and to returning stream handler.
        /// </summary>
        /// <param name="voiceInfo">Outgoing audio stream parameters. Set applicable fields to read them by encoder and by receiving client when voice created.</param>
        /// <param name="source">Streaming audio source.</param>
        /// <param name="sampleType">Voice's audio sample type. If does not match source audio sample type, conversion will occur.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="encoder">Audio encoder. Set to null to use default Opus encoder.</param>
        /// <returns>Outgoing stream handler.</returns>
        /// <remarks>
        /// audioSourceDesc.SamplingRate and voiceInfo.SamplingRate may do not match. Automatic resampling will occur in this case.
        /// </remarks>
        public LocalVoice CreateLocalVoiceAudioFromSource(VoiceInfo voiceInfo, IAudioDesc source, AudioSampleType sampleType, IEncoder encoder = null, int channelId = 0)
        {
            // resolve AudioSampleType.Source to concrete type for encoder creation
            if (sampleType == AudioSampleType.Source)
            {
                if (source is IAudioPusher<float> || source is IAudioReader<float>)
                {
                    sampleType = AudioSampleType.Float;
                }
                else if (source is IAudioPusher<short> || source is IAudioReader<short>)
                {
                    sampleType = AudioSampleType.Short;
                }
            }

            if (encoder == null)
            {
                switch (sampleType)
                {
                    case AudioSampleType.Float:
                        encoder = Platform.CreateDefaultAudioEncoder<float>(logger, voiceInfo);
                        break;
                    case AudioSampleType.Short:
                        encoder = Platform.CreateDefaultAudioEncoder<short>(logger, voiceInfo);
                        break;
                }    
            }
                
            if (source is IAudioPusher<float>)
            {
                if (sampleType == AudioSampleType.Short)
                {
                    logger.LogInfo("[PV] Creating local voice with source samples type conversion from IAudioPusher float to short.");
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, encoder, channelId);
                    // we can safely reuse the same buffer in callbacks from native code
                    // 
                    var bufferFactory = new FactoryReusableArray<float>(0);
                    ((IAudioPusher<float>)source).SetCallback(buf => {
                        var shortBuf = localVoice.BufferFactory.New(buf.Length);
                        AudioUtil.Convert(buf, shortBuf, buf.Length);
                        localVoice.PushDataAsync(shortBuf);
                    }, bufferFactory);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, encoder, channelId);
                    ((IAudioPusher<float>)source).SetCallback(buf => localVoice.PushDataAsync(buf), localVoice.BufferFactory);
                    return localVoice;
                }
            }
            else if (source is IAudioPusher<short>)
            {
                if (sampleType == AudioSampleType.Float)
                {
                    logger.LogInfo("[PV] Creating local voice with source samples type conversion from IAudioPusher short to float.");
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, encoder, channelId);
                    // we can safely reuse the same buffer in callbacks from native code
                    // 
                    var bufferFactory = new FactoryReusableArray<short>(0);
                    ((IAudioPusher<short>)source).SetCallback(buf =>
                    {
                        var floatBuf = localVoice.BufferFactory.New(buf.Length);
                        AudioUtil.Convert(buf, floatBuf, buf.Length);
                        localVoice.PushDataAsync(floatBuf);
                    }, bufferFactory);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, encoder, channelId);
                    ((IAudioPusher<short>)source).SetCallback(buf => localVoice.PushDataAsync(buf), localVoice.BufferFactory);
                    return localVoice;
                }
            }
            else if (source is IAudioReader<float>)
            {
                if (sampleType == AudioSampleType.Short)
                {
                    logger.LogInfo("[PV] Creating local voice with source samples type conversion from IAudioReader float to short.");
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, encoder, channelId);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPoolFloatToShort(localVoice, source as IAudioReader<float>);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, encoder, channelId);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPool<float>(localVoice, source as IAudioReader<float>);
                    return localVoice;
                }
            }
            else if (source is IAudioReader<short>)
            {
                if (sampleType == AudioSampleType.Float)
                {
                    logger.LogInfo("[PV] Creating local voice with source samples type conversion from IAudioReader short to float.");
                    var localVoice = CreateLocalVoiceAudio<float>(voiceInfo, source, encoder, channelId);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPoolShortToFloat(localVoice, source as IAudioReader<short>);
                    return localVoice;
                }
                else
                {
                    var localVoice = CreateLocalVoiceAudio<short>(voiceInfo, source, encoder, channelId);
                    localVoice.LocalUserServiceable = new BufferReaderPushAdapterAsyncPool<short>(localVoice, source as IAudioReader<short>);
                    return localVoice;
                }
            }
            else
            {
                logger.LogError("[PV] CreateLocalVoiceAudioFromSource does not support Voice.IAudioDesc of type {0}", source.GetType());
                return LocalVoiceAudioDummy.Dummy;
            }
        }

#if PHOTON_VOICE_VIDEO_ENABLE
        /// <summary>
        /// Creates outgoing video stream consuming sequence of image buffers.
        /// </summary>
        /// <param name="voiceInfo">Outgoing stream parameters. Set applicable fields to read them by encoder and by receiving client when voice created.</param>
        /// <param name="channelId">Transport channel specific to transport.</param>
        /// <param name="encoder">Encoder compressing video data. Set to null to use default VP8 implementation.</param>
        /// <returns>Outgoing stream handler.</returns>
        public LocalVoiceVideo CreateLocalVoiceVideo(VoiceInfo voiceInfo, IEncoder encoder, int channelId = 0)
        {
            return (LocalVoiceVideo)createLocalVoice(channelId, (vId, chId) => new LocalVoiceVideo(this, encoder, vId, voiceInfo, chId));
        }
#endif

        private byte getNewVoiceId()
        {
            // id assigned starting from 1 and up to 255

            byte newId = 0; // non-zero if successfully assigned
            if (voiceIdCnt == 255)
            {
                // try to reuse id
                var ids = new bool[256];
                foreach (var v in localVoices)
                {
                    ids[v.Value.id] = true;
                }
                // ids[0] is not used
                for (byte id = 1; id != 0 /* < 256 */ ; id++)
                {
                    if (!ids[id])
                    {
                        newId = id;
                        break;
                    }
                }
            }
            else
            {
                voiceIdCnt++;
                newId = voiceIdCnt;
            }
            return newId;
        }

        void addVoice(byte newId, int channelId, LocalVoice v)
        {
            localVoices[newId] = v;

            List<LocalVoice> voiceList;
            if (!localVoicesPerChannel.TryGetValue(channelId, out voiceList))
            {
                voiceList = new List<LocalVoice>();
                localVoicesPerChannel[channelId] = voiceList;
            }
            voiceList.Add(v);

            if (this.transport.IsChannelJoined(channelId))
            {
                sendVoicesInfoAndConfigFrame(new List<LocalVoice>() { v }, channelId, 0); // broadcast if joined
            }
            v.InterestGroup = this.GlobalInterestGroup;
        }
        /// <summary>
        /// Removes local voice (outgoing data stream).
        /// <param name="voice">Handler of outgoing stream to be removed.</param>
        /// </summary>
        public void RemoveLocalVoice(LocalVoice voice)
        {
            this.localVoices.Remove(voice.id);

            this.localVoicesPerChannel[voice.channelId].Remove(voice);
            if (this.transport.IsChannelJoined(voice.channelId))
            {
                this.transport.SendVoiceRemove(voice, voice.channelId, 0);
            }

            voice.Dispose();
            this.logger.LogInfo(voice.LogPrefix + " removed");
        }

        private void sendChannelVoicesInfo(int channelId, int targetPlayerId)
        {
            if (this.transport.IsChannelJoined(channelId))
            {
                List<LocalVoice> voiceList;
                if (this.localVoicesPerChannel.TryGetValue(channelId, out voiceList))
                {
                    sendVoicesInfoAndConfigFrame(voiceList, channelId, targetPlayerId);
                }
            }
        }

        internal void sendVoicesInfoAndConfigFrame(IEnumerable<LocalVoice> voiceList, int channelId, int targetPlayerId)
        {
            this.transport.SendVoicesInfo(voiceList, channelId, targetPlayerId);
            foreach (var v in voiceList)
            {
                v.sendConfigFrame(targetPlayerId);
            }

            // send debug echo infos to myself if broadcast requested
            if (targetPlayerId == 0)
            {
                var debugEchoVoices = localVoices.Values.Where(x => x.DebugEchoMode);
                if (debugEchoVoices.Count() > 0)
                {
                    this.transport.SendVoicesInfo(debugEchoVoices, channelId, -1);
                }
            }
        }

        internal byte GlobalInterestGroup
        {
            get { return this.globalInterestGroup; }
            set
            {
                this.globalInterestGroup = value;
                foreach (var v in this.localVoices)
                {
                    v.Value.InterestGroup = this.globalInterestGroup;
                }
            }
        }

        #region nonpublic

        private byte globalInterestGroup;
        private byte voiceIdCnt = 0;

        private Dictionary<byte, LocalVoice> localVoices = new Dictionary<byte, LocalVoice>();
        private Dictionary<int, List<LocalVoice>> localVoicesPerChannel = new Dictionary<int, List<LocalVoice>>();
        // player id -> voice id -> voice
        private Dictionary<int, Dictionary<byte, RemoteVoice>> remoteVoices = new Dictionary<int, Dictionary<byte, RemoteVoice>>();

        private void clearRemoteVoices()
        {
            foreach (var playerVoices in remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.removeAndDispose();
                }
            }
            remoteVoices.Clear();
            this.logger.LogInfo("[PV] Remote voices cleared");
        }

        private void clearRemoteVoicesInChannel(int channelId)
        {
            foreach (var playerVoices in remoteVoices)
            {
                List<byte> toRemove = new List<byte>();
                foreach (var voice in playerVoices.Value)
                {
                    if (voice.Value.channelId == channelId)
                    {
                        voice.Value.removeAndDispose();
                        toRemove.Add(voice.Key);
                    }
                }
                foreach (var id in toRemove)
                {
                    playerVoices.Value.Remove(id);
                }
            }
            this.logger.LogInfo("[PV] Remote voices for channel " + this.channelStr(channelId) + " cleared");
        }

        private void clearRemoteVoicesInChannelForPlayer(int channelId, int playerId)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                List<byte> toRemove = new List<byte>();
                foreach (var v in playerVoices)
                {
                    if (v.Value.channelId == channelId)
                    {
                        v.Value.removeAndDispose();
                        toRemove.Add(v.Key);
                    }
                }
                foreach (var id in toRemove)
                {
                    playerVoices.Remove(id);
                }
            }
        }
        
		internal void onJoinChannel(int channel)
        {
            sendChannelVoicesInfo(channel, 0);// my join, broadcast
        }

        internal void onLeaveChannel(int channel)
        {
            clearRemoteVoicesInChannel(channel);
        }

        internal void onLeaveAllChannels()
        {
            clearRemoteVoices();
        }

        internal void onPlayerJoin(int channelId, int playerId)
        {
            sendChannelVoicesInfo(channelId, playerId);// send to new joined only
        }

        internal void onPlayerLeave(int channelId, int playerId)
        {
            clearRemoteVoicesInChannelForPlayer(channelId, playerId);
        }

        internal void onVoiceInfo(int channelId, int playerId, byte voiceId, byte eventNumber, VoiceInfo info)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;

            if (!remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                playerVoices = new Dictionary<byte, RemoteVoice>();
                remoteVoices[playerId] = playerVoices;
            }

            if (!playerVoices.ContainsKey(voiceId))
            {

                this.logger.LogInfo("[PV] ch#" + this.channelStr(channelId) + " p#" + this.playerStr(playerId) + " v#" + voiceId + " Info received: " + info.ToString() + " ev=" + eventNumber);

                RemoteVoiceOptions options = new RemoteVoiceOptions() { OutputImageFormat = ImageFormat.Undefined };
                if (this.OnRemoteVoiceInfoAction != null)
                {
                    this.OnRemoteVoiceInfoAction(channelId, playerId, voiceId, info, ref options);
                }
                var rv = new RemoteVoice(this, options, channelId, playerId, voiceId, info, eventNumber);
                playerVoices[voiceId] = rv;
                int delayFrames;
                if (remoteVoiceDelayFrames.TryGetValue(info.Codec, out delayFrames))
                {
                    rv.DelayFrames = delayFrames;
                }
            }
            else
            {
                if (!this.SuppressInfoDuplicateWarning)
                {
                    this.logger.LogWarning("[PV] Info duplicate for voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId));
                }
            }
        }

        internal void onVoiceRemove(int channelId, int playerId, byte[] voiceIds)
        {
            Dictionary<byte, RemoteVoice> playerVoices = null;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {
                foreach (var voiceId in voiceIds)
                {
                    RemoteVoice voice;
                    if (playerVoices.TryGetValue(voiceId, out voice))
                    {
                        playerVoices.Remove(voiceId);
                        this.logger.LogInfo("[PV] Remote voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId) + " removed");
                        voice.removeAndDispose();
                    }
                    else
                    {
                        this.logger.LogWarning("[PV] Remote voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId) + " not found when trying to remove");
                    }
                }
            }
            else
            {
                this.logger.LogWarning("[PV] Remote voice list of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId) + " not found when trying to remove voice(s)");
            }
        }

        Random rnd = new Random();
        internal void onFrame(int channelId, int playerId, byte voiceId, byte evNumber, ref FrameBuffer receivedBytes, bool isLocalPlayer)
        {
            if (isLocalPlayer)
            {
                // rtt measurement in debug echo mode
                LocalVoice voice;
                if (this.localVoices.TryGetValue(voiceId, out voice))
                {
                    int sendTime;
                    if (voice.eventTimestamps.TryGetValue(evNumber, out sendTime))
                    {
                        int rtt = Environment.TickCount - sendTime;
                        int rttvar = rtt - prevRtt;
                        prevRtt = rtt;
                        if (rttvar < 0) rttvar = -rttvar;
                        this.RoundTripTimeVariance = (rttvar + RoundTripTimeVariance * 19) / 20;
                        this.RoundTripTime = (rtt + RoundTripTime * 19) / 20;
                    }
                }
                //internal Dictionary<byte, DateTime> localEventTimestamps = new Dictionary<byte, DateTime>();
            }

            if (this.DebugLostPercent > 0 && rnd.Next(100) < this.DebugLostPercent)
            {
                this.logger.LogWarning("[PV] Debug Lost Sim: 1 packet dropped");
                return;
            }

            FramesReceived++;

            Dictionary<byte, RemoteVoice> playerVoices = null;
            if (remoteVoices.TryGetValue(playerId, out playerVoices))
            {

                RemoteVoice voice = null;
                if (playerVoices.TryGetValue(voiceId, out voice))
                {
                    voice.receiveBytes(ref receivedBytes, evNumber);
                }
                else
                {
                    this.logger.LogWarning("[PV] Frame event for not inited voice #" + voiceId + " of player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId));
                }
            }
            else
            {
                this.logger.LogWarning("[PV] Frame event for voice #" + voiceId + " of not inited player " + this.playerStr(playerId) + " at channel " + this.channelStr(channelId));
            }
        }

        internal string channelStr(int channelId)
        {
            var str = this.transport.ChannelIdStr(channelId);
            if (str != null)
            {
                return channelId + "(" + str + ")";
            }
            else
            {
                return channelId.ToString();
            }
        }

        internal string playerStr(int playerId)
        {
            var str = this.transport.PlayerIdStr(playerId);
            if (str != null)
            {
                return playerId + "(" + str + ")";
            }
            else
            {
                return playerId.ToString();
            }
        }
        //public string ToStringFull()
        //{
        //    return string.Format("Photon.Voice.Client, local: {0}, remote: {1}",  localVoices.Count, remoteVoices.Count);
        //}

        #endregion

        public void Dispose()
        {
            foreach (var v in this.localVoices)
            {
                v.Value.Dispose();
            }
            foreach (var playerVoices in remoteVoices)
            {
                foreach (var voice in playerVoices.Value)
                {
                    voice.Value.Dispose();
                }
            }
        }
    }
}