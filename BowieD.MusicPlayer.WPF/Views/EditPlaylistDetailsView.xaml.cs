using BowieD.MusicPlayer.WPF.Models;
using BowieD.MusicPlayer.WPF.MVVM;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Логика взаимодействия для EditPlaylistDetailsView.xaml
    /// </summary>
    public partial class EditPlaylistDetailsView : MetroWindow
    {
        public EditPlaylistDetailsView(Playlist playlist)
        {
            InitializeComponent();

            mainPanel.DataContext = this;

            PlaylistName = playlist.Name;
            PlaylistPictureData = playlist.PictureData;

            ResultInfo = playlist;
        }

        public Playlist ResultInfo { get; private set; }

        public string PlaylistName
        {
            get { return (string)GetValue(PlaylistNameProperty); }
            set { SetValue(PlaylistNameProperty, value); }
        }

        public static readonly DependencyProperty PlaylistNameProperty = DependencyProperty.Register("PlaylistName", typeof(string), typeof(EditPlaylistDetailsView), new PropertyMetadata(string.Empty));

        public byte[] PlaylistPictureData
        {
            get { return (byte[])GetValue(PlaylistPictureDataProperty); }
            set { SetValue(PlaylistPictureDataProperty, value); }
        }

        public static readonly DependencyProperty PlaylistPictureDataProperty = DependencyProperty.Register("PlaylistPictureData", typeof(byte[]), typeof(EditPlaylistDetailsView), new PropertyMetadata(Array.Empty<byte>()));

        private ICommand? _saveCommand;

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null)
                {
                    _saveCommand = new BaseCommand(() =>
                    {
                        ResultInfo = new Playlist(ResultInfo.ID, PlaylistName, ResultInfo.SongFileNames, PlaylistPictureData);

                        DialogResult = true;
                        Close();
                    });
                }

                return _saveCommand;
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog ofd = new()
            {
                Filter = ImageTool.FileDialogFilter
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var data = File.ReadAllBytes(ofd.FileName);

                    PlaylistPictureData = ImageTool.ResizeInByteArray(data, 700, 700);
                }
                catch { }
            }
        }
    }
}
