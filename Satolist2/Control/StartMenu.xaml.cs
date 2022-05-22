using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
using Image = System.Drawing.Image;

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
					MainViewModel.EditorSettings.SaveTemporarySettings();	//一旦保存
					RefleshList();
				}
				);

			//リスト構築
			RefleshList();
		}

		//選択中のゴーストを開く
		public void OpenSelectedGhost()
		{
			OpenGhost(selectedItem);
		}

		//ゴーストを開く
		public void OpenGhost(GhostItemViewModel item)
		{
			if (selectedItem != null)
			{
				mainVm.MainWindow.OpenGhost(item.Path, executablePath: item.RunningExecutablePath);
			}
		}

		//RemoteHistory
		public void RemoveHistory(GhostItemViewModel item)
		{
			items.Remove(item);
			MainViewModel.EditorSettings.TemporarySettings.RemoveHistory(item.Path);
		}

		//リスト表示の再構築
		public void RefleshList()
		{
			var fmoReader = new SakuraFMOReader();
			fmoReader.Read();
			fmoReader.RemoveSurfacePreviewGeneratorRuntime();
			items.Clear();

			//起動中のゴーストを一番最初に表示
			foreach (var item in fmoReader.Records)
			{
				items.Add(new GhostItemViewModel(this)
				{
					Name = item.Value.GhostName,
					Path = item.Value.GhostPath,
					IsRunning = true,
					RunningExecutablePath = item.Value.ExecutablePath
				});
			}

			//ヒストリーから表示(お気に入りを優先)
			foreach (var item in MainViewModel.EditorSettings.TemporarySettings.GhostHistory.OrderBy(o => !o.IsFavorite))
			{
				//既に起動中ならスキップ
				var foundItem = items.FirstOrDefault(o => o.Path == item.Path);
				if (foundItem == null)
				{
					var addItem = new GhostItemViewModel(this)
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
			set => path = DictionaryUtility.RemoveLastSlash(DictionaryUtility.NormalizeFullPath(value));
		}

		//プレビューイメージ
		//サーフェスパレットの機能が出力してくるのでつかう
		public Image Image
		{
			get 
			{
				try
				{
					var bitmapPath = DictionaryUtility.ConbinePath(Path, "ghost/master/profile/satolist/preview.png");
					if (System.IO.File.Exists(bitmapPath))
					{
						return Bitmap.FromFile(bitmapPath);
					}
				}
				catch { }
				return null;
			}
		}

		//ヒストリーに存在するレコード
		public bool IsHistory { get; private set; }
		//起動中のゴースト
		public bool IsRunning { get; set; }
		//起動中のゴーストを実行しているsspのパス
		public string RunningExecutablePath { get; set; }

		public ActionCommand ToggleFavoriteCommand { get;}
		public ActionCommand OpenCommand { get; }
		public ActionCommand OpenExplorerCommand { get; }
		public ActionCommand RemoveCommand { get; }

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
					MainViewModel.EditorSettings.SaveTemporarySettings();
				}
			}
		}

		public GhostItemViewModel(StartMenuViewModel parent)
		{
			//お気に入り設定をトグル
			ToggleFavoriteCommand = new ActionCommand(
				o =>
				{
					IsFavorite = !IsFavorite;
				}
				);

			//開く
			OpenCommand = new ActionCommand(
				o =>
				{
					parent.OpenGhost(this);
				}
				);

			//エクスプローラで開く
			OpenExplorerCommand = new ActionCommand(
				o =>
				{
					try
					{
						System.Diagnostics.Process.Start(Path);
					}
					catch { }
				}
				);

			//削除
			RemoveCommand = new ActionCommand(
				o =>
				{
					parent.RemoveHistory(this);
				},
				o => !IsRunning	//起動中だと無条件に表示するので削除なし
				);
		}

		public void SetHistoryModel(OpenGhostHistory history)
		{
			IsHistory = true;
			historyModel = history;
			isFavorite = historyModel.IsFavorite;
		}
	}
}
