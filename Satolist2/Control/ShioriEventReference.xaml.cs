using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentFTP.Servers.Handlers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
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
	/// ShioriEventReference.xaml の相互作用ロジック
	/// </summary>
	public partial class ShioriEventReference : UserControl
	{
		public ShioriEventReference()
		{
			InitializeComponent();
		}

		//スクリプトツリーアイテムのダブルクリック
		private void ScriptTreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(sender is FrameworkElement elem)
			{
				if(elem.DataContext is UkadocEventViewModel vm)
				{
					vm.InsertCommand?.Execute(null);
					e.Handled = true;
				}
			}
		}
	}

	public class ShioriEventReferenceViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		public const string ContentId = "UkadocEventReference";
		public string DockingTitle => "Ukadocイベントリファレンス";
		public string DockingContentId => ContentId;

		private bool isLoaded;
		private UkadocCategoryViewModel[] items;
		private UkadocSakuraScriptCategoryViewModel[] scriptItems;
		private Dictionary<string, UkadocEventViewModel> events;
		private string searchString;

		private ShioriEventReference Control { get; set; }
		public ReadOnlyCollection<UkadocCategoryViewModel> Items => new ReadOnlyCollection<UkadocCategoryViewModel>(items);
		public ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel> ScriptItems => new ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel>(scriptItems);

		//ukadocデータを読み込めているか
		public bool IsLoaded
		{
			get => isLoaded;
			set
			{
				isLoaded = value;
				NotifyChanged();
			}
		}

		//検索ボックス
		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = Constants.RemoveNewlineForSearchBox(value);
				NotifyChanged();
			}
		}
		
		public ShioriEventReferenceViewModel()
		{
			events = new Dictionary<string, UkadocEventViewModel>();
			RefleshView();
		}

		//イベントが存在していれば選択する
		public void TrySelectEvent(string eventName)
		{
			//検索ボックスで検索が指定されていたた、無視してみる
			if (!string.IsNullOrWhiteSpace(SearchString))
				return;

			//イベントを検索して、見つけたものに移動する
			UkadocEventViewModel ev;
			if(events.TryGetValue(eventName, out ev))
			{
				//所属するカテゴリを検索
				var categoryItem = items.First(o => o.Events.Contains(ev));
				categoryItem.IsExpanded = true;

				//開くのを待って選択状態に
				Control.Dispatcher.BeginInvoke(new Action(() =>
				{
					if (Control.EventTree.ItemContainerGenerator.ContainerFromItem(categoryItem) is TreeViewItem categoryContainer)
					{
						if (categoryContainer.ItemContainerGenerator.ContainerFromItem(ev) is TreeViewItem item)
						{
							item.IsSelected = true;
							item.BringIntoView();
						}
					}
				}), System.Windows.Threading.DispatcherPriority.Render);
			}
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is ShioriEventReference ctl)
			{
				Control = ctl;
			}
		}

		//ビューモデルを再構築し、ビューを最新にする
		public void RefleshView()
		{
			if(MainViewModel.EditorSettings?.TemporarySettings?.UkadocCache != null)
			{
				var categories = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.EventReferenceCategories.ToArray();
				items = categories.Select(o => new UkadocCategoryViewModel(o)).ToArray();
				scriptItems = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.SakuraScriptReferenceCategories.Select(o => new UkadocSakuraScriptCategoryViewModel(o)).ToArray();

				//検索用にまとめる
				events.Clear();
				foreach (var c in items)
				{
					foreach (var e in c.Events)
					{
						events[e.Name] = e;
					}
				}
				IsLoaded = true;
			}
			else
			{
				items = Array.Empty<UkadocCategoryViewModel>();
				scriptItems = Array.Empty<UkadocSakuraScriptCategoryViewModel>();
				IsLoaded = false;
			}
			NotifyChanged(nameof(Items));
			NotifyChanged(nameof(ScriptItems));
		}
	}

	//Ukadocイベントリファレンス ダウンローダ
	public static class UkadocDownloader
	{
		private static readonly Regex HtmlTagPattern = new Regex(@"<(""[^""]*""|'[^']*'|[^'"">])*>");
		private static readonly Regex HtmlIndentPattern = new Regex(@"[\r\n]+[\t 　]+");
		private static readonly Regex HtmlNewLinePattern = new Regex(@"(<br[ 　]*/?>|</li>|</p>|</dd>|</dt>)");
		private static readonly Regex HtmlTextNewLinePattern = new Regex(@"[\r\n]+");

		public const string ScriptReferenceUrl = @"http://ssp.shillest.net/ukadoc/manual/list_sakura_script.html";
		public const string EventReferenceUrl = @"http://ssp.shillest.net/ukadoc/manual/list_shiori_event.html";

		private static string EscapeHtml(string rawString)
		{
			var str = rawString;

			//インデントの除去
			str = HtmlIndentPattern.Replace(str, "");

			//テキスト改行の除去
			str = HtmlTextNewLinePattern.Replace(str, "");

			//html改行をテキスト改行に変換
			str = HtmlNewLinePattern.Replace(str, "\r\n");

			//htmlタグの除去
			str = HtmlTagPattern.Replace(str, "");

			return HttpUtility.HtmlDecode(str);
		}

		public static Task<UkadocCacheData> DownloadAsync()
		{
			return Task.Run<UkadocCacheData>(() =>
			{
				try
				{
					//イベントリストの取得
					List<UkadocCategoryModel> categories = new List<UkadocCategoryModel>();
					{
						WebClient webClient = new WebClient();
						webClient.Encoding = System.Text.Encoding.UTF8;
						var str = webClient.DownloadString(EventReferenceUrl);
						var parser = new HtmlParser();
						var document = parser.ParseDocument(str);
						var categoryItems = document.GetElementsByClassName("navigation-category");
						foreach (var item in categoryItems)
						{
							//1つはヘッダ、1つはリスト
							var ul = item.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlUnorderedListElement);
							var head = item.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlHeadingElement);

							if (head == null || ul == null)
								continue;
							var category = new UkadocCategoryModel();
							category.Name = head.InnerHtml;
							categories.Add(category);

							if (ul != null)
							{
								foreach (var listitem in ul.Children)
								{
									//リストアイテムをリストアップする
									if (listitem is AngleSharp.Html.Dom.IHtmlListItemElement itemElem)
									{
										var anchor = (AngleSharp.Html.Dom.IHtmlAnchorElement)itemElem.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlAnchorElement);
										if (anchor != null)
										{
											var eventId = anchor.Hash.Substring(1);
											var ev = document.GetElementById(eventId);

											if (ev == null)
												continue;

											var evHead = ev.Children[0];
											var evDetail = ev.Children[1];

											var eventNode = new UkadocEventModel();
											eventNode.Name = eventId;
											eventNode.DocumentId = eventId;
											category.Events.Add(eventNode);

											//evDetailのクラス設定無しの子が本文
											//リファレンス類はreference,ベースウェアはsupported-baseware
											foreach (var detailItem in evDetail.Children)
											{
												if (!string.IsNullOrEmpty(detailItem.ClassName))
													continue;
												eventNode.Details.Add(EscapeHtml(detailItem.InnerHtml));
											}

											var evReference = evDetail.GetElementsByClassName("reference").FirstOrDefault();
											var evReferenceItems = evReference?.Children;
											var supportedBaseware = evDetail.GetElementsByClassName("supported-baseware").FirstOrDefault();
											var supportedBasewareItems = supportedBaseware?.Children;

											if (evReferenceItems != null)
											{
												for (int i = 0; i + 1 < evReferenceItems.Length; i += 2)
												{
													eventNode.References.Add(new Tuple<string, string>(
														EscapeHtml(evReferenceItems[i].InnerHtml),
														EscapeHtml(evReferenceItems[i + 1].InnerHtml)
														));
												}
											}

											if (supportedBasewareItems != null)
											{
												foreach (var bw in supportedBasewareItems)
												{
													if (bw.Children.FirstOrDefault() is AngleSharp.Html.Dom.IHtmlImageElement img && !string.IsNullOrEmpty(img.AlternativeText))
														eventNode.SupportedBasewares.Add(EscapeHtml(img.AlternativeText));
												}
											}
										}
									}
								}
							}
						}
						/*
						return new UkadocCacheData()
						{
							EventReferenceCategories = categories
						};
						*/
					}

					//さくらスクリプトリストの取得
					List<UkadocSakuraScriptCategoryModel> sakuraScriptCategories = new List<UkadocSakuraScriptCategoryModel>();
					{
						WebClient webClient = new WebClient();
						webClient.Encoding = System.Text.Encoding.UTF8;
						var str = webClient.DownloadString(ScriptReferenceUrl);
						var parser = new HtmlParser();
						var document = parser.ParseDocument(str);
						var categoryItems = document.GetElementsByClassName("navigation-category");

						foreach(var item in categoryItems)
						{
							var ul = item.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlUnorderedListElement);
							var head = item.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlHeadingElement);

							if (head == null || ul == null)
								continue;
							var category = new UkadocSakuraScriptCategoryModel();
							category.Name = head.InnerHtml;
							sakuraScriptCategories.Add(category);

							foreach (var listItem in ul.Children)
							{
								//リストアイテムのリストアップ
								if (listItem is AngleSharp.Html.Dom.IHtmlListItemElement itemElem)
								{
									var anchor = (AngleSharp.Html.Dom.IHtmlAnchorElement)itemElem.Children.FirstOrDefault(o => o is AngleSharp.Html.Dom.IHtmlAnchorElement);
									if (anchor != null)
									{
										var eventId = anchor.Hash.Substring(1);
										var ev = document.GetElementById(eventId);
										var name = anchor.InnerHtml.ToString();

										//さくらスクリプト形式の "\" から始まらない場合は無視して読み飛ばす
										if (ev == null || !name.StartsWith(@"\"))
											continue;

										var evHead = ev.Children[0];
										var evDetail = ev.Children[1];
										var supportedBaseware = evDetail.GetElementsByClassName("supported-baseware").FirstOrDefault();
										var supportedBasewareItems = supportedBaseware?.Children;

										var scriptNode = new UkadocSakuraScriptModel();
										scriptNode.Name = name;
										scriptNode.DocumentId = eventId;
										category.Scripts.Add(scriptNode);

										//説明を追加
										foreach (var detailItem in evDetail.Children)
										{
											if (!string.IsNullOrEmpty(detailItem.ClassName))
												continue;
											scriptNode.Details.Add(EscapeHtml(detailItem.InnerHtml));
										}

										//ベースウェア指定を追加
										if (supportedBasewareItems != null)
										{
											foreach (var bw in supportedBasewareItems)
											{
												if (bw.Children.FirstOrDefault() is AngleSharp.Html.Dom.IHtmlImageElement img && !string.IsNullOrEmpty(img.AlternativeText))
													scriptNode.SupportedBasewares.Add(EscapeHtml(img.AlternativeText));
											}
										}
									}
								}
							}

							if(!category.Scripts.Any())
							{
								//からっぽなら削除
								sakuraScriptCategories.Remove(category);
							}
						}
					}

					return new UkadocCacheData()
					{
						EventReferenceCategories = categories,
						SakuraScriptReferenceCategories = sakuraScriptCategories
					};
				}
				catch
				{
					return null;
				}
			});
		}
	}

	//Ukadocイベントリファレンスの情報
	public class UkadocCacheData
	{
		private int CurrentUkadocCacheVersion = 1;

		//カテゴリ情報
		[JsonProperty]
		public List<UkadocCategoryModel> EventReferenceCategories { get; set; }
		[JsonProperty]
		public List<UkadocSakuraScriptCategoryModel> SakuraScriptReferenceCategories { get; set; }
		[JsonProperty]
		public int Version { get; set; }

		public UkadocCacheData()
		{
			EventReferenceCategories = new List<UkadocCategoryModel>();
			SakuraScriptReferenceCategories = new List<UkadocSakuraScriptCategoryModel>();
			Version = CurrentUkadocCacheVersion;
		}
	}

	//さくらスクリプトカテゴリモデル
	public class UkadocSakuraScriptCategoryModel
	{
		[JsonProperty]
		public string Name { get; set; }
		[JsonProperty]
		public List<UkadocSakuraScriptModel> Scripts { get; }

		public UkadocSakuraScriptCategoryModel()
		{
			Name = string.Empty;
			Scripts = new List<UkadocSakuraScriptModel>();
		}
	}

	//さくらスクリプトのモデル
	public class UkadocSakuraScriptModel
	{
		//名前
		[JsonProperty]
		public string Name { get; set; }
		//html上のID(ブラウザで開く際に使用)
		[JsonProperty]
		public string DocumentId { get; set; }
		//使えるベースウェア
		[JsonProperty]
		public List<string> SupportedBasewares { get; set; }
		//説明
		[JsonProperty]
		public List<string> Details { get; set; }

		public UkadocSakuraScriptModel()
		{
			Details = new List<string>();
			SupportedBasewares = new List<string>();
		}
	}

	//Ukadocのカテゴリモデル
	public class UkadocCategoryModel
	{
		//カテゴリ名
		[JsonProperty]
		public string Name { get; set; }
		//イベント
		[JsonProperty]
		public List<UkadocEventModel> Events { get; set; }

		public UkadocCategoryModel()
		{
			Name = string.Empty;
			Events = new List<UkadocEventModel>();
		}
	}

	//Ukadocのイベントモデル
	public class UkadocEventModel
	{
		//イベント名
		[JsonProperty]
		public string Name { get; set; }
		//html側ID
		[JsonProperty]
		public string DocumentId { get; set; }
		//サポートベースウェア
		[JsonProperty]
		public List<string> SupportedBasewares { get; set; }
		//Reference
		[JsonProperty]
		public List<Tuple<string, string>> References { get; set; }
		//説明
		[JsonProperty]
		public List<string> Details { get; set; }

		public UkadocEventModel()
		{
			Name = string.Empty;
			SupportedBasewares = new List<string>();
			References = new List<Tuple<string, string>>();
			Details = new List<string>();
		}
	}

	//Ukadocのカテゴリビューモデル
	public class UkadocCategoryViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		private UkadocCategoryModel model;
		private bool isExpanded;

		public string Name => model.Name;
		public UkadocEventViewModel[] Events { get; private set; }
		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				isExpanded = value;
				NotifyChanged();
			}
		}

		public string Tooltip => Name;

		public UkadocCategoryViewModel(UkadocCategoryModel model)
		{
			this.model = model;
			Events = model.Events.Select(o => new UkadocEventViewModel(o)).ToArray();
		}

		public bool Filter(string filterString, object[] args)
		{
			return Events.Any(o => o.Filter(filterString, args));
		}
	}

	//ukadocイベントリファレンスのイベントノード
	public class UkadocEventViewModel : NotificationObject, Utility.SearchFilterConverter.IFilter
	{
		private UkadocEventModel model;

		//子無しノードなので実装だけしておく
		public bool IsExpanded
		{
			get => false;
			set { }
		}

		//イベントノードを示す
		//これがないと説明が出ない
		public bool IsEventNode => true;

		//Referenceがあるか
		public bool HasReference => References.Any();

		public string Name => model.Name;
		public string Detail => string.Join("\r\n", model.Details);
		public ReadOnlyCollection<Tuple<string, string>> References => new ReadOnlyCollection<Tuple<string, string>>(model.References);
		public ReadOnlyCollection<string> SupportedBasewares => new ReadOnlyCollection<string>(model.SupportedBasewares);

		//イベント名称の挿入
		public ICommand InsertCommand { get; }
		public ICommand CopyToClipboardCommand { get; }
		public ICommand OpenInUkadocCommand { get; }

		public string Tooltip
		{
			get
			{
				var d = Detail.Split(Constants.NewLineSeparator, StringSplitOptions.RemoveEmptyEntries);
				if (d.FirstOrDefault() == null)
				{
					return Name;
				}
				else
				{
					return string.Format("{0}\r\n{1}", Name, d.FirstOrDefault());
				}
			}
		}

		public UkadocEventViewModel(UkadocEventModel model)
		{
			this.model = model;

			InsertCommand = new ActionCommand(
				o =>
				{
					//開いているやつに挿入
					MainWindow.Instance.InsertToActiveEditor(Name);
				});

			CopyToClipboardCommand = new ActionCommand(
				o =>
				{
					//クリップボードにコピー
					try
					{
						Clipboard.SetText(Name);
					}
					catch { }
				});

			OpenInUkadocCommand = new ActionCommand(
				o =>
				{
					try
					{
						if (string.IsNullOrEmpty(model.DocumentId))
							Process.Start(UkadocDownloader.EventReferenceUrl);
						else
							Process.Start(string.Format("{0}#{1}", UkadocDownloader.EventReferenceUrl, model.DocumentId));
					}
					catch { }
				});
		}

		public bool Filter(string filterString, object[] args)
		{
			return Name.ToLower().Contains(filterString.ToLower());
		}
	}
}
