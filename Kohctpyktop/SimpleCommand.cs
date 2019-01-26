using System;
using System.Windows.Input;

namespace Kohctpyktop
{
    public class SimpleCommand : ICommand
    {
        private readonly Action _executeAction;
        private readonly Func<bool> _canExecuteFunc;

        public SimpleCommand(Action executeAction, Func<bool> canExecuteFunc = null)
        {
            _executeAction = executeAction ?? delegate {};
            _canExecuteFunc = canExecuteFunc ?? (() => true);
        }
        public bool CanExecute(object parameter)
        {
            return _canExecuteFunc();
        }

        public void Execute(object parameter)
        {
            _executeAction();
        }

        public event EventHandler CanExecuteChanged;
    }
}