using Satolist2.Module.TextEditor;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	/// GhostDescriptEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class GhostDescriptEditor : UserControl
	{
		public GhostDescriptEditor()
		{
			InitializeComponent();
			
		}

		//入力欄のフォーカスを取得した場合、そのListItemをSelectedにする
		private void StringInput_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement elem)
			{
				if (elem.DataContext is DescriptItemViewModel itemViewModel)
				{
					itemViewModel.IsSelected = true;
				}
			}
		}

		//ListViewItemがFocusを得た場合、StringInputをフォーカスする。
		//Selected担った場合も同様
		private void ListViewItem_GotFocus(object sender, RoutedEventArgs e)
		{    
			if (sender is FrameworkElement elem)
			{
				var item = FindChildItem("StringInput", elem);
				if(item is TextBox textBox)
				{
					textBox.Focus();
				}
			}
		}

		private FrameworkElement FindChildItem(string Name, DependencyObject obj)
		{
			var count = VisualTreeHelper.GetChildrenCount(obj);
			for(int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);
				if (child is FrameworkElement elem && elem.Name == Name)
					return elem;

				var childResult = FindChildItem(Name, child);
				if (childResult is FrameworkElement childElem)
					return childElem;
			}
			return null;
		}

		//キーの押下による移動をサポート
		private void StringInput_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//タブを押して移動をサポートする
			if(e.Key == Key.Tab)
			{
				SettingsList.SelectedIndex = (SettingsList.SelectedIndex + 1) % SettingsList.Items.Count;
				e.Handled = true;
			}
		}
	}

	internal class GhostDescriptEditorViewModel : NotificationObject, ISaveFileObject, IDockingWindowContent, IDisposable, IControlBindedReceiver
	{
		public const string ContentId = "GhostProperty";
		public virtual string SaveFilePath => "/ghost/master/descript.txt";
		public virtual string DockingTitle => "ゴーストプロパティ";
		public virtual string DockingContentId => ContentId;
		public virtual DescriptItemModel[] ItemModel => DataModelManager.DescriptItems;
		public string FileName => System.IO.Path.GetFileName(SaveFilePath);


		private const int TabIndexList = 0;

		private ListEditorSerializingScope serializingScope;
		private MainViewModel main;
		private GhostDescriptEditor control;
		private int currentTabIndex;
		private int selectedIndex;
		private EditorLoadState loadState;
		private List<string> commonLine;
		private bool isChanged;
		private IEnumerable<DescriptItemViewModel> items;
		private string searchString;
		public ICollectionView Items { get; }

		public TextEditorModuleBase MainTextEditor => control?.MainTextEditor?.MainTextEditor;

		public int SelectedIndex
		{
			get => selectedIndex;
			set
			{
				selectedIndex = value;
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
					if (value == TabIndexList)
						TextToList();
					else
						ListToText();

					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public EditorLoadState LoadState
		{
			get => loadState;
			set
			{
				loadState = value;
				NotifyChanged();
			}
		}

		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = value;
				Items.Refresh();
			}
		}

		//変更されたか
		public bool IsChanged
		{
			get => isChanged;
		}

		

		public GhostDescriptEditorViewModel(MainViewModel main)
		{
			this.main = main;
			serializingScope = new ListEditorSerializingScope();
			loadState = EditorLoadState.Initialized;
			items = ItemModel.Select(o => new DescriptItemViewModel(o, this)).ToArray();
			Items = CollectionViewSource.GetDefaultView(items);
			Items.Filter = new Predicate<object>(
				o =>
				{
					return ((DescriptItemViewModel)o).Label.Contains(searchString ?? string.Empty);
				}
				);

			commonLine = new List<string>();

			if (main.Ghost != null)
				Load();
			isChanged = false;
		}

		//ファイルからの読み込み
		private bool Load()
		{
			loadState = EditorLoadState.Initialized;
			try
			{
				var fullPath = main.Ghost.FullPath + SaveFilePath;
				if (System.IO.File.Exists(fullPath))
				{
					var fileBody = System.IO.File.ReadAllText(fullPath, Constants.EncodingShiftJis);
					Deserialize(fileBody);
				}
				loadState = EditorLoadState.Loaded;
				return true;
			}
			catch
			{
				loadState = EditorLoadState.LoadFailed;
				return false;
			}
		}

		//ファイルへの書き込み
		public bool Save()
		{
			if (loadState != EditorLoadState.Loaded)
				return false;

			string saveText;
			if(CurrentTabIndex == TabIndexList)
			{
				//リスト化されてるのでシリアライズして保存
				saveText = Serialize();
			}
			else
			{
				//テキスト編集モードなのでDocumentを保存
				saveText = MainTextEditor.Text;
			}

			try
			{
				var fullPath = main.Ghost.FullPath + SaveFilePath;
				System.IO.File.WriteAllText(fullPath, saveText, Constants.EncodingShiftJis);
				isChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
		}

		//テキストのデシリアライズ
		private void Deserialize(string body)
		{
			commonLine.Clear();
			var lines = DictionaryUtility.SplitLines(body);

			foreach(var line in lines)
			{
				var sp = DictionaryUtility.SplitCSVLine(line);
				if(sp.Length == 2)
				{
					//Modelに一致するものを探す
					var item = items.FirstOrDefault(o => o.Label == sp[0]);
					if(item != null)
					{
						item.Value = sp[1]; //TODO: bool変換?
						continue;
					}
				}

				commonLine.Add(line);
			}
		}

		//テキストへのシリアライズ
		private string Serialize()
		{
			var lines = new List<string>();
			foreach(var item in items)
			{
				if(!string.IsNullOrEmpty(item.Value))
					lines.Add(string.Concat(item.Label, ',', item.Value));
			}
			lines.AddRange(commonLine);
			return DictionaryUtility.JoinLines(lines);
		}

		private void TextToList()
		{
			Deserialize(MainTextEditor.Text);
		}

		private void ListToText()
		{
			using (serializingScope.NotifySerialize())
			{
				MainTextEditor.Text = Serialize();
			}
		}

		private void Document_TextChanged(object sender, EventArgs e)
		{
			if (!serializingScope.IsSerializing)
			{
				Changed();
			}
		}

		public void Changed()
		{
			isChanged = true;
		}

		public void Dispose()
		{
			if (MainTextEditor != null)
				MainTextEditor.OnTextChanged -= Document_TextChanged;
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is GhostDescriptEditor editor)
			{
				this.control = editor;
				MainTextEditor.OnTextChanged += Document_TextChanged;
			}
		}
	}

	internal class GhostInstallEditorViewModel : GhostDescriptEditorViewModel
	{
		public new const string ContentId = "Install";
		public override string DockingContentId => ContentId;
		public override string DockingTitle => "インストール設定";
		public override string SaveFilePath => "/install.txt";
		public override DescriptItemModel[] ItemModel => DataModelManager.InstallItems;

		public GhostInstallEditorViewModel(MainViewModel main):base(main)
		{

		}
	}

	internal enum DescriptBool
	{
		Default,
		False,
		True
	}

	internal class DescriptItemViewModel :NotificationObject
	{
		private string val;
		private bool isSelected;
		private DescriptItemModel model;
		private DescriptSelectItem selectedItem;
		private ISaveFileObject parent;

		public string Label => model.Property;
		public string Description => model.Description;
		public string Default => model.Default;
		public string Help
		{
			get
			{
				if (string.IsNullOrEmpty(model.Help))
					return model.Description;
				else
					return string.Concat(model.Description, Constants.NewLine, model.Help);
			}
		}
		public DescriptItemModel.DescriptType Type => model.Type;
		public bool Required => model.Required;
		public bool Hidden => model.Hidden;
		public IEnumerable<DescriptSelectItem> Items
		{
			get
			{
				//リストにないものが選択されている場合はアイテムの１つとして列挙する形で許容する
				foreach (var item in model.Items)
					yield return item;

				if(!model.Items.Any(o => o == selectedItem))
					yield return selectedItem;
			}
		}

		public string Value
		{
			get
			{
				if (Type == DescriptItemModel.DescriptType.Constant)
				{
					//constantなら必ずdefaultが帰る
					return Default;
				}
				else
				{
					return val;
				}
			}
			set
			{
				if (val != value)
				{
					val = value;

					var item = model.Items.FirstOrDefault(o => o.Value == val);
					if (item != null)
						selectedItem = item;
					else
						selectedItem = new DescriptSelectItem() { Label = val, Value = val };

					NotifyChanged();
					NotifyChanged(nameof(IsDefault));
					NotifyChanged(nameof(SelectedItem));
					parent.Changed();
				}
			}
		}

		public DescriptSelectItem SelectedItem
		{
			get => selectedItem;
			set
			{
				if (selectedItem != value)
				{
					selectedItem = value;
					val = selectedItem.Value;
					NotifyChanged(nameof(IsDefault));
					NotifyChanged(nameof(Value));
					NotifyChanged();
					parent.Changed();
				}
			}
		}

		public bool IsDefault
		{
			get
			{
				switch(Type)
				{
					case DescriptItemModel.DescriptType.String:
						return string.IsNullOrEmpty(val);
					case DescriptItemModel.DescriptType.Select:
						return string.IsNullOrEmpty(selectedItem.Value);
						
				}
				return false;
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

		public DescriptItemViewModel(DescriptItemModel model, ISaveFileObject document)
		{
			this.parent = document;
			this.model = model;
			SelectedItem = model.Items[0];
		}
	}
}
