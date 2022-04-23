using Satolist2.Utility;
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

namespace Satolist2.Dialog
{
	/// <summary>
	/// ProgressDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class ProgressDialog : Window
	{
		internal new ProgressDialogViewModel DataContext
		{
			get => (ProgressDialogViewModel)base.DataContext;
			set
			{
				base.DataContext = value;
			}
		}


		public ProgressDialog()
		{
			InitializeComponent();
			DataContext = new ProgressDialogViewModel(this);
			
		}

		public void SetTask(Task progressTask)
		{
			progressTask.ContinueWith((t) =>
			{
				Dispatcher.Invoke(() =>
				{
					CloseButton.IsEnabled = true;
				});
			});
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//処理終了。
			Close();
		}
	}

	internal class ProgressDialogViewModel : NotificationObject
	{
		private bool isIndeterminate;
		private double progress;
		private string title;
		private string message;
		private string logs;
		private ProgressDialog dialog;

		public bool IsIndeterminate
		{
			get => isIndeterminate;
			set
			{
				isIndeterminate = value;
				NotifyChanged();
			}
		}

		public double Progress
		{
			get => progress;
			set
			{
				progress = value;
				NotifyChanged();
			}
		}

		public string Title
		{
			get => title;
			set
			{
				title = value;
				NotifyChanged();
			}
		}

		public string Message
		{
			get => message;
			set
			{
				message = value;
				NotifyChanged();
			}
		}

		public string Logs
		{
			get => logs;
			set
			{
				logs = value;
				NotifyChanged();
			}
		}

		public ProgressDialogViewModel(ProgressDialog dialog)
		{
			this.dialog = dialog;
		}

		//ログに流しつつメッセージ表示
		public void SetMessage(string message)
		{
			Message = message;
			Logs = Logs + message + Constants.NewLine;
		}

		public void SetMessage(string message, double progress)
		{
			SetMessage(message);
			Progress = progress;
		}
	}
}
