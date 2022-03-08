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

	internal class ReplaceListViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		private const int TAB_INDEX_LIST = 0;

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
					if (value == TAB_INDEX_LIST)
						TextToList();
					else
						ListToText();

					currentTabIndex = value;
					NotifyChanged();
				}
			}
		}

		public ActionCommand AddItemCommand { get; }
		public ActionCommand RemoveItemCommand { get; }

		public string DockingTitle => "文字置換リスト";

		public string DockingContentId => "ReplaceList";

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
					items.Add(new ReplaceListItemViewModel(this));
					items.Last().IsSelected = true;
					RequestScroll(items.Last());
				}
				);

			if (main.Ghost != null)
				Load();
		}

		public void RemoveItem(ReplaceListItemViewModel item)
		{
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
			var replacePath = main.Ghost.FullPath + "/ghost/master/replace.txt";
			var replaceAfterPath = main.Ghost.FullPath + "/ghost/master/replace_after.txt";

			if(System.IO.File.Exists(replacePath))
			{
				var fileBody = System.IO.File.ReadAllText(replacePath, Constants.EncodingShiftJis);
				DeserializeReplace(fileBody);
			}

			if(System.IO.File.Exists(replaceAfterPath))
			{
				var fileBody = System.IO.File.ReadAllText(replaceAfterPath, Constants.EncodingShiftJis);
				DeserializeReplaceAfter(fileBody);
			}
		}

		private void DeserializeReplace(string body)
		{
			replaceCommonLine.Clear();
			//items.Clear();
			var lines = DictionaryUtility.SplitLines(body);
			
			foreach(var item in lines)
			{
				var sp = item.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
				if(sp.Length == 2)
				{
					items.Add(new ReplaceListItemViewModel(this)
					{
						Before = sp[0],
						After = sp[1]
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
						After = sp[1]
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
					lines.Add(string.Join("{0}\t{1}", item.Before, item.After));
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
					lines.Add(string.Join("{0}\t{1}", item.Before, item.After));
			}

			lines.AddRange(replaceAfterCommonLine);
			return string.Join(Constants.NewLine, lines);
		}

		private void TextToList()
		{
			ReplaceDocument = new TextDocument(SerializeReplace());
			ReplaceAfterDocument = new TextDocument(SerializeReplaceAfter());
		}

		private void ListToText()
		{
			items.Clear();
			DeserializeReplace(ReplaceDocument.Text);
			DeserializeReplaceAfter(ReplaceAfterDocument.Text);
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (ReplaceList)control;
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
				before = value;
				NotifyChanged();
			}
		}

		public string After
		{
			get => after;
			set
			{
				after = value;
				NotifyChanged();
			}
		}

		public bool IsReplaceAfter
		{
			get => isReplaceAfter;
			set
			{
				isReplaceAfter = value;
				NotifyChanged();
				NotifyChanged(nameof(IsReplace));
			}
		}

		public bool IsReplace
		{
			get => !isReplaceAfter;
			set
			{
				isReplaceAfter = !value;
				NotifyChanged();
				NotifyChanged(nameof(IsReplaceAfter));
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
	}
}
