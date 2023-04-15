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

	internal class SaoriListViewModel : NotificationObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		public const string ContentId = "SaoriList";
		private SaoriList control;
		private const int TabIndexList = 0;
		private int currentTabIndex;
		private List<string> commonLines;

		private ListEditorSerializingScope serializingScope;
		private ObservableCollection<SaoriListModuleViewModel> items;

		public ReadOnlyObservableCollection<SaoriListModuleViewModel> Items => new ReadOnlyObservableCollection<SaoriListModuleViewModel>(items);

		public MainViewModel Main { get; }
		public string DockingTitle => "SAORIリスト";

		public string DockingContentId => ContentId;

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
			serializingScope = new ListEditorSerializingScope();
			items = new ObservableCollection<SaoriListModuleViewModel>();
			commonLines = new List<string>();

			AddModuleCommand = new ActionCommand(
				o =>
				{
					//モジュール選択を表示
					var path = SaoriListModuleViewModel.SelectModule(Main.Ghost.FullDictionaryPath);
					if(!string.IsNullOrEmpty(path))
					{
						items.Add(new SaoriListModuleViewModel(this, path));
						Main.SatoriConfViewModel.Changed();
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
				if(sp.Length >= 2)
				{
					//SAORI有効レコード
					var call = sp[0];
					var module = sp[1];
					var command = sp.Length == 3 ? sp[2] : string.Empty;

					var moduleViewModel = items.FirstOrDefault(o => o.ModulePath == module);
					if(moduleViewModel == null)
					{
						moduleViewModel = new SaoriListModuleViewModel(this, module);
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
			Deserialize(control.MainTextEditor.MainTextEditor.Text);
		}

		private void ListToText()
		{
			using (serializingScope.NotifySerialize())
			{
				control.MainTextEditor.MainTextEditor.Text = Serialize();
			}
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			if (!serializingScope.IsSerializing)
			{
				Main.SatoriConfViewModel.Changed();
			}
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
				saveText = control.MainTextEditor.MainTextEditor.Text;
			}
			return saveText;
		}
		public void Dispose()
		{
			control.MainTextEditor.MainTextEditor.OnTextChanged -= Document_TextChanged;
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is SaoriList ctrl)
			{
				this.control = ctrl;
				this.control.MainTextEditor.MainTextEditor.OnTextChanged -= Document_TextChanged;
			}
		}
	}

	internal class SaoriListModuleViewModel : NotificationObject
	{
		private ObservableCollection<SaoriListFunctionViewModel> items;
		private string modulePath;
		private SaoriListViewModel parent;

		public SaoriListViewModel Parent => parent;
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
				parent.Main.SatoriConfViewModel.Changed();
			}
		}

		public void AddFunction(string name, string command = "")
		{
			items.Add(new SaoriListFunctionViewModel(this, name, command));
		}
			

		public SaoriListModuleViewModel(SaoriListViewModel parent, string path)
		{
			this.parent = parent;
			items = new ObservableCollection<SaoriListFunctionViewModel>();
			modulePath = path;

			SelectModuleCommand = new ActionCommand(
				o =>
				{
					var module = SelectModule(parent.Main.Ghost.FullDictionaryPath);
					if(!string.IsNullOrEmpty(module))
					{
						ModulePath = module;
					}
				}
				);

			RemoveModuleCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("SAORIモジュール設定を削除します。よろしいですか？", "SAORIリスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						parent.RemoveModule(this);
						parent.Main.SatoriConfViewModel.Changed();
					}
				}
				);

			AddFunctionCommand = new ActionCommand(
				o =>
				{
					//関数の追加
					AddFunction(string.Empty, string.Empty);
					parent.Main.SatoriConfViewModel.Changed();
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
				parent.Parent.Main.SatoriConfViewModel.Changed();
				NotifyChanged();
			}
		}
		public string Command
		{
			get => command;
			set
			{
				command = value;
				parent.Parent.Main.SatoriConfViewModel.Changed();
				NotifyChanged();
			}
		}

		public SaoriListFunctionViewModel(SaoriListModuleViewModel parent, string name, string command)
		{
			this.parent = parent;
			this.name = name;
			this.command = command;

			RemoveFunctionCommand = new ActionCommand(
				o =>
				{
					if (MessageBox.Show("SAORI呼び出しを削除します。よろしいですか？", "SAORIリスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						parent.RemoveFunction(this);
						parent.Parent.Main.SatoriConfViewModel.Changed();
					}
				}
				);
		}
	}
}
