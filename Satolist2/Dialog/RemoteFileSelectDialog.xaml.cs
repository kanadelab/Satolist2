using Satolist2.Core;
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
using System.Windows.Threading;

namespace Satolist2.Dialog
{
	/// <summary>
	/// RemoteFileSelectDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class RemoteFileSelectDialog : DialogContentBase
	{
		public event MouseButtonEventHandler ItemDoubleClick;

		public enum DialogState
		{
			List,
			Connecting,
			Error,
			Empty
		}

		public RemoteFileSelectDialog(Window ownerWindow)
		{
			InitializeComponent();
			Owner = ownerWindow;
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ItemDoubleClick?.Invoke(sender, e);
		}
	}

	public class RemoteFileSelectDialogViewModel : NotificationObject
	{
		private bool activated;
		private bool showFileNameInput;
		private RemoteFileSelectDialog dialog;
		private ObservableCollection<FileListItem> items;
		private FileListItem selectedItem;
		private FtpClient ftpClient;
		private string currentPath;
		private string fileName;
		private RemoteFileSelectDialog.DialogState state;
		private List<CreateDirectoryRecord> createDirectory;

		public ReadOnlyObservableCollection<FileListItem> Items => new ReadOnlyObservableCollection<FileListItem>(items);
		public FileListItem SelectedItem
		{
			get => selectedItem;
			set
			{
				selectedItem = value;
				NotifyChanged();
				OkCommand.NotifyCanExecuteChanged();
			}
		}

		public ActionCommand CreateDirectoryCommand { get; }
		public ActionCommand HomeDirectoryCommand { get; }
		public ActionCommand UpDirectoryCommand { get; }
		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }

		//選択結果
		public string ResultPath { get; private set; }

		public string CurrentPath
		{
			get => currentPath;
			set
			{
				if (currentPath != value)
				{
					currentPath = value;

					//内容更新リクエスト
					State = RemoteFileSelectDialog.DialogState.Connecting;

					var req = new FtpFileListRequest(value);
					req.OnFinished += Req_OnFinished;
					ftpClient.ExecuteCommandAsync(req);
				}
				NotifyChanged();
			}
		}

		public bool ShowFileNameInput
		{
			get => showFileNameInput;
			set
			{
				showFileNameInput = value;
				NotifyChanged();
			}
		}

		public string FileName
		{
			get => fileName;
			set
			{
				fileName = value;
				NotifyChanged();
			}
		}

		public RemoteFileSelectDialog.DialogState State
		{
			get => state;
			set
			{
				state = value;
				NotifyChanged();
			}
		}

		//接続先ドメイン
		public string Domain { get; }

		public RemoteFileSelectDialogViewModel(RemoteFileSelectDialog dialog, FtpAccount account, string directory = "")
		{
			Domain = account.url;
			createDirectory = new List<CreateDirectoryRecord>();
			items = new ObservableCollection<FileListItem>();
			ftpClient = new FtpClient(account);
			currentPath = directory;
			state = RemoteFileSelectDialog.DialogState.Connecting;
			this.dialog = dialog;

			dialog.Activated += Dialog_Activated;
			dialog.ItemDoubleClick += Dialog_ItemDoubleClick;
			dialog.AddressBar.LostFocus += AddressBar_LostFocus;
			dialog.AddressBar.PreviewKeyDown += AddressBar_PreviewKeyDown;

			CreateDirectoryCommand = new ActionCommand(
				o =>
				{
					//現在のディレクトリをリストアップ
					var currentItems = items.Select(s => s.Name);
					var inputDialog = new TextInputDialog(dialog.Host);
					var vm = new CreateDirectoryDialogViewModel(inputDialog, currentItems);
					inputDialog.DataContext = vm;
					if (inputDialog.ShowDialog() == true)
					{
						createDirectory.Add(new CreateDirectoryRecord()
						{
							parent = currentPath,
							name = vm.Text
						});
						items.Insert(0, new FileListItem()
						{
							FullName = string.Concat(currentPath, "/", vm.Text),
							Name = vm.Text,
							Type = FileListItemType.NewDirectory,
							IsSelected = true
						});
					}
				}
				);

			//カラパスを入力してデフォルト位置に飛ぶ
			HomeDirectoryCommand = new ActionCommand(o => CurrentPath = string.Empty);

			//1つ上のパスに移動
			UpDirectoryCommand = new ActionCommand(
				o =>
				{
					var index = currentPath.LastIndexOf(@"/");
					if (index >= 0)
					{
						CurrentPath = currentPath.Substring(0, currentPath.LastIndexOf(@"/") + 1);
					}
				}
				);

			//キャンセル
			CancelCommand = new ActionCommand(
				o =>
				{
					dialog.DialogResult = false;
					dialog.Close();
				}
				);

			//OKボタン
			OkCommand = new ActionCommand(
				o =>
				{
					if (string.IsNullOrEmpty(FileName))
					{
						//選択されているものがあればそれ優先
						if (selectedItem != null)
							ResultPath = selectedItem.FullName;
						else
							ResultPath = currentPath;
					}
					else
					{
						if (selectedItem != null)
							ResultPath = DictionaryUtility.ConbinePath(selectedItem.FullName, fileName);
						else
							ResultPath = DictionaryUtility.ConbinePath(currentPath, fileName);
					}

					dialog.DialogResult = true;
					dialog.Close();
				},
				o =>
				{
					//ディレクトリを選択している場合のみ決定可能
					//nullはカレントを選択している扱いで許容
					if (selectedItem == null || selectedItem.Type == FileListItemType.NewDirectory || selectedItem.Type == FileListItemType.Directory)
						return true;
					else
						return false;
				}
				);

		}

		private void AddressBar_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//enterで設定内容を確定
			if(e.Key == Key.Enter)
			{
				CurrentPath = dialog.AddressBar.Text;
				e.Handled = true;
			}
		}

		private void AddressBar_LostFocus(object sender, RoutedEventArgs e)
		{
			//再反映
			NotifyChanged(nameof(CurrentPath));
		}

		//アイテムダブルクリック
		private void Dialog_ItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is ListViewItem listItem)
			{
				if (listItem.DataContext is FileListItem item && (item.Type == FileListItemType.Directory || item.Type == FileListItemType.NewDirectory ))
				{
					//階層をすすめる
					CurrentPath = item.FullName;
				}
			}
		}

		//ダイアログが開かれたら
		private void Dialog_Activated(object sender, EventArgs e)
		{
			//初回のアクティベート時のみ
			if (activated)
				return;

			//プロパティを上書きしてパス更新を走らせる
			activated = true;
			var path = currentPath;
			currentPath = null;
			CurrentPath = path;
		}

		//フォルダリストを取得した
		private void Req_OnFinished(FtpRequestBase request)
		{
			var req = (FtpFileListRequest)request;
			req.OnFinished -= Req_OnFinished;
			dialog.Dispatcher.Invoke(() => { UpdateFileList(req); });
		}

		private void UpdateFileList(FtpFileListRequest request)
		{
			//リストを書き換えるので一旦カラに
			items.Clear();

			//リザルト見つつ設定
			if (request.IsSucceed)
			{
				//デフォルト位置を参照しているようであれば、実際のパスに書き換える
				if (string.IsNullOrEmpty(currentPath))
				{
					currentPath = request.ResultCurrentPath;
					NotifyChanged(nameof(CurrentPath));
				}

				foreach(var item in createDirectory)
				{
					if(item.parent == currentPath)
					{
						items.Add(new FileListItem()
						{ 
							FullName = string.Concat(currentPath, "/", item.name),
							Name = item.name,
							Type = FileListItemType.NewDirectory
						});
					}
				}

				foreach (var item in request.Result)
				{
					var data = new FileListItem();
					data.Name = item.Name;
					data.FullName = item.FullName;

					if (item.Type == FluentFTP.FtpFileSystemObjectType.Directory)
					{
						data.Type = FileListItemType.Directory;
						items.Add(data);
					}
					else if (item.Type == FluentFTP.FtpFileSystemObjectType.File)
					{
						data.Type = FileListItemType.File;
						items.Add(data);
					}
				}

				if (items.Count > 0)
					State = RemoteFileSelectDialog.DialogState.List;
				else
					State = RemoteFileSelectDialog.DialogState.Empty;
			}
			else
			{
				State = RemoteFileSelectDialog.DialogState.Error;
			}
		}

		struct CreateDirectoryRecord
		{
			public string parent;
			public string name;
		}
	}

	public enum FileListItemType
	{
		Directory,
		File,
		Ghost,			//ななろだのこと
		NewDirectory	//仮の新規作成ディレクトリ
	}

	public class FileListItem : NotificationObject
	{
		bool isSelected;
		public FileListItemType Type { get; set; }
		public string FullName { get; set; }
		public string Name { get; set; }
		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}
	}

	public class CreateDirectoryDialogViewModel : NotificationObject
	{
		private string text;
		private string[] existsFiles;
		private TextInputDialog dialog;

		public string Description => "追加するフォルダ名を設定します。\n※実際にサーバ上にフォルダが作られるのはアップロード時です。";
		public string Title => "新規フォルダの作成";
		public string Wartermark => "フォルダ名";

		public ActionCommand OkButtonCommand { get; }

		public string Text
		{
			get => text;
			set
			{
				text = value;
				NotifyChanged();
				NotifyChanged(nameof(ErrorMessage));
				OkButtonCommand.NotifyCanExecuteChanged();
			}
		}

		public string ErrorMessage
		{
			get
			{
				if(!DictionaryUtility.IsValidFileName(Text))
				{
					return "フォルダ名で使用できない文字が使用されています。";
				}
				if(existsFiles.FirstOrDefault(o => o == Text) != null)
				{
					return "既に存在する名前が入力されています。";
				}
				return string.Empty;
			}
		}

		public CreateDirectoryDialogViewModel(TextInputDialog dialog, IEnumerable<string> currentDirectoryFiles)
		{
			this.dialog = dialog;
			this.existsFiles = currentDirectoryFiles?.ToArray() ?? Array.Empty<string>();

			text = string.Empty;

			OkButtonCommand = new ActionCommand
				(
				o =>
				{
					dialog.DialogResult = true;
					dialog.Close();
				},
				o =>
				{
					//エラーが出てなければ実行可能
					return string.IsNullOrEmpty(ErrorMessage);
				}
				);
		}

	}
}
