using ICSharpCode.AvalonEdit.Document;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2.Control
{
	/// <summary>
	/// ReplaceList.xaml の相互作用ロジック
	/// </summary>
	public partial class ReplaceList : UserControl
	{
		public ReplaceList()
		{
			InitializeComponent();
		}
	}

	internal class ReplaceListViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver, IDisposable
	{
		public const string ContentId = "ReplaceList";
		public const string ReplaceFilePath = "/ghost/master/replace.txt";
		public const string ReplaceAfterFilePath = "/ghost/master/replace_after.txt";
		private const int TabIndexList = 0;

		private MainViewModel main;
		private ReplaceList control;
		private ObservableCollection<ReplaceListItemViewModel> items;
		private List<string> replaceCommonLine;
		private List<string> replaceAfterCommonLine;
		private TextDocument replaceDocument;
		private TextDocument replaceAfterDocument;
		private int currentTabIndex;
		

		public ReadOnlyObservableCollection<ReplaceListItemViewModel> Items => new ReadOnlyObservableCollection<ReplaceListItemViewModel>(items);
		public TextDocument ReplaceDocument
		{
			get => replaceDocument;
			set
			{
				replaceDocument = value;
				NotifyChanged();
			}
		}

		public TextDocument ReplaceAfterDocument
		{
			get => replaceAfterDocument;
			set
			{
				replaceAfterDocument = value;
				NotifyChanged();
			}
		}

		public int CurrentTabIndex
		{
			get => currentTabIndex;
			set
			{
				if (currentTabIndex != value)
				{
					if(currentTabIndex == TabIndexList)
					{
						//リストから別の場所へ
						ListToText();
					}
					else if(value == TabIndexList)
					{
						//別の場所からリストへ
						TextToList();
					}

					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public ActionCommand AddItemCommand { get; }
		public ActionCommand RemoveItemCommand { get; }
		public SaveFileObjectWrapper ReplaceSaveObject { get; }
		public SaveFileObjectWrapper ReplaceAfterSaveObject { get; }

		public string DockingTitle => "文字置換リスト";

		public string DockingContentId => ContentId;

		public ReplaceListViewModel(MainViewModel main)
		{
			this.main = main;
			items = new ObservableCollection<ReplaceListItemViewModel>();
			replaceCommonLine = new List<string>();
			replaceAfterCommonLine = new List<string>();
			replaceDocument = new TextDocument();
			replaceAfterDocument = new TextDocument();

			RemoveItemCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("選択中の項目を削除します。よろしいですか？", "文字置換リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//選択中のアイテムを削除
						var removes = items.Where(i => i.IsSelected).ToArray();
						foreach (var item in removes)
							RemoveItem(item);
					}
				},
				o =>
				{
					return items.Any(i => i.IsSelected);
				}
				);

			AddItemCommand = new ActionCommand(
				o =>
				{
					UnSelectAll();

					//アイテムを追加
					items.Add(new ReplaceListItemViewModel(this));
					items.Last().IsSelected = true;
					RequestScroll(items.Last());
				}
				);

			ReplaceSaveObject = new SaveFileObjectWrapper(ReplaceFilePath, SaveReplace);
			ReplaceAfterSaveObject = new SaveFileObjectWrapper(ReplaceAfterFilePath, SaveReplaceAfter);

			if (main.Ghost != null)
				Load();
		}

		public void RemoveItem(ReplaceListItemViewModel item)
		{
			item.ChangedCurrentFile();
			items.Remove(item);
		}

		public void RequestScroll(ReplaceListItemViewModel item)
		{
			control.MainList.ScrollIntoView(item);
		}

		public void UnSelectAll()
		{
			foreach (var item in items)
				item.IsSelected = false;
		}

		//ロード周り
		public void Load()
		{
			var replacePath = main.Ghost.FullPath + ReplaceFilePath;
			var replaceAfterPath = main.Ghost.FullPath + ReplaceAfterFilePath;

			//セットで考える必要がある、片方が失敗したら両方NG扱いにする
			ReplaceAfterSaveObject.LoadState = EditorLoadState.Initialized;
			ReplaceSaveObject.LoadState = EditorLoadState.Initialized;

			try
			{

				if (System.IO.File.Exists(replacePath))
				{
					var fileBody = System.IO.File.ReadAllText(replacePath, Constants.EncodingShiftJis);
					DeserializeReplace(fileBody);
				}

				if (System.IO.File.Exists(replaceAfterPath))
				{
					var fileBody = System.IO.File.ReadAllText(replaceAfterPath, Constants.EncodingShiftJis);
					DeserializeReplaceAfter(fileBody);
				}

				ReplaceSaveObject.LoadState = EditorLoadState.Loaded;
				ReplaceAfterSaveObject.LoadState = EditorLoadState.Loaded;
			}
			catch
			{
				ReplaceAfterSaveObject.LoadState = EditorLoadState.LoadFailed;
				ReplaceSaveObject.LoadState = EditorLoadState.LoadFailed;
			}
			finally
			{
				ReplaceAfterSaveObject.IsChanged = false;
				ReplaceSaveObject.IsChanged = false;
			}
		}

		//保存
		public bool SaveReplace()
		{
			if (ReplaceSaveObject.LoadState != EditorLoadState.Loaded)
				return false;

			string saveText;
			if(CurrentTabIndex == TabIndexList)
			{
				//リスト化されているのでシリアライズして保存
				saveText = SerializeReplace();
			}
			else
			{
				//テキスト編集モードなのでDocumentを保存
				saveText = ReplaceDocument.Text;
			}

			try
			{
				var fullPath = main.Ghost.FullPath + ReplaceFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, Constants.EncodingShiftJis);
				ReplaceSaveObject.IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool SaveReplaceAfter()
		{
			if (ReplaceAfterSaveObject.LoadState != EditorLoadState.Loaded)
				return false;

			string saveText;
			if(CurrentTabIndex == TabIndexList)
			{
				//リスト化されているのでシリアライズして保存
				saveText = SerializeReplaceAfter();
			}
			else
			{
				//テキスト編集モードなのでDocument保存
				saveText = ReplaceAfterDocument.Text;
			}

			try
			{
				var fullPath = main.Ghost.FullPath + ReplaceAfterFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, Constants.EncodingShiftJis);
				ReplaceSaveObject.IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void DeserializeReplace(string body)
		{
			replaceCommonLine.Clear();
			var lines = DictionaryUtility.SplitLines(body);
			
			foreach(var item in lines)
			{
				var sp = item.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
				if(sp.Length == 2)
				{
					items.Add(new ReplaceListItemViewModel(this)
					{
						Before = sp[0],
						After = sp[1],
						IsReplace = true
					});
				}
				else
				{
					replaceCommonLine.Add(item);
				}
			}
		}

		private void DeserializeReplaceAfter(string body)
		{
			replaceAfterCommonLine.Clear();
			//items.Clear();
			var lines = DictionaryUtility.SplitLines(body);

			foreach (var item in lines)
			{
				var sp = item.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
				if (sp.Length == 2)
				{
					items.Add(new ReplaceListItemViewModel(this)
					{
						Before = sp[0],
						After = sp[1],
						IsReplaceAfter = true
					});
				}
				else
				{
					replaceAfterCommonLine.Add(item);
				}
			}
		}

		private string SerializeReplace()
		{
			var lines = new List<string>();
			foreach(var item in items)
			{
				if(item.IsReplace)
					lines.Add(string.Format("{0}\t{1}", item.Before, item.After));
			}

			lines.AddRange(replaceCommonLine);
			return string.Join(Constants.NewLine, lines);
		}

		private string SerializeReplaceAfter()
		{
			var lines = new List<string>();
			foreach (var item in items)
			{
				if (item.IsReplaceAfter)
					lines.Add(string.Format("{0}\t{1}", item.Before, item.After));
			}

			lines.AddRange(replaceAfterCommonLine);
			return string.Join(Constants.NewLine, lines);
		}

		private void ListToText()
		{
			if (ReplaceDocument != null)
				ReplaceDocument.TextChanged -= ReplaceDocument_TextChanged;
			if (ReplaceAfterDocument != null)
				ReplaceAfterDocument.TextChanged -= ReplaceAfterDocument_TextChanged;

			ReplaceDocument = new TextDocument(SerializeReplace());
			ReplaceAfterDocument = new TextDocument(SerializeReplaceAfter());
			ReplaceDocument.TextChanged += ReplaceDocument_TextChanged;
			ReplaceAfterDocument.TextChanged += ReplaceAfterDocument_TextChanged;
		}

		private void ReplaceAfterDocument_TextChanged(object sender, EventArgs e)
		{
			ReplaceAfterSaveObject.Changed();
		}

		private void ReplaceDocument_TextChanged(object sender, EventArgs e)
		{
			ReplaceSaveObject.Changed();
		}

		private void TextToList()
		{
			items.Clear();
			DeserializeReplace(ReplaceDocument.Text);
			DeserializeReplaceAfter(ReplaceAfterDocument.Text);
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (ReplaceList)control;
		}

		public void Dispose()
		{
			if (ReplaceDocument != null)
				ReplaceDocument.TextChanged -= ReplaceDocument_TextChanged;
			if (ReplaceAfterDocument != null)
				ReplaceAfterDocument.TextChanged -= ReplaceAfterDocument_TextChanged;
		}
	}

	//replace.txt, replace_after.txt の２ファイルがこのエディタにあるので、それぞれの変更を示すインターフェースを用意するためのオブジェクト
	//単純な形の変更管理
	internal class SaveFileObjectWrapper : NotificationObject, ISaveFileObject
	{
		private bool isChanged;
		private EditorLoadState loadState;
		private Func<bool> saveFunc;

		public bool IsChanged 
		{
			get => isChanged;
			set
			{
				isChanged = value;
				NotifyChanged();
			}
		}

		public string SaveFilePath { get; }

		public EditorLoadState LoadState
		{
			get => loadState;
			set
			{
				loadState = value;
				NotifyChanged();
			}
		}

		public bool Save()
		{
			if( saveFunc())
			{
				IsChanged = false;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void Changed()
		{
			IsChanged = true;
		}

		public SaveFileObjectWrapper(string saveFilePath, Func<bool> saveFunc)
		{
			this.saveFunc = saveFunc;
			this.SaveFilePath = saveFilePath;
		}

		public void SetSaveFunc(Func<bool> saveFunc)
		{
			this.saveFunc = saveFunc;
		}
	}

	internal class ReplaceListItemViewModel : NotificationObject
	{
		private ReplaceListViewModel parent;
		private string before;
		private string after;
		private bool isReplaceAfter;
		private bool isSelected;

		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand RemoveSingleItemCommand { get; }

		public string Before
		{
			get => before;
			set
			{
				if (before != value)
				{
					before = value;
					NotifyChanged();
					ChangedCurrentFile();
				}
			}
		}

		public string After
		{
			get => after;
			set
			{
				if (after != value)
				{
					after = value;
					NotifyChanged();
					ChangedCurrentFile();
				}
			}
		}

		public bool IsReplaceAfter
		{
			get => isReplaceAfter;
			set
			{
				if (isReplaceAfter != value)
				{
					isReplaceAfter = value;
					NotifyChanged();
					NotifyChanged(nameof(IsReplace));
					ChangedBothFile();
				}
			}
		}

		public bool IsReplace
		{
			get => !isReplaceAfter;
			set
			{
				if (isReplaceAfter != !value)
				{
					isReplaceAfter = !value;
					NotifyChanged();
					NotifyChanged(nameof(IsReplaceAfter));
					ChangedBothFile();
				}
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

		public ReplaceListItemViewModel(ReplaceListViewModel parent)
		{
			this.parent = parent;
			isReplaceAfter = true;
			before = string.Empty;
			after = string.Empty;

			RemoveItemCommand = parent.RemoveItemCommand;
			RemoveSingleItemCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show("項目を削除します。よろしいですか？", "文字置換リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.parent.RemoveItem(this);
					}
				}
				);
		}

		public void ChangedCurrentFile()
		{
			if(isReplaceAfter)
			{
				parent.ReplaceAfterSaveObject.Changed();
			}
			else
			{
				parent.ReplaceSaveObject.Changed();
			}
		}

		public void ChangedBothFile()
		{
			parent.ReplaceAfterSaveObject.Changed();
			parent.ReplaceSaveObject.Changed();
		}
	}
}
