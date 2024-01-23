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
	/// VariableList.xaml の相互作用ロジック
	/// </summary>
	public partial class VariableList : UserControl
	{
		public VariableList()
		{
			InitializeComponent();
			
		}
	}

	internal class VariableListViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver, IDisposable
	{
		public const string ContentId = "VariableList";
		private const int TabIndexList = 0;

		public MainViewModel Main { get; }
		private VariableList control;
		private ObservableCollection<VariableListItemViewModel> items;
		private int currentTabIndex;
		private List<string> commonLines;
		private ListEditorSerializingScope serializingScope;

		public CustomizedReadOnlyObservableCollection<VariableListItemViewModel> Items => new CustomizedReadOnlyObservableCollection<VariableListItemViewModel>(items);

		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand AddItemCommand { get; }

		public int CurrentTabIndex
		{
			get => currentTabIndex;
			set
			{
				if (currentTabIndex != value)
				{
					if (value == TabIndexList)
						TextToList();
					else
						ListToText();

					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public string DockingTitle => "変数設定";

		public string DockingContentId => ContentId;

		public VariableListViewModel(MainViewModel main)
		{
			Main = main;
			items = new ObservableCollection<VariableListItemViewModel>();
			commonLines = new List<string>();
			serializingScope = new ListEditorSerializingScope();

			RemoveItemCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show("選択中の項目を削除します。よろしいですか？", "変数リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//選択中のアイテムを削除
						var removes = items.Where(i => i.IsSelected).ToArray();
						foreach (var item in removes)
							items.Remove(item);
						Main.SatoriConfViewModel.Changed();
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
					items.Add(new VariableListItemViewModel(this, string.Empty, string.Empty));
					items.Last().IsSelected = true;
					RequestScroll(items.Last());
					Main.SatoriConfViewModel.Changed();
				}
				);

			if (main.Ghost != null)
				Load();
		}

		public void UnSelectAll()
		{
			foreach (var item in items)
				item.IsSelected = false;
		}

		public void RequestScroll(object item)
		{
			control.MainList.ScrollIntoView(item);
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (VariableList)control;
			this.control.MainTextEditor.MainTextEditor.OnTextChanged += Document_TextChanged;
		}

		public void RemoveItem(VariableListItemViewModel item)
		{
			items.Remove(item);
		}

		//読み込み周辺
		public void Load()
		{
			if(Main.SatoriConfViewModel.LoadState == EditorLoadState.Loaded)
			{
				Main.SatoriConfViewModel.VariableInitializeListFileSaveBody = GetSaveBody;
				Deserialize(Main.SatoriConfViewModel.VariableInitializeListBody);
			}
		}

		public void Deserialize(string body)
		{
			items.Clear();
			commonLines.Clear();

			var lines = DictionaryUtility.SplitLines(body);
			foreach(var item in lines)
			{
				//変数じゃない
				if (item.IndexOf(Constants.VariableHead) != 0)
				{
					commonLines.Add(item);
					continue;
				}	

				var sp = item.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
				items.Add(new VariableListItemViewModel(this, sp[0].Substring(1), sp.Length > 1 ? sp[1] : string.Empty));
			}
		}

		public string Serialize()
		{
			var lines = new List<string>();
			//lines.Add(Constants.VariableInitializeEvent);

			foreach(var item in items)
			{
				lines.Add(string.Format("{0}{1}\t{2}", Constants.VariableHead, item.Name, item.Data));
			}
			lines.AddRange(commonLines);

			return DictionaryUtility.JoinLines(lines);
		}

		public void TextToList()
		{
			Deserialize(control.MainTextEditor.MainTextEditor.Text);
		}
		public void ListToText()
		{
			using (serializingScope.NotifySerialize())
			{
				control.MainTextEditor.MainTextEditor.Text = Serialize();
			}
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			if(!serializingScope.IsSerializing)
			{
				Main.SatoriConfViewModel.Changed();
			}
		}

		//保存内容を出力
		private string GetSaveBody()
		{
			string saveText;
			if(currentTabIndex == TabIndexList)
			{
				//リストモードなのでシリアライズ
				saveText = Serialize();
			}
			else
			{
				//テキストなのでdocumetを保存
				saveText = control.MainTextEditor.MainTextEditor.Text;
			}
			return saveText;
		}

		public void Dispose()
		{
			control.MainTextEditor.MainTextEditor.OnTextChanged -= Document_TextChanged;
		}
	}

	internal class VariableListItemViewModel : NotificationObject
	{
		private VariableListViewModel parent;
		private string name;
		private string data;
		private bool isSelected;
		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand RemoveSingleItemCommand { get; }

		public string Name
		{
			get => name;
			set
			{
				name = value;
				parent.Main.SatoriConfViewModel.Changed();
				NotifyChanged();
			}
		}

		public string Data
		{
			get => data;
			set
			{
				data = value;
				parent.Main.SatoriConfViewModel.Changed();
				NotifyChanged();
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

		public VariableListItemViewModel(VariableListViewModel parent, string name, string data)
		{
			this.parent = parent;
			this.name = name;
			this.data = data;
			RemoveItemCommand = parent.RemoveItemCommand;
			RemoveSingleItemCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show("項目を削除します。よろしいですか？", "変数リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.parent.RemoveItem(this);
						parent.Main.SatoriConfViewModel.Changed();
					}
				}
				);

		}
	
	}

	//satori_conf.txt のデータ
	//変数リストとSAORIリストと共有するオブジェクト
	internal class SatoriConfWrapper : SaveFileObjectWrapper
	{
		private MainViewModel main;
		public const string SatoriConfFilePath = "/ghost/master/satori_conf.txt";

		//変数リストの保存内容を取得する
		public Func<string> VariableInitializeListFileSaveBody { get; set; }

		//SAORIリストの保存内容を取得する
		public Func<string> SaoriListFileSaveBody { get; set; }

		//読み込んだ情報の取得
		public string CommonFileBody { get; private set; }
		public string SaoriListBody { get; private set; }
		public string VariableInitializeListBody { get; private set; }

		public SatoriConfWrapper(MainViewModel main) : base(SatoriConfFilePath, null)
		{
			this.main = main;
			base.SetSaveFunc(SaveSatoriConf);
			this.CommonFileBody = string.Empty;
			this.SaoriListBody = string.Empty;
			this.VariableInitializeListBody = string.Empty;

			if (main.Ghost != null)
				Load();
		}

		private void Load()
		{
			var fullPath = main.Ghost.FullPath + SatoriConfFilePath;
			LoadState = EditorLoadState.Initialized;

			try
			{
				if(System.IO.File.Exists(fullPath))
				{
					var fileBody = System.IO.File.ReadAllText(fullPath, main.Ghost.BootConf.DicEncoding);

					//ここで初期化とSAORIのリストを切り出す
					var lines = DictionaryUtility.SplitLines(fileBody);
					bool isVariableList = false;
					bool isSaoriList = false;
					var saoriListLines = new List<string>();
					var variableInitializeListLines = new List<string>();
					var commonLines = new List<string>();
					
					foreach(var line in lines)
					{
						if(line == Constants.VariableInitializeEvent)
						{
							isVariableList = true;
							isSaoriList = false;
							continue;	//ヘッダは含めない
						}
						else if(line == Constants.SaoriListEvent)
						{
							isVariableList = false;
							isSaoriList = true;
							continue;	//ヘッダは含めない
						}
						else if(line.IndexOf(Constants.SentenceHead) == 0 || line.IndexOf(Constants.WordHead) == 0)
						{
							isVariableList = false;
							isSaoriList = false;
							commonLines.Add(line);
							continue;
						}

						if(isVariableList)
						{
							variableInitializeListLines.Add(line);
						}
						else if(isSaoriList)
						{
							saoriListLines.Add(line);
						}
						else
						{
							commonLines.Add(line);
						}
					}

					//処理終了後、まとめる
					CommonFileBody = DictionaryUtility.JoinLines(commonLines);
					SaoriListBody = DictionaryUtility.JoinLines(saoriListLines);
					VariableInitializeListBody = DictionaryUtility.JoinLines(variableInitializeListLines);
				}
				LoadState = EditorLoadState.Loaded;
			}
			catch
			{
				LoadState = EditorLoadState.LoadFailed;
			}
		}

		private bool SaveSatoriConf()
		{
			if (LoadState != EditorLoadState.Loaded)
				return false;

			var saveText = CommonFileBody;
			var variableInitializeListBody = VariableInitializeListFileSaveBody();
			var saoriListBody = SaoriListFileSaveBody();

			if(!string.IsNullOrEmpty(variableInitializeListBody))
			{
				saveText = string.Concat(saveText, Constants.NewLine, Constants.VariableInitializeEvent, Constants.NewLine, variableInitializeListBody);
			}

			if(!string.IsNullOrEmpty(saoriListBody))
			{
				saveText = string.Concat(saveText, Constants.NewLine, Constants.SaoriListEvent, Constants.NewLine, saoriListBody);
			}

			try
			{
				var fullPath = main.Ghost.FullPath + SatoriConfFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, main.Ghost.BootConf.DicEncoding);
				IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
