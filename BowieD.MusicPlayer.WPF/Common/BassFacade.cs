using System;
using System.Collections.Generic;
using Un4seen.Bass;

namespace BowieD.MusicPlayer.WPF.Common
{
    internal static class BassFacade
    {
        private static int Handle;

        public const int FREQ_HZ = 44100;

        public static bool HasInitDefaultDevice;
        public static int Volume { get; private set; } = 100;

        public static bool IsStopped { get; private set; } = true;

        public static bool Init()
        {
            if (!HasInitDefaultDevice)
            {
                HasInitDefaultDevice = Bass.BASS_Init(-1, FREQ_HZ, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
            }

            return HasInitDefaultDevice;
        }

        public static void Play(string fileName)
        {
            if (State != BASSActive.BASS_ACTIVE_PAUSED)
            {
                Stop();

                if (Init())
                {
                    Handle = Bass.BASS_StreamCreateFile(fileName, 0, 0, BASSFlag.BASS_DEFAULT);

                    if (Handle != 0)
                    {
                        SetVolume(Volume);

                        Bass.BASS_ChannelPlay(Handle, false);
                    }
                }
            }
            else
            {
                Bass.BASS_ChannelPlay(Handle, false);
            }

            IsStopped = false;
        }

        public static void Pause()
        {
            if (State == BASSActive.BASS_ACTIVE_PLAYING)
            {
                Bass.BASS_ChannelPause(Handle);
            }
        }

        public static void Resume()
        {
            if (State == BASSActive.BASS_ACTIVE_PAUSED)
            {
                Bass.BASS_ChannelPlay(Handle, false);
            }
        }

        public static void Stop()
        {
            Bass.BASS_ChannelStop(Handle);
            Bass.BASS_StreamFree(Handle);
            IsStopped = true;
        }

        public static double GetStreamLengthInSeconds()
        {
            var timeBytes = Bass.BASS_ChannelGetLength(Handle);
            return Bass.BASS_ChannelBytes2Seconds(Handle, timeBytes);
        }

        public static double GetStreamPositionInSeconds()
        {
            var pos = Bass.BASS_ChannelGetPosition(Handle);
            return Bass.BASS_ChannelBytes2Seconds(Handle, pos);
        }

        public static bool SetStreamPositionInSeconds(double position)
        {
            return Bass.BASS_ChannelSetPosition(Handle, position);
        }

        public static void SetVolume(double value)
        {
            if (value > 100)
                Volume = 100;
            else if (value < 0)
                Volume = 0;
            else
                Volume = (int)value;

            Bass.BASS_ChannelSetAttribute(Handle, BASSAttribute.BASS_ATTRIB_VOL, Volume / 100f);
        }

        public static BASSActive State => Bass.BASS_ChannelIsActive(Handle);

        public static IEnumerable<string> SupportedExtensions => SUPPORTED_EXTENSIONS;

        private static readonly string[] SUPPORTED_EXTENSIONS = new string[]
        {
            ".mp3",
            ".m4a",
            ".wma",
            ".ogg",
            ".opus"
        };
    }
}
