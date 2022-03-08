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

	internal class VariableListViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		private const int TAB_INDEX_LIST = 0;

		private MainViewModel main;
		private VariableList control;
		private ObservableCollection<VariableListItemViewModel> items;
		private TextDocument document;
		private int currentTabIndex;
		private List<string> commonLines;

		public ReadOnlyObservableCollection<VariableListItemViewModel> Items => new ReadOnlyObservableCollection<VariableListItemViewModel>(items);

		public ActionCommand RemoveItemCommand { get; }
		public ActionCommand AddItemCommand { get; }

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
				if (currentTabIndex != value)
				{
					if (value == TAB_INDEX_LIST)
						TextToList();
					else
						ListToText();

					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public string DockingTitle => "変数設定";

		public string DockingContentId => "VariableList";

		public VariableListViewModel(MainViewModel main)
		{
			this.main = main;
			items = new ObservableCollection<VariableListItemViewModel>();
			commonLines = new List<string>();
			document = new TextDocument();

			RemoveItemCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show("選択中の項目を削除します。よろしいですか？", "変数リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//選択中のアイテムを削除
						var removes = items.Where(i => i.IsSelected).ToArray();
						foreach (var item in removes)
							items.Remove(item);
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
					items.Add(new VariableListItemViewModel(this));
					items.Last().IsSelected = true;
					RequestScroll(items.Last());
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
		}

		public void RemoveItem(VariableListItemViewModel item)
		{
			items.Remove(item);
		}

		//読み込み周辺
		//TODO: SAORIリストと共有できる必要がある

		public void Load()
		{
			var satoriConfPath = main.Ghost.FullPath + "/ghost/master/satori_conf.txt";

			if(System.IO.File.Exists(satoriConfPath))
			{
				var fileBody = System.IO.File.ReadAllText(satoriConfPath, Constants.EncodingShiftJis);
				Deserialize(fileBody);
			}
		}

		public void Deserialize(string body)
		{
			items.Clear();

			var lines = DictionaryUtility.SplitLines(body);
			bool begin = false;
			foreach(var item in lines)
			{
				if(item == Constants.VariableInitializeEvent)
				{
					begin = true;
					continue;
				}

				//変数じゃない
				if (item.IndexOf(Constants.VariableHead) != 0)
					continue;

				if(begin)
				{
					var sp = item.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
					items.Add(new VariableListItemViewModel(this)
					{
						Name = sp[0].Substring(1),
						Data = sp.Length > 1 ? sp[1] : string.Empty
					});
				}
			}
		}

		public string Serialize()
		{
			var lines = new List<string>();
			lines.Add(Constants.VariableInitializeEvent);

			foreach(var item in items)
			{
				lines.Add(string.Format("{0}{1}\t{2}", Constants.VariableHead, item.Name, item.Data));
			}

			return string.Join(Constants.NewLine, lines);
		}

		public void TextToList()
		{
			Deserialize(Document.Text);
		}
		public void ListToText()
		{
			Document = new TextDocument(Serialize());
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
				NotifyChanged();
			}
		}

		public string Data
		{
			get => data;
			set
			{
				data = value;
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

		public VariableListItemViewModel(VariableListViewModel parent)
		{
			this.parent = parent;
			RemoveItemCommand = parent.RemoveItemCommand;
			RemoveSingleItemCommand = new ActionCommand(
				o =>
				{
					if(MessageBox.Show("項目を削除します。よろしいですか？", "変数リスト", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						this.parent.RemoveItem(this);
					}
				}
				);

		}
	
	}
}
