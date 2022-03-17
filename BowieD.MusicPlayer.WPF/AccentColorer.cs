using System.Windows.Media;

namespace BowieD.MusicPlayer.WPF
{
    public static class AccentColorer
    {
        public static void SetAccentColor(Color themeColor)
        {
            (SolidColorBrush brush, Color color) createVariant(byte opacity, Color color)
            {
                Color brushColor = Color.FromArgb(opacity, color.R, color.G, color.B);

                SolidColorBrush scb = new(brushColor);

                scb.Freeze();

                return (scb, brushColor);
            }

            var accentBase = createVariant(byte.MaxValue, themeColor);
            var accent = createVariant(0xCC, themeColor);
            var accent2 = createVariant(0x99, themeColor);
            var accent3 = createVariant(0x66, themeColor);
            var accent4 = createVariant(0x33, themeColor);
            var highlight = createVariant(byte.MaxValue, Color.Multiply(themeColor, 0.75f));

            App.Current.Resources["MahApps.Colors.AccentBase"] = accentBase.color;
            App.Current.Resources["MahApps.Colors.Accent"] = accent.color;
            App.Current.Resources["MahApps.Colors.Accent2"] = accent2.color;
            App.Current.Resources["MahApps.Colors.Accent3"] = accent3.color;
            App.Current.Resources["MahApps.Colors.Accent4"] = accent4.color;
            App.Current.Resources["MahApps.Colors.Highlight"] = highlight.color;

            App.Current.Resources["MahApps.Brushes.AccentBase"] = accentBase.brush;
            App.Current.Resources["MahApps.Brushes.Accent"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.Accent2"] = accent2.brush;
            App.Current.Resources["MahApps.Brushes.Accent3"] = accent3.brush;
            App.Current.Resources["MahApps.Brushes.Accent4"] = accent4.brush;
            App.Current.Resources["MahApps.Brushes.Highlight"] = highlight.brush;

            App.Current.Resources["MahApps.Brushes.WindowTitle"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.TextBlock.FloatingMessage"] = accentBase.brush;
            App.Current.Resources["MahApps.Brushes.Badged.Background"] = accentBase.brush;
            App.Current.Resources["MahApps.Brushes.Dialog.Background.Accent"] = highlight.brush;
            App.Current.Resources["MahApps.Brushes.Dialog.Glow"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.CheckmarkFill"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.RightArrowFill"] = accent.brush;

            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.Background"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.Background.Inactive"] = accent3.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.Background.MouseOver"] = accent2.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.BorderBrush"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.BorderBrush.Focus"] = accent.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.BorderBrush.Inactive"] = accent3.brush;
            App.Current.Resources["MahApps.Brushes.DataGrid.Selection.BorderBrush.MouseOver"] = accent2.brush;

            LinearGradientBrush lgb = new()
            {
                StartPoint = new(1.002, 0.5),
                EndPoint = new(0.001, 0.5),
            };

            lgb.GradientStops.Add(new(highlight.color, 0));
            lgb.GradientStops.Add(new(accent3.color, 1));

            lgb.Freeze();

            App.Current.Resources["MahApps.Brushes.Progress"] = lgb;
        }
    }
}
