using Satolist2.Model;
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
	/// StartMenu.xaml の相互作用ロジック
	/// </summary>
	public partial class StartMenu : UserControl
	{
		public StartMenu()
		{
			InitializeComponent();
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(DataContext is StartMenuViewModel vm)
			{
				vm.OpenSelectedGhost();
			}
		}
	}

	internal class StartMenuViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "StartMenu";
		private GhostItemViewModel selectedItem;
		private MainViewModel mainVm;
		private ObservableCollection<GhostItemViewModel> items;

		public IEnumerable<GhostItemViewModel> GhostList
		{
			get => items;
		}

		public GhostItemViewModel SelectedItem
		{
			get => selectedItem;
			set
			{
				selectedItem = value;
				NotifyChanged();
			}
		}

		public ActionCommand ReloadCommand { get; }

		public string DockingTitle => "スタートメニュー";

		public string DockingContentId => ContentId;

		public StartMenuViewModel(MainViewModel main)
		{
			mainVm = main;
			items = new ObservableCollection<GhostItemViewModel>();

			ReloadCommand = new ActionCommand(
				o =>
				{
					RefleshList();
				}
				);

			//リスト構築
			RefleshList();
		}

		public void OpenSelectedGhost()
		{
			if(selectedItem != null)
			{
				mainVm.MainWindow.OpenGhost(selectedItem.Path, executablePath: selectedItem.RunningExecutablePath);
			}
		}

		//リスト表示の再構築
		public void RefleshList()
		{
			var fmoReader = new SakuraFMOReader();
			fmoReader.Read();
			items.Clear();

			//起動中のゴーストを一番最初に表示
			foreach (var item in fmoReader.Records)
			{
				items.Add(new GhostItemViewModel()
				{
					Name = item.Value.GhostName,
					Path = item.Value.GhostPath,
					IsRunning = true,
					RunningExecutablePath = item.Value.ExecutablePath
				});
			}

			//ヒストリーから表示
			foreach (var item in MainViewModel.EditorSettings.TemporarySettings.GhostHistory)
			{
				//既に起動中ならスキップ
				var foundItem = items.FirstOrDefault(o => o.Path == item.Path);
				if (foundItem == null)
				{
					var addItem = new GhostItemViewModel()
					{
						Name = item.Name,
						Path = item.Path
					};
					addItem.SetHistoryModel(item);
					items.Add(addItem);
				}
				else
				{
					foundItem.SetHistoryModel(item);
				}
			}
		}
	}

	internal class GhostItemViewModel : NotificationObject
	{
		private string path;
		private bool isFavorite;
		private OpenGhostHistory historyModel;

		public string Name { get; set; }
		public string Path
		{
			get => path;
			set => path = DictionaryUtility.NormalizeFullPath(value);
		}

		//ヒストリーに存在するレコード
		public bool IsHistory { get; private set; }
		//起動中のゴースト
		public bool IsRunning { get; set; }
		//起動中のゴーストを実行しているsspのパス
		public string RunningExecutablePath { get; set; }

		public ActionCommand ToggleFavoriteCommand { get;}
	

		public bool IsFavorite
		{
			get => isFavorite;
			set
			{
				if (isFavorite != value)
				{
					isFavorite = value;
					if (historyModel != null)
					{
						historyModel.IsFavorite = isFavorite;
					}
					else if(isFavorite)
					{
						//ヒストリに存在しない情報でお気に入りに設定した場合は、その時点でヒストリに追加する
						var addedModel = MainViewModel.EditorSettings.TemporarySettings.AddHistory(Path, Name, isFavorite);
						SetHistoryModel(addedModel);
					}
					NotifyChanged();
				}
			}
		}

		public GhostItemViewModel()
		{
			//お気に入り設定をトグル
			ToggleFavoriteCommand = new ActionCommand(
				o =>
				{
					IsFavorite = !IsFavorite;
				}
				);
		}

		public void SetHistoryModel(OpenGhostHistory history)
		{
			IsHistory = true;
			historyModel = history;
			IsFavorite = historyModel.IsFavorite;
		}
	}
}
