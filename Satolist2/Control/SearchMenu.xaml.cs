using Satolist2.Utility;
using System;
using System.Collections.Generic;
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

		public ActionCommand SearchCommand { get; }

		public SearchMenuViewModel(MainViewModel main)
		{
			Main = main;

			SearchCommand = new ActionCommand(
				o =>
				{
					//検索の実行。
					//searchResultに検索を投げる
					Main.SearchResultViewModel.RunSearch(SearchString);
				},
				o => !string.IsNullOrEmpty(SearchString)
				);
		}
	}
}
