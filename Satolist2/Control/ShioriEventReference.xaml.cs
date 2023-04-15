using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentFTP.Servers.Handlers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
	/// ShioriEventReference.xaml の相互作用ロジック
	/// </summary>
	public partial class ShioriEventReference : UserControl
	{
		public ShioriEventReference()
		{
			InitializeComponent();
		}
	}

	public class ShioriEventReferenceViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		public const string ContentId = "UkadocEventReference";
		public string DockingTitle => "Ukadocイベントリファレンス";
		public string DockingContentId => ContentId;

		private bool isLoaded_;
		private UkadocCategoryViewModel[] items;
		private Dictionary<string, UkadocEventViewModel> events;

		private ShioriEventReference Control { get; set; }
		public ReadOnlyCollection<UkadocCategoryViewModel> Items => new ReadOnlyCollection<UkadocCategoryViewModel>(items);

		//ukadocデータを読み込めているか
		public bool IsLoaded
		{
			get => isLoaded_;
			set
			{
				isLoaded_ = value;
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
				IsLoaded = false;
			}
			NotifyChanged(nameof(Items));
		}
	}

	//Ukadocイベントリファレンス ダウンローダ
	public static class UkadocDownloader
	{
		private static readonly Regex HtmlTagPattern = new Regex(@"<(""[^""]*""|'[^']*'|[^'"">])*>");
		private static readonly Regex HtmlIndentPattern = new Regex(@"[\r\n]+[\t 　]+");
		private static readonly Regex HtmlNewLinePattern = new Regex(@"(<br[ 　]*/?>|</li>|</p>)");
		private static readonly Regex HtmlTextNewLinePattern = new Regex(@"[\r\n]+");

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

			return str;
		}

		public static Task<UkadocCacheData> DownloadAsync()
		{
			return Task.Run<UkadocCacheData>(() =>
			{
				try
				{
					List<UkadocCategoryModel> categories = new List<UkadocCategoryModel>();

					WebClient webClient = new WebClient();
					webClient.Encoding = System.Text.Encoding.UTF8;
					var str = webClient.DownloadString(@"http://ssp.shillest.net/ukadoc/manual/list_shiori_event.html");
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
					return new UkadocCacheData()
					{
						EventReferenceCategories = categories
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
		public int Version { get; set; }

		public UkadocCacheData()
		{
			EventReferenceCategories = new List<UkadocCategoryModel>();
			Version = CurrentUkadocCacheVersion;
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
	public class UkadocCategoryViewModel : NotificationObject
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

		public UkadocCategoryViewModel(UkadocCategoryModel model)
		{
			this.model = model;
			Events = model.Events.Select(o => new UkadocEventViewModel(o)).ToArray();
		}
	}

	//ukadocイベントリファレンスのイベントノード
	public class UkadocEventViewModel
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

		public UkadocEventViewModel(UkadocEventModel model)
		{
			this.model = model;
		}
	}
}
