using System;
using System.Windows.Input;

namespace BowieD.MusicPlayer.WPF.MVVM
{
    public class GenericCommand<T> : ICommand
    {
        private readonly Action<T?> _action;
        private readonly Func<T?, bool>? _canExecute;

        public GenericCommand(Action<T?> action)
        {
            _action = action;
            _canExecute = null;
        }
        public GenericCommand(Action<T?> action, Func<T?, bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute is null)
                return true;

            return _canExecute((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            _action((T?)parameter);
        }
    }

    public class BaseCommand : GenericCommand<object>
    {
        public BaseCommand(Action<object?> action) : base(action) { }
        public BaseCommand(Action<object?> action, Func<object?, bool> canExecute) : base(action, canExecute) { }
        public BaseCommand(Action action) : base(p => action())
        {

        }
        public BaseCommand(Action action, Func<bool> canExecute) : base(p => action(), p => canExecute())
        {

        }
    }
}
