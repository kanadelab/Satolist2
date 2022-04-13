using AvalonDock.Controls;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Satolist2.Control;
using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
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

namespace Satolist2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private List<DockingWindow> EventEditors { get; }
		private List<DockingWindow> TextEditors { get; }
		private MainViewModel mainViewModel;

		private LayoutDocumentPane DocumentPane { get; set; }


		public MainWindow()
		{
			InitializeComponent();

			//デフォルト状態で閉じておくもの
			GhostDescriptEditor.IsVisible = false;
			GhostInstallEditor.IsVisible = false;
			UpdateIgnoreList.IsVisible = false;
			SaoriList.IsVisible = false;
			ReplaceList.IsVisible = false;
			VariableList.IsVisible = false;

			AllowDrop = true;
			EventEditors = new List<DockingWindow>();
			TextEditors = new List<DockingWindow>();

			ReflectVisibleMenuDataContext();

			//カラのVM生成
			mainViewModel = new MainViewModel(this);
			DataContext = mainViewModel;
			ReflectControlViewModel(mainViewModel);

			//レイアウトの復元
			DeserializeLayout(TemporarySettings.Instance.SerializedDockingLayout);

			//DocumentPane探す
			DocumentPane = FindDocumentPane(DockingManager.Layout);

			//スタートメニューは自動で閉じるので復活する
			StartMenu.Show();

#if DEPLOY
			//公開時はデバッグメニューを封じておく。今のところ根本に消すわけではないけど
			DebugMainMenuVisibleMenu.Visibility = Visibility.Collapsed;
			DebugMainMenuVisibleMenu.IsEnabled = false;
			DebugMainMenu.Hide();
#endif
		}

		private LayoutDocumentPane FindDocumentPane(ILayoutContainer panel)
		{
			if (panel.Children != null)
			{
				foreach (var c in panel.Children)
				{
					if (c is LayoutDocumentPane documentPane)
					{
						return documentPane;
					}
					else if (c is ILayoutContainer childPanel)
					{
						var found = FindDocumentPane(childPanel);
						if (found != null)
							return found;
					}
				}
			}
			return null;
		}

		private void MainWindow_Drop(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var items = (string[])e.Data.GetData(DataFormats.FileDrop);
				OpenGhost(items.First(), "master");
			}
		}

		private void MainWindow_DragEnter(object sender, DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
		}

		internal void OpenEventEditor(EventModel ev)
		{
			var currentWindow = EventEditors.FirstOrDefault(o => ((EventEditorViewModel)o.ViewModel).Event == ev);
			if (currentWindow == null)
			{
				var newWindow = new DockingWindow(new EventEditor(), new EventEditorViewModel(mainViewModel, ev));
				newWindow.CanClose = true;
				newWindow.CanHide = false;

				//閉じたときのイベントを設定
				newWindow.Closing += EventEditorClosing;
				ev.OnRemove += OpendEventRemoved;

				EventEditors.Add(newWindow);
				//DockingManager.
				DocumentPane.Children.Add(newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
		}

		//一致するテキストファイルの編集画面を閉じる
		//辞書のシリアライズステータスの切り替え
		public void CloseDictionaryEditors(DictionaryModel dict)
		{
			{
				var editors = EventEditors.Where(
					o => ReferenceEquals(((EventEditorViewModel)o.ViewModel).Event.Dictionary, dict)
					);
				foreach (var e in editors.ToArray())
				{
					e.Close();
				}
			}

			{
				var editors = TextEditors.Where(
					o => ReferenceEquals(((TextEditorViewModel)o.ViewModel).TextFile, dict)
					);
				foreach(var e in editors.ToArray())
				{
					e.Close();
				}
			}
		}

		//イベントが削除されたときイベントエディタを閉じる
		private void OpendEventRemoved(EventModel obj)
		{
			var removedEvent = EventEditors.FirstOrDefault(
				o => ((EventEditorViewModel)o.ViewModel).Event == obj
				);
			if (removedEvent != null)
				removedEvent.Close();
		}

		//イベントエディタを閉じた
		private void EventEditorClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//登録イベントを解除
			if(sender is DockingWindow window)
			{
				EventEditors.Remove(window);
				window.Closing -= EventEditorClosing;
				if (window.ViewModel is EventEditorViewModel viewModel)
					viewModel.Event.OnRemove -= OpendEventRemoved;
			}
		}

		internal void OpenTextEditor(TextFileModel text)
		{
			var currentWindow = TextEditors.FirstOrDefault(o => ((TextEditorViewModel)o.ViewModel).TextFile == text);
			if(currentWindow == null)
			{
				var newWindow = new DockingWindow(new TextEditor(), new TextEditorViewModel(text));
				newWindow.CanClose = true;
				newWindow.CanHide = false;

				TextEditors.Add(newWindow);
				DocumentPane.Children.Add(newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
		}

		internal void OpenGhost(string ghostPath, string shellDirectoryName = "master")
		{
			var ghost = new GhostModel(ghostPath);

			TemporarySettings.Instance.AddHistory(ghost);

			//無事に開けたらヒストリーに足す
			mainViewModel = new MainViewModel(this, ghost, shellDirectoryName);
			DataContext = mainViewModel;
			ReflectControlViewModel(mainViewModel);

			//開けたらスタートメニューを閉じる
			StartMenu.Hide();
		}

		//各コントロールのViewModelの再バインド
		private void ReflectControlViewModel(MainViewModel mainVm)
		{
			FileEventTree.ViewModel = mainVm.FileEventTreeViewModel;
			EventList.ViewModel = mainVm.EventListViewModel;
			SurfacePalette.ViewModel = mainVm.SurfacePaletteViewModel;
			SurfaceViewer.ViewModel = mainVm.SurfaceViewerViewModel;
			DebugMainMenu.ViewModel = mainVm.DebugMainMenuViewModel;
			StartMenu.ViewModel = mainVm.StartMenuViewModel;
			GhostDescriptEditor.ViewModel = mainVm.GhostDescriptEditorViewModel;
			GhostInstallEditor.ViewModel = mainVm.GhostInstallEditorViewModel;
			UpdateIgnoreList.ViewModel = mainVm.UpdateIgnoreListViewModel;
			SaoriList.ViewModel = mainVm.SaoriListViewModel;
			ReplaceList.ViewModel = mainVm.ReplaceListViewModel;
			VariableList.ViewModel = mainVm.VariableListViewModel;
		}

		private void ReflectVisibleMenuDataContext()
		{
			//デシリアライズ時、DockingWindowも作り直し食らうので
			//ちょっと微妙だけど、バインディングの繋ぎ変えを一括で行う
			FileEventTreeVisibleMenu.DataContext = FileEventTree;
			EventListVisibleMenu.DataContext = EventList;
			SurfaceViewerVisibleMenu.DataContext = SurfaceViewer;
			SurfacePaletteVisibleMenu.DataContext = SurfacePalette;
			DebugMainMenuVisibleMenu.DataContext = DebugMainMenu;
			StartMenuVisibleMenu.DataContext = StartMenu;
			GhostDescriptEditorVisibleMenu.DataContext = GhostDescriptEditor;
			GhostInstallEditorVisibleMenu.DataContext = GhostInstallEditor;
			UpdateIgnoreListVisibleMenu.DataContext = UpdateIgnoreList;
			SaoriListVisibleMenu.DataContext = SaoriList;
			ReplaceListVisibleMenu.DataContext = ReplaceList;
			VariableListVisibleMenu.DataContext = VariableList;
			
			//
			
		}

		/*
		private void OpenGhost(string path)
		{
			//ゴーストルートフォルダを開く。
			
			

		}
		*/

		//レイアウトの保存と読込
		private string SerializeDockingLayout()
		{
			XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockingManager);
			using(var writer = new System.IO.StringWriter())
			{
				serializer.Serialize(writer);
				return writer.ToString();
			}
		}

		private void DeserializeLayout(string serializedLayout)
		{
			//無効な設定
			if (string.IsNullOrEmpty(serializedLayout))
				return;
			//return;	
			//読込
			try
			{
				XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockingManager);
				using (var reader = new System.IO.StringReader(serializedLayout))
				{
					serializer.LayoutSerializationCallback += Serializer_LayoutSerializationCallback;
					serializer.Deserialize(reader);
					ReflectVisibleMenuDataContext();
				}
			}
			catch
			{
				//だめそうだったらパス
			}
		}

		private void Serializer_LayoutSerializationCallback(object sender, LayoutSerializationCallbackEventArgs e)
		{
		//	throw new NotImplementedException();
			switch(e.Model.ContentId)
			{
				case FileEventTreeViewModel.ContentId:
					FileEventTree = (DockingWindow)e.Model;
					break;
				case EventListViewModel.ContentId:
					EventList = (DockingWindow)e.Model;
					break;
				case SurfacePaletteViewModel.ContentId:
					SurfacePalette = (DockingWindow)e.Model;
					break;
				case SurfaceViewerViewModel.ContentId:
					SurfaceViewer = (DockingWindow)e.Model;
					break;
				case StartMenuViewModel.ContentId:
					StartMenu = (DockingWindow)e.Model;
					break;
				case DebugMainMenuViewModel.ContentId:
					DebugMainMenu = (DockingWindow)e.Model;
					break;
				case GhostDescriptEditorViewModel.ContentId:
					GhostDescriptEditor = (DockingWindow)e.Model;
					break;
				case GhostInstallEditorViewModel.ContentId:
					GhostInstallEditor = (DockingWindow)e.Model;
					break;
				case UpdateIgnoreListViewModel.ContentId:
					UpdateIgnoreList = (DockingWindow)e.Model;
					break;
				case SaoriListViewModel.ContentId:
					SaoriList = (DockingWindow)e.Model;
					break;
				case ReplaceListViewModel.ContentId:
					ReplaceList = (DockingWindow)e.Model;
					break;
				case VariableListViewModel.ContentId:
					VariableList = (DockingWindow)e.Model;
					break;
				default:
					//イベントエディタ等一時的なモノはデシリアライズする必要はない
					e.Cancel = true;
					break;
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//保存するかを尋ねる
			if(DataContext is MainViewModel vm)
			{
				if(vm.IsChanged)
				{
					if(!vm.AskSave())
					{
						e.Cancel = true;
						return;
					}
				}
			}

			//ウインドウを閉じるときに、複製する
			TemporarySettings.Instance.SerializedDockingLayout = SerializeDockingLayout();
		}
	}

	//ワークスペースは切り離せるのが望ましそう。Dockのビューモデルとかもくっついてそうだし
	internal class MainViewModel : NotificationObject
	{
		public static EditorSettings EditorSettings { get; private set; }

		public MainWindow MainWindow { get; }
		public GhostModel Ghost { get; private set; }

		//それぞれのドッキングウィンドウ
		public FileEventTreeViewModel FileEventTreeViewModel { get; }
		public EventListViewModel EventListViewModel { get; }
		public SurfaceViewerViewModel SurfaceViewerViewModel { get; }
		public SurfacePaletteViewModel SurfacePaletteViewModel { get; }
		public DebugMainMenuViewModel DebugMainMenuViewModel { get; }
		public StartMenuViewModel StartMenuViewModel { get; }
		public GhostDescriptEditorViewModel GhostDescriptEditorViewModel { get; }
		public GhostInstallEditorViewModel GhostInstallEditorViewModel { get; }
		public UpdateIgnoreListViewModel UpdateIgnoreListViewModel { get; }
		public SaoriListViewModel SaoriListViewModel { get; }
		public ReplaceListViewModel ReplaceListViewModel { get; }
		public VariableListViewModel VariableListViewModel { get; }
		

		public List<EventEditorViewModel> EventEditors { get; }
		public SatoriConfWrapper SatoriConfViewModel { get; }

		//汎用コマンド
		public ActionCommand SaveFileCommand { get; }
		public ActionCommand OpenGhostDirectoryCommand { get; }
		public ActionCommand EditInsertPaletteCommand { get; }
		public ActionCommand ReloadShioriCommand { get; }

		//設定情報
		public InsertItemPaletteModel InsertPalette
		{
			get => EditorSettings.InsertPalette;
			private set
			{
				EditorSettings.InsertPalette = value;
				NotifyChanged();
			}
		}

		public IEnumerable<ISaveFileObject> SaveLoadPanes
		{
			get
			{
				yield return GhostDescriptEditorViewModel;
				yield return GhostInstallEditorViewModel;
				yield return ReplaceListViewModel.ReplaceSaveObject;
				yield return ReplaceListViewModel.ReplaceAfterSaveObject;
				yield return UpdateIgnoreListViewModel.DeleteSaveObject;
				yield return UpdateIgnoreListViewModel.DeveloperOptionsSaveObject;
				yield return SatoriConfViewModel;
			}
		}

		public bool IsChanged
		{
			get
			{
				foreach (var item in SaveLoadPanes)
					if (item.IsChanged)
						return true;

				if(Ghost != null)
				{
					foreach (var item in Ghost.Dictionaries)
						if (item.IsChanged)
							return true;
				}
				return false;
			}
		}

		public MainViewModel(MainWindow mainWindow, GhostModel ghost = null, string shellDirectoryName = null)
		{
			MainWindow = mainWindow;
			Ghost = ghost;
			string shellPath = null;

			//エディタ設定のロード
			if(EditorSettings == null)
			{
				EditorSettings = new EditorSettings();
				//同じタイミングでリストデータ周辺をロード
				DataModelManager.Load();
			}

			if(ghost != null && !string.IsNullOrEmpty(shellDirectoryName))
			{
				shellPath = ghost.FullPath + "/shell/" + shellDirectoryName;
			}

			EventEditors = new List<EventEditorViewModel>();
			FileEventTreeViewModel = new FileEventTreeViewModel(this);
			EventListViewModel = new EventListViewModel(this);
			SurfaceViewerViewModel = new SurfaceViewerViewModel(shellPath);
			SurfacePaletteViewModel = new SurfacePaletteViewModel(shellPath);
			DebugMainMenuViewModel = new DebugMainMenuViewModel(this,
				System.IO.Path.GetFullPath("../../../TestSampleGhost"),
				"master"
				);
			StartMenuViewModel = new StartMenuViewModel(this);
			GhostDescriptEditorViewModel = new GhostDescriptEditorViewModel(this);
			GhostInstallEditorViewModel = new GhostInstallEditorViewModel(this);
			UpdateIgnoreListViewModel = new UpdateIgnoreListViewModel(this);
			SatoriConfViewModel = new SatoriConfWrapper(this);
			SaoriListViewModel = new SaoriListViewModel(this);
			ReplaceListViewModel = new ReplaceListViewModel(this);
			VariableListViewModel = new VariableListViewModel(this);

			SaveFileCommand = new ActionCommand(
				o =>
				{
					AskSave();
				});

			OpenGhostDirectoryCommand = new ActionCommand(
				o =>
				{
					try
					{
						Process.Start(ghost.FullDictionaryPath);
					}
					catch { }
				},
				o => ghost != null

				);

			EditInsertPaletteCommand = new ActionCommand(
				o =>
				{
					var dialog = new TextEditorInsertPaletteSettingsDialog();
					using (var vm = new TextEditorInsertPaletteSettingsDialogViewModel(dialog, InsertPalette))
					{
						dialog.DataContext = vm;
						if(dialog.ShowDialog() == true)
						{
							InsertPalette = vm.Items.First().ToModel();
						}
					}
				}
				);

			ReloadShioriCommand = new ActionCommand(
				o => GhostRuntimeRequest.ReloadShiori(ghost),
				o => ghost != null
				);
			

			//読込エラーが発生している場合に通知
			foreach(var err in SaveLoadPanes.Where(o => o.LoadState == EditorLoadState.LoadFailed))
			{
				//TODO: ロードエラー通知
			}
		}

		//イベントエディタのオープン
		public void OpenEventEditor(EventModel ev)
		{
			MainWindow.OpenEventEditor(ev);
		}

		public void OpenEventEditor(InlineEventModel ev)
		{
			//TODO: 親のイベントを開くので、カレットの位置をインラインイベントに合わせたい
			MainWindow.OpenEventEditor(ev.ParentEvent);
		}

		//イベントの追加
		public void OpenAddEventDialog(string name = null, string condition = null, EventType type = EventType.Sentence, DictionaryModel addTarget = null)
		{
			var dialog = new AddEventDialog(this);

			//カスタムするものがあれば追加設定
			if (name != null)
				dialog.DataContext.Name = name;
			if (condition != null)
				dialog.DataContext.Condition = condition;
			dialog.DataContext.Type = type;
			if (addTarget != null)
				dialog.DataContext.AddTarget = addTarget;

			//OKされたら追加
			if (dialog.ShowDialog() == true)
			{
				//新規で追加
				var newEvent = new EventModel(dialog.DataContext.Type, dialog.DataContext.Name, dialog.DataContext.Condition, string.Empty);
				dialog.DataContext.AddTarget.AddEvent(newEvent);
				OpenEventEditor(newEvent);
			}
		}

		//保存ダイアログを呼ぶ処理
		//falseが帰ったら終了をキャンセル
		public bool AskSave()
		{
			List<ISaveFileObject> objects = new List<ISaveFileObject>();
			objects.AddRange(SaveLoadPanes);
			if(Ghost != null)
				objects.AddRange(Ghost.Dictionaries);

			var dialog = new SaveFileListDialog();
			var dialogViewModel = new SaveFileListViewModel(objects);
			dialog.DataContext = dialogViewModel;
			dialog.ShowDialog();

			if(dialog.Result == MessageBoxResult.Yes)
			{
				//現時点ではエラーはダイアログに表示するだけにしておく
				var errorList = new List<string>();

				//保存
				foreach( var saveItem in dialogViewModel.Items.Where(o => o.IsSave))
				{
					bool success = saveItem.SaveItem.Save();
					if (!success)
						errorList.Add(saveItem.SaveFilePath);
				}

				if(errorList.Count > 0)
				{
					var message = string.Format("保存に失敗しました。\r\n\r\n{0}", DictionaryUtility.JoinLines(errorList));
					MessageBox.Show(message, "さとりすと", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return false;
				}
				return true;
				
			}
			else if(dialog.Result == MessageBoxResult.No)
			{
				return true;
			}
			else
			{
				return false;
			}
		}


	}
}
