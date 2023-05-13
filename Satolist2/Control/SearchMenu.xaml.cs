using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
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
	/// SearchMenu.xaml の相互作用ロジック
	/// </summary>
	public partial class SearchMenu : UserControl
	{
		public SearchMenu()
		{
			InitializeComponent();
			
		}
	}

	internal class SearchMenuViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "SearchMenu";
		private string searchString;
		private bool isSearchTitle;
		private bool isSearchBody;
		private bool isRegiex;
		private bool hasRegexError;

		public string DockingTitle => "検索";
		public string DockingContentId => ContentId;

		public MainViewModel Main { get; }

		//検索文字列
		public string SearchString
		{
			get => searchString;
			set
			{
				searchString = value;
				NotifyChanged();
				SearchCommand.NotifyCanExecuteChanged();
			}
		}

		//項目名を検索するかどうか
		public bool IsSearchTitle
		{
			get => isSearchTitle;
			set
			{
				isSearchTitle = value;
				NotifyChanged();
			}
		}

		//本文を検索するかどうか
		public bool IsSearchBody
		{
			get => isSearchBody;
			set
			{
				isSearchBody = value;
				NotifyChanged();
			}
		}

		//正規表現検索を使用するか
		public bool IsRegex
		{
			get => isRegiex;
			set
			{
				isRegiex = value;
				NotifyChanged();
			}
		}

		//正規表現にエラーがあるかどうか
		public bool HasRegexError
		{
			get => hasRegexError;
			set
			{
				hasRegexError = value;
				NotifyChanged();
			}
		}

		public ActionCommand SearchCommand { get; }
		public ActionCommand BackToEditorCommand { get; }

		public SearchMenuViewModel(MainViewModel main)
		{
			Main = main;
			isSearchBody = true;
			isSearchTitle = true;

			SearchCommand = new ActionCommand(
				o =>
				{
					var query = new SearchQuery()
					{
						IsSearchBody = IsSearchBody,
						IsSearchTitle = IsSearchTitle,
						SearchString = searchString
					};

					if (isRegiex)
					{
						try
						{
							query.SearchRegex = new Regex(searchString);
						}
						catch
						{
							//正規表現パターン不正
							HasRegexError = true;
							return;
						}
					}
					else
					{
						query.SearchString = searchString;
					}

					//ここまでくればエラー表示は消える
					HasRegexError = false;

					//検索の実行。
					//searchResultに検索を投げる
					Main.SearchResultViewModel.RunSearch(query);
				},

				//検索文字列があるかどうか、何かしら検索対象に選択しているかどうか
				o => !string.IsNullOrEmpty(SearchString) && (IsSearchBody || isSearchTitle)
				);

			BackToEditorCommand = new ActionCommand(
				o =>
				{
					//閉じる
					Main.MainWindow.IsShowSearchMenu = false;
					Main.MainWindow.ActivateActiveEditor();
				});
		}

	}
}
