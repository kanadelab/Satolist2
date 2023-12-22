using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
	/// SearchResult.xaml の相互作用ロジック
	/// </summary>
	public partial class SearchResult : UserControl
	{
		public SearchResult()
		{
			InitializeComponent();
			
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(DataContext is SearchResultViewModel vm)
			{
				vm.OpenSelectedItemEditor();
				e.Handled = true;
			}
		}
	}

	//検索条件オブジェクト
	internal class SearchQuery
	{
		//検索テキスト
		public string SearchString { get; set; }

		//検索オブジェクト
		public Regex SearchRegex { get; set; }
		
		//タイトルを検索対象にするか
		public bool IsSearchTitle { get; set; }

		//本文を検索対象にするか
		public bool IsSearchBody { get; set; }

		public bool IsStringPerfectMatch { get; set; }

		public SearchQuery()
		{
			SearchString = string.Empty;
			IsSearchTitle = true;
			IsSearchBody = true;
			IsStringPerfectMatch = false;
		}
	}

	internal class SearchResultViewModel : NotificationObject, IDockingWindowContent, IDisposable
	{
		public const string ContentId = "SearchResult";
		private ObservableCollection<SearchResultItemViewModel> items;
		private MainViewModel Main { get; }
		private string dockingTitle;

		public string DockingTitle
		{
			get => dockingTitle;
			set
			{
				dockingTitle = value;
				NotifyChanged();
			}
		}

		public string DockingContentId => ContentId;

		public ReadOnlyObservableCollection<SearchResultItemViewModel> Items
		{
			get => new ReadOnlyObservableCollection<SearchResultItemViewModel>(items);
		}

		//
		public SearchResultViewModel(MainViewModel main)
		{
			Main = main;
			items = new ObservableCollection<SearchResultItemViewModel>();
			DockingTitle = "検索結果";
		}

		//検索の実行?
		public void RunSearch(SearchQuery query)
		{
			//クリア処理
			foreach (var item in items)
				item.Dispose();
			items.Clear();

			//検索対象を作成
			object queryObj = (object)query.SearchRegex ?? (object)query.SearchString ?? string.Empty;

			//検索操作
			foreach(var dic in Main.Ghost.Dictionaries)
			{
				if (!dic.IsSerialized)
				{
					//どこかにヒットするか
					foreach (var ev in dic.Events)
					{
						bool isHit = false;
						if(query.IsSearchTitle)
						{
							if(QuerySearchHit(ev.Name, queryObj, query.IsStringPerfectMatch))
							{
								isHit = true;
							}
						}

						if(!isHit && query.IsSearchBody)
						{
							if(QuerySearchHit(ev.Body, queryObj, query.IsStringPerfectMatch) ||
								QuerySearchHit(ev.Condition, queryObj, query.IsStringPerfectMatch))
							{
								isHit = true;
							}
						}

						if (isHit)
						{
							//hit
							items.Add(new SearchResultItemViewModel(this, ev));
						}
					}
				}
				else
				{
					var lines = DictionaryUtility.SplitLines(dic.Body);
					for(int i = 0; i < lines.Length; i++)
					{
						var line = lines[i];
						if (string.IsNullOrEmpty(line))
							continue;

						bool isHit = false;

						//行頭が＊＠ではじまるなら項目名として検索する
						if (line[0] == Constants.WordHead || line[0] == Constants.SentenceHead)
						{
							//項目名と条件部を分離
							var sp = line.Split(Constants.TabSeparator, 2, StringSplitOptions.None);

							//タイトルの一致
							if (query.IsSearchTitle)
							{
								if (QuerySearchHit(sp[0].Substring(1), queryObj, query.IsStringPerfectMatch))
								{
									isHit = true;
								}
							}

							//それ以外
							if(!isHit && sp.Length>=2 && query.IsSearchBody)
							{
								if (QuerySearchHit(sp[1], queryObj, query.IsStringPerfectMatch))
								{
									isHit = true;
								}
							}
						}
						else if(query.IsSearchBody)
						{
							//本文
							if (QuerySearchHit(line, queryObj, query.IsStringPerfectMatch))
							{
								//hit
								isHit = true;
							}
						}

						//ヒットしていたら結果を作成
						if(isHit)
						{
							items.Add(new SearchResultItemViewModel(this, i, line, dic));
						}
					}
				}
			}

			//ウインドウの表示・アクティブ化
			Main.MainWindow.SearchResult.IsActive = true;
			Main.MainWindow.SearchResult.IsVisible = true;
			DockingTitle = string.Format("検索結果 ({0})", items.Count);
		}

		//検索処理
		private	bool QuerySearchHit(string target, object query, bool isStringPerfectMatch)
		{
			if(query is Regex regex)
			{
				return regex.IsMatch(target);
			}
			else if(query is string searchString)
			{
				if (isStringPerfectMatch)
				{
					return (target == (string)query);
				}
				else
				{
					return target.Contains(searchString);
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		//選択中のアイテムを開く
		public void OpenSelectedItemEditor()
		{
			foreach(var item in Items.Where(o => o.IsSelected))
			{
				if (item.Event != null)
				{
					//検索結果に一致する内容でオープン
					Main.OpenEventEditor(item.Event);
				}
				else if(item.TextFile != null)
				{
					Main.OpenTextEditor(item.TextFile, item.TextFileLineNumber);
				}
			}
		}

		//選択中のアイテムを削除
		public void RemoveSelectedItem()
		{
			//リスト化されてるものだけ
			foreach(var item in items.Where(o => o.IsListedEvent && o.IsSelected).ToArray())
			{
				item.Event.Dictionary.RemoveEvent(item.Event);
			}
		}

		//外部から項目の削除通知を受けた際の内容削除
		internal void OnRemoveItem(SearchResultItemViewModel item)
		{
			item.Dispose();
			items.Remove(item);
			DockingTitle = string.Format("検索結果 ({0})", items.Count);
		}

		//念のため程度?
		public void Dispose()
		{
			foreach (var item in items)
				item.Dispose();
			items.Clear();
		}
	}

	public class SearchResultItemViewModel : NotificationObject, IDisposable
	{
		private EventModel ev;
		private DictionaryModel textFile;
		private int textFileLineNumber;
		private string textFileLinePreview;
		private bool isSelected;
		private SearchResultViewModel Parent { get; }

		public bool IsSelected
		{
			get => isSelected;
			set
			{
				isSelected = value;
				NotifyChanged();
			}
		}

		public EventModel Event
		{
			get => ev;
			set
			{
				ev = value;
				NotifyChanged();
				NotifySearchResultChanged();
			}
		}

		public DictionaryModel TextFile
		{
			get => textFile;
			set
			{
				textFile = value;
				NotifyChanged();
				NotifySearchResultChanged();
			}
		}

		public int TextFileLineNumber
		{
			get => textFileLineNumber;
			set
			{
				textFileLineNumber = value;
				NotifyChanged();
				NotifySearchResultChanged();
			}
		}

		public string TextFileLinePreview
		{
			get => textFileLinePreview;
			set
			{
				textFileLinePreview = value;
				NotifyChanged();
				NotifySearchResultChanged();
			}
		}

		public bool IsListedEvent
		{
			get => Event != null;
		}

		public ActionCommand OpenCommand { get; }
		public ActionCommand RemoveItemCommand { get; }

		//検索結果の表示について、中身に応じての切り替え

		//項目名ラベル
		public string HitName
		{
			get
			{
				if (Event != null)
					return Event.Name;
				else if (TextFile != null)
					return string.Format("{0} 行目", TextFileLineNumber);
				return string.Empty;	//error?
			}
		}

		//内容ラベル
		public string HitBody
		{
			get
			{
				if (ev != null)
					return ev.BodyPreview;
				else if (TextFile != null)
					return TextFileLinePreview;
				return string.Empty;	//error?
			}
		}

		//ファイル名ラベル
		public string HitFileName
		{
			get
			{
				if (ev != null && ev.Dictionary != null)
					return ev.Dictionary.RelativeName;
				else if (textFile != null)
					return textFile.RelativeName;
				return string.Empty;	//error?
			}
		}

		private SearchResultItemViewModel()
		{
			OpenCommand = new ActionCommand(
				o =>
				{
					Parent.OpenSelectedItemEditor();
				}
				);

			RemoveItemCommand = new ActionCommand(
				o =>
				{
					var message = "選択した項目を削除します。よろしいですか？";
					var result = MessageBox.Show(message, "項目の削除", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (result != MessageBoxResult.Yes)
						return;

					Parent.RemoveSelectedItem();
				},
				o => IsListedEvent	//リスト化されてないと削除できない
				);
		}

		//単語群・文に関係する設定
		internal SearchResultItemViewModel(SearchResultViewModel parent, EventModel ev):this()
		{
			Parent = parent;
			Event = ev;
			Event.PropertyChanged += Event_PropertyChanged;
			Event.OnRemove += Event_OnRemove;
		}

		internal SearchResultItemViewModel(SearchResultViewModel parent, int lineNumber, string linePreviewString, DictionaryModel textFile)
		{
			Parent = parent;
			TextFile = textFile;
			TextFileLineNumber = lineNumber;
			TextFileLinePreview = linePreviewString;
			TextFile.OnDelete += Text_OnDelete;
		}

		private void Text_OnDelete(DictionaryModel obj)
		{
			//削除
			Parent.OnRemoveItem(this);
		}

		private void Event_OnRemove(EventModel obj)
		{
			//Model側の削除通知
			Parent.OnRemoveItem(this);
		}

		private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//ViewModel側に変更があった場合、自分側を更新するためのもの
			if(sender is EventModel)
			{
				switch(e.PropertyName)
				{
					case nameof(EventModel.Name):
					case nameof(EventModel.BodyPreview):
					case nameof(EventModel.Dictionary):
						NotifySearchResultChanged();
						break;
				}
			}
		}

		//プレーンテキストに関する設定?

		//検索結果結果の更新
		private void NotifySearchResultChanged()
		{
			NotifyChanged(nameof(HitName));
			NotifyChanged(nameof(HitBody));
			NotifyChanged(nameof(HitFileName));
		}

		public void Dispose()
		{
			if(Event != null)
			{
				Event.PropertyChanged -= Event_PropertyChanged;
				Event.OnRemove -= Event_OnRemove;
			}

			if(TextFile != null)
			{
				TextFile.OnDelete -= Text_OnDelete;
			}
		}
	}
}
