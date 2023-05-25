using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
	public partial class ProgressDialog : DialogContentBase
	{
		//ダイアログのキャンセル機能
		public CancellationTokenSource Cancellation { get; }
		//キャンセル可能かどうか
		public bool Cancellable { get; private set; }

		internal new ProgressDialogViewModel DataContext
		{
			get => (ProgressDialogViewModel)base.DataContext;
			set
			{
				base.DataContext = value;
			}
		}

		internal ProgressDialog()
		{
			InitializeComponent();
			Cancellation = new CancellationTokenSource();
			DataContext = new ProgressDialogViewModel(this);
			
		}

		public void SetTask(Task progressTask, bool isCancellable = false)
		{
			Cancellable = isCancellable;
			
			//キャンセル可能かどうかで表示が変わる
			if(Cancellable)
			{
				CloseButton.Visibility = Visibility.Collapsed;
				CancelButton.Visibility = Visibility.Visible;
			}
			else
			{
				CancelButton.IsEnabled = false;
			}

			progressTask.ContinueWith((t) =>
			{
				Dispatcher.Invoke(() =>
				{
					CancelButton.Visibility = Visibility.Collapsed;
					CancelButton.IsEnabled = false;
					CloseButton.Visibility = Visibility.Visible;
					CloseButton.IsEnabled = true;
				});
			});
		}

		//OKボタン
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//処理終了。
			Close();
		}

		//キャンセルボタン
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			//キャンセルを伝える
			CancelButton.IsEnabled = false;
			Cancellation.Cancel();
		}
	}

	internal class ProgressDialogViewModel : NotificationObject
	{
		private bool isIndeterminate;
		private double progress;
		private string title;
		private string message;
		private string logs;
		private string stableMessage;
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

		public string StableMessage
		{
			get => stableMessage;
			set
			{
				stableMessage = value;
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
			dialog.MainTextBox.ScrollToEnd();
		}

		public void SetMessage(string message, double progress)
		{
			SetMessage(message);
			Progress = progress;
		}

		public void SetProgress(Progress progress)
		{
			SetMessage(progress.Message);
			IsIndeterminate = !progress.UseProgress;
			Progress = progress.Value;
		}

		public void SetCompletedProgress()
		{
			Progress = 100.0;
			IsIndeterminate = false;
		}

		public void SetFailedProgress()
		{
			Progress = 0.0;
			IsIndeterminate = false;
		}
	}
}
