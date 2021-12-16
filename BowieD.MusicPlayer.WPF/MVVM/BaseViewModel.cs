using System.ComponentModel;
using System.Windows;

namespace BowieD.MusicPlayer.WPF.MVVM
{
    public abstract class BaseViewModel : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        internal void ChangeProperty<T>(ref T backField, T newValue, string propertyName)
        {
            backField = newValue;

            TriggerPropertyChanged(propertyName);
        }
        internal void ChangeProperty<T>(ref T backField, T newValue, params string[] properties)
        {
            backField = newValue;

            TriggerPropertyChanged(properties);
        }

        internal void TriggerPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        internal void TriggerPropertyChanged(params string[] properties)
        {
            foreach (var p in properties)
                TriggerPropertyChanged(p);
        }
    }
    public abstract class BaseViewModelView<TView> : BaseViewModel where TView : Window
    {
        public BaseViewModelView(TView view) : base()
        {
            this.View = view;
        }

        public TView View { get; }
    }
}
