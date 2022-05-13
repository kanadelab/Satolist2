using ICSharpCode.AvalonEdit.Document;
using Microsoft.Win32;
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
	/// SaoriList.xaml の相互作用ロジック
	/// </summary>
	public partial class SaoriList : UserControl
	{
		public SaoriList()
		{
			InitializeComponent();
			
		}
	}

	internal class SaoriListViewModel : NotificationObject, IDockingWindowContent, IDisposable
	{
		public const string ContentId = "SaoriList";
		private const int TabIndexList = 0;
		private TextDocument document;
		private int currentTabIndex;
		private List<string> commonLines;

		private ObservableCollection<SaoriListModuleViewModel> items;

		public ReadOnlyObservableCollection<SaoriListModuleViewModel> Items => new ReadOnlyObservableCollection<SaoriListModuleViewModel>(items);

		public MainViewModel Main { get; }
		public string DockingTitle => "SAORIリスト";

		public string DockingContentId => ContentId;

		public TextDocument Document
		{
			get => document;
			set
			{
				document = value;
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
					if (value == TabIndexList)
						TextToList();
					else
						ListToText();
					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public ActionCommand AddModuleCommand { get; }

		public SaoriListViewModel(MainViewModel main)
		{
			Main = main;
			items = new ObservableCollection<SaoriListModuleViewModel>();
			commonLines = new List<string>();

			/*
			items.Add(new SaoriListModuleViewModel(this) { ModulePath = "saori/ssu.dll" });
			items.Add(new SaoriListModuleViewModel(this) { ModulePath = "saori/ssu.dll" });
			*/

			AddModuleCommand = new ActionCommand(
				o =>
				{
					//モジュール選択を表示
					var path = SaoriListModuleViewModel.SelectModule(Main.Ghost.FullDictionaryPath);
					if(!string.IsNullOrEmpty(path))
					{
						items.Add(new SaoriListModuleViewModel(this) { ModulePath = path });
					}

				});

			if (main.Ghost != null)
				Load();
		}

		public void RemoveModule(SaoriListModuleViewModel module)
		{
			items.Remove(module);
		}

		public void Load()
		{
			if (Main.SatoriConfViewModel.LoadState == EditorLoadState.Loaded)
			{
				Main.SatoriConfViewModel.SaoriListFileSaveBody = GetSaveBody;
				Deserialize(Main.SatoriConfViewModel.SaoriListBody);
			}
		}

		private void Deserialize(string body)
		{
			items.Clear();
			commonLines.Clear();

			var lines = DictionaryUtility.SplitLines(body);
			foreach(var item in lines)
			{
				var sp = item.Split(Constants.CommaSeparator, 3, StringSplitOptions.None);
				if(sp.Length == 3)
				{
					//SAORI有効レコード
					var call = sp[0];
					var module = sp[1];
					var command = sp[2];

					var moduleViewModel = items.FirstOrDefault(o => o.ModulePath == module);
					if(moduleViewModel == null)
					{
						moduleViewModel = new SaoriListModuleViewModel(this);
						moduleViewModel.ModulePath = module;
						items.Add(moduleViewModel);
					}
					moduleViewModel.AddFunction(call, command);
				}
				else
				{
					commonLines.Add(item);
				}
			}
		}

		private string Serialize()
		{
			var lines = new List<string>();
			
			foreach(var module in Items)
			{
				foreach(var func in module.Items)
				{
					lines.Add(string.Format("{0},{1},{2}", func.Name, module.ModulePath, func.Command));
				}
			}

			lines.AddRange(commonLines);
			return DictionaryUtility.JoinLines(lines);
		}

		private void TextToList()
		{
			Deserialize(Document.Text);
		}

		private void ListToText()
		{
			if (Document != null)
				Document.TextChanged -= Document_TextChanged;

			Document = new TextDocument(Serialize());
			Document.TextChanged += Document_TextChanged;
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			Main.SatoriConfViewModel.Changed();
		}
		

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
				//テキストなのでdocumentを保存
				saveText = document.Text;
			}
			return saveText;
		}
		public void Dispose()
		{
			if (Document != null)
				Document.TextChanged -= Document_TextChanged;
		}
	}

	internal class SaoriListModuleViewModel : NotificationObject
	{
		private ObservableCollection<SaoriListFunctionViewModel> items;
		private string modulePath;
		private SaoriListViewModel parent;

		public ReadOnlyObservableCollection<SaoriListFunctionViewModel> Items => new ReadOnlyObservableCollection<SaoriListFunctionViewModel>(items);
		public string Type => "SaoriModule";
		public ActionCommand SelectModuleCommand { get; }
		public ActionCommand RemoveModuleCommand { get; }
		public ActionCommand AddFunctionCommand { get; }

		public string ModulePath
		{
			get => modulePath;
			set
			{
				modulePath = value;
				NotifyChanged();
			}
		}

		public void AddFunction(string name, string command = "")
		{
			items.Add(new SaoriListFunctionViewModel(this) { Name = name, Command = command });
		}
			

		public SaoriListModuleViewModel(SaoriListViewModel parent)
		{
			items = new ObservableCollection<SaoriListFunctionViewModel>();

			/*
			items.Add(new SaoriListFunctionViewModel(this)
			{
				Command = "calc",
				Name = "calc"
			});

			items.Add(new SaoriListFunctionViewModel(this)
			{
				Command = "calc",
				Name = "calc"
			});
			*/

			SelectModuleCommand = new ActionCommand(
				o =>
				{
					var path = SelectModule(parent.Main.Ghost.FullDictionaryPath);
					if(!string.IsNullOrEmpty(path))
					{
						ModulePath = path;
					}
				}
				);

			RemoveModuleCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("SAORIモジュール設定を削除します。よろしいですか？", "SAORIリスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						parent.RemoveModule(this);
				}
				);

			AddFunctionCommand = new ActionCommand(
				o =>
				{
					//関数の追加
					AddFunction(string.Empty, string.Empty);
				}
				);
		}

		public void RemoveFunction(SaoriListFunctionViewModel func)
		{
			items.Remove(func);
		}

		public static string SelectModule(string fullDictionaryPath)
		{
			var fileOpenDialog = new OpenFileDialog();
			fileOpenDialog.InitialDirectory = DictionaryUtility.NormalizeWindowsPath(fullDictionaryPath);
			fileOpenDialog.CheckFileExists = true;
			fileOpenDialog.Multiselect = false;
			fileOpenDialog.Filter = "SAORIモジュール (*.dll,*.exe)|*.dll;*.exe|すべてのファイル (*.*)|*.*";
			if (fileOpenDialog.ShowDialog() == true)
			{
				if (!DictionaryUtility.IsChildPath(fullDictionaryPath, fileOpenDialog.FileName))
				{
					MessageBox.Show(string.Format("{0}\r\n{1}", fileOpenDialog.FileName, "ゴーストの配下のファイルではないため追加できません。"), "更新除外リスト", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return null;
				}
				var relativePath = DictionaryUtility.MakeRelativePath(fullDictionaryPath, fileOpenDialog.FileName);
				return relativePath;
			}
			return null;
		}

	}

	internal class SaoriListFunctionViewModel : NotificationObject
	{
		private string name;
		private string command;
		private SaoriListModuleViewModel parent;

		public object Items => Array.Empty<object>();
		public string Type => "SaoriFunction";
		public ActionCommand RemoveFunctionCommand { get; }

		public string Name
		{
			get => name;
			set
			{
				name = value;
				NotifyChanged();
			}
		}
		public string Command
		{
			get => command;
			set
			{
				command = value;
				NotifyChanged();
			}
		}

		public SaoriListFunctionViewModel(SaoriListModuleViewModel parent)
		{
			this.parent = parent;

			RemoveFunctionCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("SAORI呼び出しを削除します。よろしいですか？", "SAORIリスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						parent.RemoveFunction(this);
				}
				);
		}
	}
}
