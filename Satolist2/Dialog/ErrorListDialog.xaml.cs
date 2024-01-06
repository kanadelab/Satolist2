using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// ErrorListDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class ErrorListDialog : DialogContentBase
	{
		internal new ErrorListDialogViewModel DataContext
		{
			get => (ErrorListDialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		internal ErrorListDialog(MainViewModel main, bool isYesNoButton)
		{
			InitializeComponent();
			Owner = main?.MainWindow.RootWindow;
			DataContext = new ErrorListDialogViewModel(this);

			if(isYesNoButton)
			{
				OkButtonPanel.Visibility = Visibility.Collapsed;
				YesNoButtonPanel.Visibility = Visibility.Visible;
			}
			else
			{
				OkButtonPanel.Visibility = Visibility.Visible;
				YesNoButtonPanel.Visibility = Visibility.Collapsed;
			}
		}
	}

	//エラーリスト、主に概要+説明
	internal class ErrorListDialogViewModel : NotificationObject
	{
		private ObservableCollection<ErrorListDialogItemViewModel> items;
		private string title;
		private string description;

		public ErrorListDialog Dialog { get; }
		public CustomizedReadOnlyObservableCollection<ErrorListDialogItemViewModel> Items
		{
			get => new CustomizedReadOnlyObservableCollection<ErrorListDialogItemViewModel>(items);
		}

		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }

		//タイトルバー
		public string Title
		{
			get => title;
			set
			{
				title = value;
				NotifyChanged();
			}
		}

		//トップの説明
		public string Description
		{
			get => description;
			set
			{
				description = value;
				NotifyChanged();
			}
		}

		public ErrorListDialogViewModel(ErrorListDialog dialog)
		{
			Dialog = dialog;
			items = new ObservableCollection<ErrorListDialogItemViewModel>();

			OkCommand = new ActionCommand(
				o =>
				{
					Dialog.DialogResult = true;
					Dialog.Close();
				}
				);

			CancelCommand = new ActionCommand(
				o =>
				{
					Dialog.DialogResult = false;
					Dialog.Close();
				}
				);
		}

		//エラー内容の設定
		public void SetErrorItems(IEnumerable<ErrorListDialogItemViewModel> errorItems)
		{
			items.Clear();
			foreach (var item in errorItems)
				items.Add(item);
		}
	}

	//エラー内容ごと
	internal class ErrorListDialogItemViewModel : NotificationObject
	{
		private string title;
		private string description;

		//主にファイル名想定
		public string Title
		{
			get => title;
			set
			{
				title = value;
				NotifyChanged();
			}
		}

		//説明
		public string Description
		{
			get => description;
			set
			{
				description = value;
				NotifyChanged();
			}
		}
	}
}
