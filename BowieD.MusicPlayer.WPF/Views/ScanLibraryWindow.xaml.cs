using BowieD.MusicPlayer.WPF.ViewModels;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BowieD.MusicPlayer.WPF.Views
{
    /// <summary>
    /// Логика взаимодействия для ScanLibraryWindow.xaml
    /// </summary>
    public partial class ScanLibraryWindow : MetroWindow
    {
        public ScanLibraryWindow()
        {
            InitializeComponent();

            this.ViewModel = new(this);

            this.DataContext = this.ViewModel;
        }

        public ScanLibraryWindowViewModel ViewModel { get; }
    }
}
