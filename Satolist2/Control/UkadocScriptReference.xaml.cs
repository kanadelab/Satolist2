using AngleSharp.Dom.Events;
using Satolist2.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
	/// UkadocScriptReference.xaml の相互作用ロジック
	/// </summary>
	public partial class UkadocScriptReference : UserControl
	{
		public UkadocScriptReference()
		{
			InitializeComponent();
		}

		//スクリプトツリーアイテムのダブルクリック
		private void ScriptTreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is FrameworkElement elem)
			{
				if (elem.DataContext is UkadocSakuraScriptViewModel vm)
				{
					vm.InsertCommand?.Execute(null);
					e.Handled = true;
				}
			}
		}

		//勝手に横スクロールしないための
		private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
		{
			e.Handled = true;
		}
	}

	public class UkadocScriptReferenceViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "UkadocScriptReference";
		public string DockingTitle => "Ukadocスクリプトリファレンス";
		public string DockingContentId => ContentId;

		private bool isLoaded;
		private UkadocSakuraScriptCategoryViewModel[] scriptItems;
		private string searchString;

		public ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel> ScriptItems => new ReadOnlyCollection<UkadocSakuraScriptCategoryViewModel>(scriptItems);

		//ukadocデータが読み込めているか
		public bool IsLoaded
		{
			get => isLoaded;
			set
			{
				isLoaded = value;
				NotifyChanged();
			}
		}

		//検索文字列
		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = value;
				NotifyChanged();
			}
		}

		public UkadocScriptReferenceViewModel()
		{
			RefleshView();
		}

		public void RefleshView()
		{
			if (MainViewModel.EditorSettings?.TemporarySettings?.UkadocCache != null)
			{
				var categories = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.EventReferenceCategories.ToArray();
				scriptItems = MainViewModel.EditorSettings.TemporarySettings.UkadocCache.SakuraScriptReferenceCategories.Select(o => new UkadocSakuraScriptCategoryViewModel(o)).ToArray();
				IsLoaded = true;
			}
			else
			{
				scriptItems = Array.Empty<UkadocSakuraScriptCategoryViewModel>();
				IsLoaded = false;
			}
			NotifyChanged(nameof(ScriptItems));
		}
	}

	//Ukadocさくらスクリプトのカテゴリビューモデル
	public class UkadocSakuraScriptCategoryViewModel : NotificationObject, SearchFilterConverter.IFilter
	{
		private UkadocSakuraScriptCategoryModel model;
		private bool isExpanded;

		public string Name => model.Name;
		public UkadocSakuraScriptViewModel[] Scripts { get; private set; }

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

		public UkadocSakuraScriptCategoryViewModel(UkadocSakuraScriptCategoryModel model)
		{
			this.model = model;
			Scripts = model.Scripts.Select(o => new UkadocSakuraScriptViewModel(o)).ToArray();
		}

		public bool Filter(string filterString)
		{
			return Scripts.Any(o => o.Filter(filterString));
		}
	}

	//さくらスクリプトのビューモデル
	public class UkadocSakuraScriptViewModel : NotificationObject, SearchFilterConverter.IFilter
	{
		private UkadocSakuraScriptModel model;

		public bool IsExpanded
		{
			get => false;
			set { }
		}

		//ダミー子
		public object Scripts => Array.Empty<object>();

		public bool IsScriptNode => true;
		public string Name => model.Name;
		public string Detail => string.Join("\r\n", model.Details);
		public ReadOnlyCollection<string> SupportedBasewares => new ReadOnlyCollection<string>(model.SupportedBasewares);

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

		//さくらスクリプトの挿入
		public ICommand InsertCommand { get; }
		public ICommand CopyToClipboardCommand { get; }
		public ICommand OpenInUkadocCommand { get; }

		public UkadocSakuraScriptViewModel(UkadocSakuraScriptModel model)
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
							Process.Start(UkadocDownloader.ScriptReferenceUrl);
						else
							Process.Start(string.Format("{0}#{1}", UkadocDownloader.ScriptReferenceUrl, model.DocumentId));
					}
					catch { }
				});
		}

		public bool Filter(string filterString)
		{
			return Name.ToLower().Contains(filterString.ToLower());
		}
	}
}
