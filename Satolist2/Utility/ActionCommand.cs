using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Satolist2.Utility
{
	internal class ActionCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;
		private Action<object> executeAction;
		private Func<object, bool> canExecuteFunc;

		public ActionCommand(Action<object> execute, bool canExecute = true)
		{
			executeAction = execute;
			canExecuteFunc = (o) => canExecute;
		}

		public ActionCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			executeAction = execute;
			canExecuteFunc = canExecute;
		}
		

		public bool CanExecute(object parameter)
		{
			if(canExecuteFunc != null)
			{
				return canExecuteFunc.Invoke(parameter);
			}
			return true;
		}

		public void Execute(object parameter)
		{
			executeAction?.Invoke(parameter);
		}
	}
}
