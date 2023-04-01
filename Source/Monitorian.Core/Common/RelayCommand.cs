using System;
using System.Windows.Input;

namespace Monitorian.Core.Common
{
	public class RelayCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public event EventHandler CanExecuteChanged;

		public RelayCommand(Action execute)
		{
			_execute = execute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute?.Invoke() != false;
		}

		public RelayCommand(Action execute, Func<bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public void Execute(object parameter)
		{
			_execute();
		}
	}
}
