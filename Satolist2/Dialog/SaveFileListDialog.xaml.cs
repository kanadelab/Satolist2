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
	/// SaveFileListDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class SaveFileListDialog : DialogContentBase
	{
		public MessageBoxResult Result { get; private set; }

		internal SaveFileListDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Yes;
			Close();
		}

		private void DisposeButton_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.No;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Result = MessageBoxResult.Cancel;
			Close();
		}

	}

	internal class SaveFileListViewModel
	{
		public SaveFileListItemViewModel[] Items { get; }

		public SaveFileListViewModel(IEnumerable<ISaveFileObject> saveFiles)
		{
			//変更があったものだけフィルタしてリストアップ
			Items = saveFiles.Where( o=> o.IsChanged).Select( o => new SaveFileListItemViewModel(o)).ToArray();
		}
	}

	internal class SaveFileListItemViewModel : NotificationObject
	{
		private bool isSave;

		public ISaveFileObject SaveItem { get; }
		public string SaveFilePath
		{
			get
			{
				//見栄え的に最初のスラッシュは消しておく
				//スラッシュ付けない仕様に固定するのでもいいかも
				if(SaveItem.SaveFilePath.IndexOf('/') == 0)
				{
					return SaveItem.SaveFilePath.Substring(1);
				}
				else
				{
					return SaveItem.SaveFilePath;
				}
			}
		}

		public bool IsSave
		{
			get => isSave;
			set
			{
				isSave = value;
				NotifyChanged();
			}
		}

		public SaveFileListItemViewModel(ISaveFileObject obj)
		{
			isSave = true;
			SaveItem = obj;
		}
	}

	
}
