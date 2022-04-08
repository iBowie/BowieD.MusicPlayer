using BowieD.MusicPlayer.WPF.MVVM;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Configuration
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AppSettings : BaseViewModel
    {
        public static AppSettings Instance { get; private set; }

        private bool _autoAccentColor;
        private bool _discordRichPresence;
        private bool _useDarkMode;
        private bool _enableHardwareAcceleration;
        private bool _enableReplayGain;
        private bool _smoothPlayPause;
        private bool _libraryScanCommonMusicFolder;
        private bool _libraryScanMyMusicFolder;
        private double _smoothFadeDuration;
        private bool _smoothAccentColorSwitch;
        private double _smoothAccentColorSwitchDuration;

        public AppSettings()
        {
            if (Instance is not null)
                throw new InvalidOperationException();

            Instance = this;

            LoadDefaults();
        }

        [JsonProperty]
        public bool AutoAccentColor
        {
            get => _autoAccentColor;
            set => ChangeProperty(ref _autoAccentColor, value, nameof(AutoAccentColor));
        }
        [JsonProperty]
        public bool EnableDiscordRichPresence
        {
            get => _discordRichPresence;
            set => ChangeProperty(ref _discordRichPresence, value, nameof(EnableDiscordRichPresence));
        }
        [JsonProperty]
        public bool UseDarkMode
        {
            get => _useDarkMode;
            set => ChangeProperty(ref _useDarkMode, value, nameof(UseDarkMode));
        }
        [JsonProperty]
        public bool EnableHardwareAcceleration
        {
            get => _enableHardwareAcceleration;
            set => ChangeProperty(ref _enableHardwareAcceleration, value, nameof(EnableHardwareAcceleration));
        }
        [JsonProperty]
        public bool EnableReplayGain
        {
            get => _enableReplayGain;
            set => ChangeProperty(ref _enableReplayGain, value, nameof(EnableReplayGain));
        }
        [JsonProperty]
        public bool SmoothPlayPause
        {
            get => _smoothPlayPause;
            set => ChangeProperty(ref _smoothPlayPause, value, nameof(SmoothPlayPause));
        }
        [JsonProperty]
        public double SmoothFadeDuration
        {
            get => _smoothFadeDuration;
            set => ChangeProperty(ref _smoothFadeDuration, value, nameof(SmoothFadeDuration));
        }
        [JsonProperty]
        public bool LibraryScanCommonMusicFolder
        {
            get => _libraryScanCommonMusicFolder;
            set => ChangeProperty(ref _libraryScanCommonMusicFolder, value, nameof(LibraryScanCommonMusicFolder));
        }
        [JsonProperty]
        public bool LibraryScanMyMusicFolder
        {
            get => _libraryScanMyMusicFolder;
            set => ChangeProperty(ref _libraryScanMyMusicFolder, value, nameof(LibraryScanMyMusicFolder));
        }
        [JsonProperty]
        public bool SmoothAccentColorSwitch
        {
            get => _smoothAccentColorSwitch;
            set => ChangeProperty(ref _smoothAccentColorSwitch, value, nameof(SmoothAccentColorSwitch));
        }
        [JsonProperty]
        public double SmoothAccentColorSwitchDuration
        {
            get => _smoothAccentColorSwitchDuration;
            set => ChangeProperty(ref _smoothAccentColorSwitchDuration, value, nameof(SmoothAccentColorSwitchDuration));
        }

        [JsonProperty]
        public ObservableCollection<string> CustomLibraryFolders { get; } = new();

        public void Load()
        {
            var dir = DataFolder.DataDirectory;
            var fn = Path.Combine(dir, "settings.json");

            if (File.Exists(fn))
            {
                try
                {
                    string content = File.ReadAllText(fn);

                    Newtonsoft.Json.JsonConvert.PopulateObject(content, this);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("could not read settings");
                    Debug.WriteLine(ex);
                }
            }
            else
            {
                LoadDefaults();
                Save();
            }
        }

        public void LoadDefaults()
        {
            AutoAccentColor = false;
            SmoothAccentColorSwitch = true;
            SmoothAccentColorSwitchDuration = 0.5;
            EnableDiscordRichPresence = true;
            UseDarkMode = true;
            EnableHardwareAcceleration = true;
            EnableReplayGain = false;
            SmoothPlayPause = false;
            SmoothFadeDuration = 0.5;
            LibraryScanCommonMusicFolder = false;
            LibraryScanMyMusicFolder = true;
            CustomLibraryFolders.Clear();
        }

        public void Save()
        {
            var dir = DataFolder.DataDirectory;
            var fn = Path.Combine(dir, "settings.json");

            try
            {
                string content = JsonConvert.SerializeObject(this);

                File.WriteAllText(fn, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("could not save settings");
                Debug.WriteLine(ex);
            }
        }

        public PropertyChangedEventHandler StartTrackingSetting(Action<AppSettings> action, string settingName)
        {
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == settingName)
                {
                    action(this);
                }
            };

            PropertyChanged += handler;

            return handler;
        }
        public PropertyChangedEventHandler StartTrackingSetting(Action<AppSettings> action, params string[] settingNames)
        {
            PropertyChangedEventHandler handler = (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) || settingNames.Contains(e.PropertyName))
                {
                    action(this);
                }
            };

            PropertyChanged += handler;

            return handler;
        }

        public void StopTrackingSetting(PropertyChangedEventHandler handler)
        {
            PropertyChanged -= handler;
        }
    }
}
