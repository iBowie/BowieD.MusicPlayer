using BowieD.MusicPlayer.WPF.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BowieD.MusicPlayer.WPF.Extensions
{
    public class LoopModeConverter<T> : IValueConverter
    {
        public LoopModeConverter()
        {

        }

        public T LoopNone { get; set; }
        public T LoopOne { get; set; }
        public T LoopQueue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ELoopMode loopMode)
            {
                switch (loopMode)
                {
                    case ELoopMode.NONE:
                        return LoopNone;
                    case ELoopMode.CURRENT:
                        return LoopOne;
                    case ELoopMode.QUEUE:
                        return LoopQueue;
                }
            }

            throw new Exception();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
