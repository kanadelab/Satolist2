using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Satolist2.Control
{
	/// <summary>
	/// UpdateIgnoreList.xaml の相互作用ロジック
	/// </summary>
	public partial class UpdateIgnoreList : UserControl
	{
		public event Action<string[]> OnFileDrop;

		public UpdateIgnoreList()
		{
			InitializeComponent();

			MainList.DragOver += MainList_DragOver;
			MainList.Drop += MainList_Drop;
			MainList.AllowDrop = true;
		}

		private void MainList_Drop(object sender, DragEventArgs e)
		{
			e.Handled = true;
			var items = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (items == null)
				return;
			OnFileDrop?.Invoke(items);
		}

		private void MainList_DragOver(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Link;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}

			e.Handled = true;
		}

		public void RequestScroll(object itemViewModel)
		{
			MainList.ScrollIntoView(itemViewModel);
		}
	}

	internal class UpdateIgnoreListViewModel : NotificationObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		//有効レコードかどうかを判定するためのもの
		private static readonly Regex NoNarNoUpdateRegex = new Regex("(nonar|noupdate)(,nonar|,noupdate)?");
		private const int TAB_INDEX_LIST = 0;

		private ObservableCollection<UpdateIgnoreListItemViewModel> items;

		private TextDocument developerOptionsText;
		private TextDocument deleteText;
		private MainViewModel main;
		private UpdateIgnoreList control;
		private int currentTabIndex;
		private List<string> deleteCommonLine;  //コメントリスト行？
		private List<string> developerOptionsCommonLine;

		public GhostModel Ghost => main.Ghost;

		public ReadOnlyObservableCollection<UpdateIgnoreListItemViewModel> Items => new ReadOnlyObservableCollection<UpdateIgnoreListItemViewModel>(items);

		public TextDocument DeveloperOptionsText
		{
			get => developerOptionsText;
			set
			{
				developerOptionsText = value;
				NotifyChanged();
			}
		}

		public TextDocument DeleteText
		{
			get => deleteText;
			set
			{
				deleteText = value;
				NotifyChanged();
			}
		}

		public int CurrentTabIndex
		{
			get => currentTabIndex;
			set
			{
				if(currentTabIndex != value)
				{
					if(currentTabIndex == TAB_INDEX_LIST)
					{
						//リストから別の場所へ
						ListToText();
					}
					else if(value == TAB_INDEX_LIST)
					{
						//別の場所からリストへ
						TextToList();
					}
				}

				currentTabIndex = value;
				NotifyChanged();
			}
		}

		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand AddItemCommand { get; }

		public string DockingTitle => "更新除外設定";

		public string DockingContentId => "UpdateIgnoreList";

		public UpdateIgnoreListViewModel(MainViewModel main)
		{
			this.main = main;

			items = new ObservableCollection<UpdateIgnoreListItemViewModel>();
			developerOptionsCommonLine = new List<string>();
			deleteCommonLine = new List<string>();

			RemoveItemCommand = new ActionCommand(
				(o) =>
				{
					if (MessageBox.Show("選択中の項目を削除します。よろしいですか？", "更新除外設定", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//選択中のアイテムを削除
						var removes = items.Where(i => i.IsSelected).ToArray();
						foreach (var item in removes)
							items.Remove(item);
					}
				},
				(o) =>
				{
					return items.FirstOrDefault(i => i.IsSelected) != null;
				}
				);

			AddItemCommand = new ActionCommand(
				(o) =>
				{
					UnSelectAll();

					//アイテムを追加
					items.Add(new UpdateIgnoreListItemViewModel(this));

					//末尾追加のはずなので、追加したアイテムのとこに移動する
					items.Last().IsSelected = true;
					control.RequestScroll(items.Last());
				}
				);

			//読み込み処理
			if(main.Ghost != null)
				Load();
		}

		//リスト上にファイルドロップ
		private void Control_OnFileDrop(string[] addItems)
		{
			UnSelectAll();

			foreach(var item in addItems)
			{
				if(!DictionaryUtility.IsChildPath(main.Ghost.FullPath, item))
				{
					//非対応パス
					ShowErrorRelativePathDialog(item);
					return;
				}
			}

			//追加対象についてチェック
			foreach(var item in addItems)
			{
				//ファイルを追加
				var addItem = new UpdateIgnoreListItemViewModel(this);
				addItem.IsSelected = true;
				addItem.Path = DictionaryUtility.MakeRelativePath(main.Ghost.FullPath, item);
				items.Add(addItem);
			}

			//末尾追加のはずなので、追加したアイテムのとこに移動する
			control.RequestScroll(items.Last());
		}

		private void UnSelectAll()
		{
			foreach (var item in items)
				item.IsSelected = false;
		}

		public void RemoveItem(UpdateIgnoreListItemViewModel item)
		{
			items.Remove(item);
		}

		//エラーダイアログ
		public static void ShowErrorRelativePathDialog(string path)
		{
			MessageBox.Show(string.Format("{0}\r\n{1}", path, "ゴーストの配下のファイルではないため追加できません。"), "更新除外リスト", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		//読み書き系
		public void Load()
		{
			var deletePath = main.Ghost.FullPath + "/delete.txt";
			var deveoperOptionsPath = main.Ghost.FullPath + "/developer_options.txt";

			if(System.IO.File.Exists(deveoperOptionsPath))
			{
				var fileBody = System.IO.File.ReadAllText(deveoperOptionsPath, Constants.EncodingShiftJis);
				DeserializeDeveloperOptions(fileBody);
			}

			if (System.IO.File.Exists(deletePath))
			{
				var fileBody = System.IO.File.ReadAllText(deletePath, Constants.EncodingShiftJis);
				DeserializeDelete(fileBody);
			}

		}

		public void AddDeveloperOptionsRecord(string path, bool nonar, bool noupdate)
		{
			//すでに存在するレコードは統合する
			path = DictionaryUtility.NormalizePath(path);
			var record = items.FirstOrDefault(o => o.Path == path);
			if(record != null)
			{
				record.IsNoNar = nonar;
				record.IsNoUpdate = noupdate;
			}
			else
			{
				record = new UpdateIgnoreListItemViewModel(this);
				record.Path = path;
				record.IsNoNar = nonar;
				record.IsNoUpdate = noupdate;
				record.IsDelete = false;
				items.Add(record);
			}
		}

		public void AddDeleteRecord(string path)
		{
			path = DictionaryUtility.NormalizePath(path);
			var record = items.FirstOrDefault(o => o.Path == path);
			if(record != null)
			{
				record.IsDelete = true;
				record.IsNoNar = false;
				record.IsNoUpdate = false;
			}
			else
			{
				items.Add(new UpdateIgnoreListItemViewModel(this) { Path = path, IsDelete = true });
			}
		}

		public void DeserializeDeveloperOptions(string body)
		{
			//リセット
			developerOptionsCommonLine.Clear();

			//csvをロード
			var csvBuilder = new CsvBuilder();
			csvBuilder.Deserialize(body);

			//フォーマットに沿っているデータを検索
			foreach(var item in csvBuilder.Records)
			{
				//ファイルパスではないレコードは無視。"//"で始まる行もコメントとしてみなしておく
				bool isInvalid = false;
				if((item.Key?.IndexOfAny(System.IO.Path.GetInvalidPathChars()) ?? -1) >= 0)
				{
					isInvalid = true;
				}
				else if((item.Key?.IndexOf("//") ?? -1) == 0)
				{
					isInvalid = true;
				}

				if(!isInvalid && NoNarNoUpdateRegex.IsMatch(item.Value) && !string.IsNullOrEmpty(item.Key))
				{
					//developeroptionsとして有効なのでレコードに追加
					bool isnonar = item.Value.IndexOf("nonar") >= 0;
					bool isnoupdate = item.Value.IndexOf("noupdate") >= 0;
					AddDeveloperOptionsRecord(item.Key, isnonar, isnoupdate);
					continue;
				}

				if(!string.IsNullOrEmpty(item.Key))
				{
					developerOptionsCommonLine.Add(string.Concat(item.Key, ",", item.Value));
				}
				else
				{
					developerOptionsCommonLine.Add(item.Value);
				}
			}
		}

		public void DeserializeDelete(string body)
		{
			//リセット
			deleteCommonLine.Clear();

			//csvではない。ファイルパスの列挙が基本。
			var lines = Satolist2.Utility.DictionaryUtility.SplitLines(body);

			foreach(var item in lines)
			{
				//パスではなさそうなレコードを弾く
				bool isInvalid = false;
				if (item.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
				{
					isInvalid = true;
				}
				else if (item.IndexOf("//") == 0)
				{
					isInvalid = true;
				}
				if (!isInvalid && !string.IsNullOrEmpty(item))
				{
					AddDeleteRecord(item);
				}
				else
				{
					deleteCommonLine.Add(item);
				}
			}
		}

		public string SerializeDeveloperOptions()
		{
			var lines = new List<string>();

			foreach(var item in items)
			{
				if(item.IsNoNar && item.IsNoUpdate)
				{
					lines.Add(string.Concat(item.Path, ",nonar,noupdate"));
				}
				else if(item.IsNoNar)
				{
					lines.Add(string.Concat(item.Path, ",nonar"));
				}
				else if(item.IsNoUpdate)
				{
					lines.Add(string.Concat(item.Path, ",noupdate"));
				}
			}

			foreach (var item in developerOptionsCommonLine)
				lines.Add(item);

			return string.Join("\r\n", lines);
		}

		public string SerializeDelete()
		{
			var lines = new List<string>();

			foreach(var item in items)
			{
				if (item.IsDelete)
					lines.Add(DictionaryUtility.NormalizeDeletePath(item.Path));
			}

			foreach (var item in deleteCommonLine)
				lines.Add(item);

			return string.Join("\r\n", lines);
		}

		public void ListToText()
		{
			//シリアライズ操作
			DeveloperOptionsText = new TextDocument(SerializeDeveloperOptions());
			DeleteText = new TextDocument(SerializeDelete());
		}

		public void TextToList()
		{
			//デシリアライズ操作
			items.Clear();
			DeserializeDeveloperOptions(DeveloperOptionsText.Text);
			DeserializeDelete(DeleteText.Text);
		}

		//ViewModelが外されたときに破棄として呼ばれる
		public void Dispose()
		{
			control.OnFileDrop -= Control_OnFileDrop;
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (UpdateIgnoreList)control;
			this.control.OnFileDrop += Control_OnFileDrop;
		}
	}

	internal class UpdateIgnoreListItemViewModel : NotificationObject
	{
		private UpdateIgnoreListViewModel parent;
		private string path;
		private bool isNoNar;
		private bool isNoUpdate;
		private bool isDelete;
		private bool isSelected;
		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand RemoveSingleItemCommand { get; }
		public ActionCommand FileOpenCommand { get; }

		//指定パス
		public string Path
		{
			get => path;
			set
			{
				if (value.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
					return;

				path = value;
				NotifyChanged();
			}
		}

		//developer_options.txt nonar
		public bool IsNoNar
		{
			get => isNoNar;
			set
			{
				isNoNar = value;
				NotifyChanged();
				NotifyChanged(nameof(IsInvalid));
			}
		}

		//developer_options.txt noupdate
		public bool IsNoUpdate
		{
			get => isNoUpdate;
			set
			{
				isNoUpdate = value;
				NotifyChanged();
				NotifyChanged(nameof(IsInvalid));
			}
		}

		//delete.txt
		public bool IsDelete
		{
			get => isDelete;
			set
			{
				isDelete = value;
				NotifyChanged();
				NotifyChanged(nameof(IsInvalid));
			}
		}

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}

		//無効なレコード。
		//テキスト化したら消滅する
		public bool IsInvalid
		{
			get => !isDelete && !isNoUpdate && !isNoNar;
		}

		public UpdateIgnoreListItemViewModel(UpdateIgnoreListViewModel parent)
		{
			this.parent = parent;

			//デフォルト。
			path = string.Empty;
			isNoNar = true;
			isNoUpdate = true;

			RemoveItemCommand = parent.RemoveItemCommand;
			RemoveSingleItemCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("項目を削除します。よろしいですか？", "更新除外設定", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.parent.RemoveItem(this);
					}
				}
				);

			FileOpenCommand = new ActionCommand(
				o =>
				{
					//NOTE: filenameに適当な名前を入れてChickFileExistsを無効にしてるとフォルダも選べるけど、もうちょっといい方法ないかな…
					//フォルダ選択ダイアログのは正直使いにくいんだ…
					var dialog = new OpenFileDialog();
					dialog.InitialDirectory = parent.Ghost.FullPath;
					dialog.CheckFileExists = false;
					dialog.FileName = "ファイルを選択";
					dialog.Multiselect = false;

					if( dialog.ShowDialog() == true)
					{
						if(!DictionaryUtility.IsChildPath(parent.Ghost.FullPath, dialog.FileName))
						{
							UpdateIgnoreListViewModel.ShowErrorRelativePathDialog(dialog.FileName);
							return;
						}
						Path = DictionaryUtility.MakeRelativePath(parent.Ghost.FullPath, dialog.FileName);
					}
				}
				);
		}
	}

}
