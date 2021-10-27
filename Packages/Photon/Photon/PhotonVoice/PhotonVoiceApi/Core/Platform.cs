#define PREVIEW_SCREEN_QUAD_TEXTURE // use video texture, otherwise use SurfaceView for Android and AVCaptureVideoPreviewLayer for iOS

using System;
using System.Collections;
using System.Diagnostics;

namespace Photon.Voice
{
    public static class Platform
    {
        static public IEncoder CreateDefaultAudioEncoder<T>(ILogger logger, VoiceInfo info)
        {
            switch (info.Codec)
            {
                case Codec.AudioOpus:
                    return OpusCodec.Factory.CreateEncoder<T[]>(info, logger);
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultAudioEncoder", info.Codec, logger);
            }
        }

#if PHOTON_VOICE_VIDEO_ENABLE
        static public IEncoderDirectImage CreateDefaultVideoEncoder(ILogger logger, VoiceInfo info)
        {
            switch (info.Codec)
            {
                case Codec.VideoVP8:
                case Codec.VideoVP9:
                    return new VPxCodec.Encoder(logger, info);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                case Codec.VideoH264:
                    //ve = new FFmpegCodec.Encoder(logger, info);
                    return new Windows.MFTCodec.VideoEncoder(logger, info);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                case Codec.VideoH264:
                    //ve = new FFmpegCodec.Encoder(logger, info);
                    return new MacOS.VideoEncoder(logger, info);
#endif
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultVideoEncoder", info.Codec, logger);
            }
        }

        static public IVideoRecorder CreateDefaultVideoRecorder(ILogger logger, PreviewManager previewManager, VoiceInfo info, string camDevice, Action<IVideoRecorder> onReady)
        {
            // native platform-specific recorders
#if UNITY_ANDROID && !UNITY_EDITOR
#if PREVIEW_SCREEN_QUAD_TEXTURE
            return new Unity.AndroidVideoRecorderTexture(logger, info, onReady);
#else
            var ve = new Unity.AndroidVideoEncoderSurfaceView(logger, previewManager, info);
            return new Unity.AndroidVideoRecorderSurfaceView(ve, ve.Preview, onReady);
#endif
#elif UNITY_IOS && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
#if PREVIEW_SCREEN_QUAD_TEXTURE
                return new IOS.VideoRecorderTexture(logger, info, onReady);
#else
                var ve = new IOS.VideoEncoderLayer(logger, info);
                return new IOS.VideoRecorderLayer(ve, onReady);
#endif
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoRecorder", info.Codec, logger);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                var ve = new UWP.VideoEncoder(logger, info);
                return new UWP.VideoRecorder(ve, ve.Preview, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoRecorder", info.Codec, logger);
#else // multi-platform VideoRecorderUnity or generic VideoRecorder
            var ve = CreateDefaultVideoEncoder(logger, info);
#if UNITY_5_3_OR_NEWER // #if UNITY
            return new Unity.VideoRecorderUnity(ve, null, camDevice, info.Width, info.Height, info.FPS, onReady);
#else
            return new VideoRecorder(ve, null);
#endif

#endif
            }

        static public IVideoPlayer CreateDefaultVideoPlayer(ILogger logger, PreviewManager previewManager, VoiceInfo info, Action<IVideoPlayer> onReady)
        {
            // native platform-specific players
#if UNITY_ANDROID && !UNITY_EDITOR
#if PREVIEW_SCREEN_QUAD_TEXTURE
            return new Unity.AndroidVideoPlayerTexture(logger, info, onReady);
#else
            var vd = new Unity.AndroidVideoDecoderSurfaceView(logger, previewManager, info);
            return new VideoPlayer(vd, vd.Preview, info.Width, info.Height, onReady);
#endif
#elif UNITY_IOS && !UNITY_EDITOR
            if (info.Codec == Codec.VideoH264)
            {
#if PREVIEW_SCREEN_QUAD_TEXTURE
                return new IOS.VideoPlayerTexture(logger, info, onReady);
#else
                var vd = new IOS.VideoDecoderLayer(logger);
                return new VideoPlayer(vd, vd.PreviewLayer, info.Width, info.Height, onReady);
#endif
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoPlayer", info.Codec, logger);
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            if (info.Codec == Codec.VideoH264)
            {
                var vd = new UWP.VideoDecoder(logger, info);
                return new VideoPlayer(vd, vd.Preview, info.Width, info.Height, onReady);
            }
            throw new UnsupportedCodecException("Platform.CreateDefaultVideoPlayer", info.Codec, logger);
#else  // multi-platform VideoPlayerUnity or generic VideoPlayer
            IDecoderQueuedOutputImageNative vd;
            switch (info.Codec)
            {
                case Codec.VideoVP8:
                case Codec.VideoVP9:
                    vd = new VPxCodec.Decoder(logger);
                    break;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                case Codec.VideoH264:
                    //vd = new FFmpegCodec.Decoder(logger);
                    vd = new Windows.MFTCodec.VideoDecoder(logger, info);
                    break;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                case Codec.VideoH264:
                    //vd = new FFmpegCodec.Decoder(logger);
                    vd = new MacOS.VideoDecoder(logger, info);
                    break;
#endif
                default:
                    throw new UnsupportedCodecException("Platform.CreateDefaultVideoPlayer", info.Codec, logger);
            }
#if UNITY_5_3_OR_NEWER // #if UNITY
            var vp = new Unity.VideoPlayerUnity(vd, onReady);
            // assign Draw method copying Image to Unity texture as software decoder Output
            vd.Output = vp.Draw;
            return vp;
#else
            return new VideoPlayer(vd, null, 0, 0);
#endif

#endif
        }

        public static PreviewManager CreateDefaultPreviewManager(ILogger logger)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
#if PREVIEW_SCREEN_QUAD_TEXTURE
            return new Unity.PreviewManagerScreenQuadTexture(logger);
#else
            return new Unity.AndroidPreviewManagerSurfaceView(logger);
#endif
#elif UNITY_IOS && !UNITY_EDITOR
#if PREVIEW_SCREEN_QUAD_TEXTURE
            return new Unity.PreviewManagerScreenQuadTexture(logger);
#else
            return new IOS.PreviewManagerLayer(logger);
#endif
#elif WINDOWS_UWP || (UNITY_WSA && !UNITY_EDITOR)
            return new UWP.PreviewManager(logger);
#elif UNITY_5_3_OR_NEWER // #if UNITY
            return new Unity.PreviewManagerScreenQuadTexture(logger); // uses custom shader
            // return new Unity.PreviewManagerUnityGUI(); // uses GUI.DrawTexture
#else
            return null;
#endif
        }

#endif
        }
    }
