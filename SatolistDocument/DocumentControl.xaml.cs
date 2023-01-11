using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SatolistDocument
{
	/// <summary>
	/// DocumentControl.xaml の相互作用ロジック
	/// </summary>
	public partial class DocumentControl : UserControl
	{
		public DocumentControl()
		{
			InitializeComponent();
			DataContext = new DocumentViewModel();
		}
	}

	public class DocumentViewModel : NotificationObject
	{
		private bool isMenuOpened_;
		
		public ActionCommand ShowMenuCommand { get; }

		public bool IsMenuOpened
		{
			get => isMenuOpened_;
			set
			{
				isMenuOpened_ = value;
				NotifyChanged();
			}
		}

		public DocumentViewModel()
		{
			ShowMenuCommand = new ActionCommand(
				o =>
				{
					IsMenuOpened = true;
				});
		}
	}

	/// <summary>
	/// ドキュメントページの情報
	/// </summary>
	public class DocumentPage
	{
		public DataTemplate PageTemplate { get; set; }
		public string Label { get; set; }
	}

	public class NotificationObject : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class ActionCommand : ICommand
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
			if (canExecuteFunc != null)
			{
				return canExecuteFunc.Invoke(parameter);
			}
			return true;
		}

		public void Execute(object parameter)
		{
			executeAction?.Invoke(parameter);
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}
	}
}
