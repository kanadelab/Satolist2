using AvalonDock.Controls;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Satolist2.Control;
using Satolist2.Core;
using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Shapes;

namespace Satolist2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : UserControl
	{
		private List<DockingWindow> EventEditors { get; }
		private List<DockingWindow> TextEditors { get; }
		private MainViewModel mainViewModel;
		private LayoutDocumentPane DocumentPane { get; set; }
		private DockingWindow ActiveEditor { get; set; }
		public Window RootWindow { get; set; }

		public ICSharpCode.AvalonEdit.TextEditor ActiveTextEditor
		{
			get
			{
				if (ActiveEditor == null)
					return null;
				if(ActiveEditor.ViewModel is TextEditorViewModelBase vm)
				{
					return vm.MainTextEditor;
				}
				return null;
			}
		}

		internal TextEditorViewModelBase ActiveTextEditorViewModel
		{
			get
			{
				if (ActiveEditor == null)
					return null;
				if (ActiveEditor.ViewModel is TextEditorViewModelBase vm)
				{
					return vm;
				}
				return null;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			DockingManager.Theme = Themes.ApplicationTheme.GetDockingSystemTheme();

			//デフォルト状態で閉じておくもの
			GhostDescriptEditor.IsVisible = false;
			GhostInstallEditor.IsVisible = false;
			UpdateIgnoreList.IsVisible = false;
			SaoriList.IsVisible = false;
			ReplaceList.IsVisible = false;
			VariableList.IsVisible = false;
			SearchMenu.IsVisible = false;
			RecvEventLog.IsVisible = false;
			InsertPalette.IsVisible = false;
			Satorite.IsVisible = false;
			DebugMainMenu.IsVisible = false;

			AllowDrop = true;
			EventEditors = new List<DockingWindow>();
			TextEditors = new List<DockingWindow>();

			ReflectVisibleMenuDataContext();

			//カラのVM生成
			mainViewModel = new MainViewModel(this);
			DataContext = mainViewModel;
			ReflectControlViewModel(mainViewModel);

			//レイアウトの復元
			DeserializeLayout(MainViewModel.EditorSettings.TemporarySettings.SerializedDockingLayout);

			//DocumentPane探す
			DocumentPane = FindDocumentPane(DockingManager.Layout);

			//スタートメニューは自動で閉じるので復活する
			StartMenu.Show();

			//さとりてウインドウのシンタックスハイライトを設定
			((SatoriteWindow)Satorite.Content).UpdateHilighting();
			((SatoriteWindow)Satorite.Content).UpdateFontSettings();

#if DEPLOY
			//公開時はデバッグメニューを封じておく。今のところ根本に消すわけではないけど
			DebugMainMenuVisibleMenu.Visibility = Visibility.Collapsed;
			DebugMainMenuVisibleMenu.IsEnabled = false;
			DebugMainMenu.Hide();
#endif
		}

		public void SetRoot(Window rootWindow)
		{
			RootWindow = rootWindow;

			//さとりすとのSSTP受信用ウインドウを起動
			RootWindow.Closed += MainWindow_Closed;
			RootWindow.Closing += Window_Closing;
			Dispatcher.BeginInvoke(new Action(() =>
			{
				Core.SSTPCallBackNativeWindow.Create((new System.Windows.Interop.WindowInteropHelper(RootWindow)).Handle);
			}), System.Windows.Threading.DispatcherPriority.Render);
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			//SSTPのウインドウ登録も消す
			Core.SSTPCallBackNativeWindow.Destory();
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

		internal EventEditor OpenEventEditor(EventModel ev)
		{
			var currentWindow = EventEditors.FirstOrDefault(o => ((EventEditorViewModel)o.ViewModel).Event == ev);
			if (currentWindow == null)
			{
				var viewModel = new EventEditorViewModel(mainViewModel, ev);
				var newWindow = new DockingWindow(new EventEditor(), viewModel);
				newWindow.CanClose = true;
				newWindow.CanHide = false;

				//閉じたときのイベントを設定
				newWindow.Closing += EventEditorClosing;
				newWindow.IsActiveChanged += NewWindow_IsActiveChanged;
				ev.OnRemove += OpendEventRemoved;

				viewModel.UpdateFontSettings();
				EventEditors.Add(newWindow);
				DocumentPane.Children.Insert(0, newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
			var editor = (EventEditor)currentWindow.Content;
			editor.RequestFocus();
			return editor;
		}

		private void NewWindow_IsActiveChanged(object sender, EventArgs e)
		{
			if(sender is DockingWindow dockWindow)
			{
				//アクティブになったのでアクティブなエディタをマークする
				if(dockWindow.IsActive && ActiveEditor != dockWindow)
				{
					if(ActiveTextEditorViewModel != null)
					{
						ActiveTextEditorViewModel.IsActiveTextEditor = false;
					}
					ActiveEditor = dockWindow;
					if (ActiveTextEditorViewModel != null)
					{
						ActiveTextEditorViewModel.IsActiveTextEditor = true;
					}
				}
			}
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
				window.IsActiveChanged -= NewWindow_IsActiveChanged;
				if (window.ViewModel is EventEditorViewModel viewModel)
					viewModel.Event.OnRemove -= OpendEventRemoved;
			}
		}

		internal TextEditor OpenTextEditor(TextFileModel text)
		{
			if (!text.BodyAvailable)
				throw new Exception();  //多分リストモードのファイルを開こうとしてる

			var currentWindow = TextEditors.FirstOrDefault(o => ((TextEditorViewModel)o.ViewModel).TextFile == text);
			if(currentWindow == null)
			{
				var viewModel = new TextEditorViewModel(mainViewModel, text);
				var newWindow = new DockingWindow(new TextEditor(), viewModel);
				newWindow.CanClose = true;
				newWindow.CanHide = false;
				newWindow.Closing += TextEditorClosing;
				newWindow.IsActiveChanged += NewWindow_IsActiveChanged;
				text.OnDelete += TextFileDeleted;
				viewModel.UpdateFontSettings();

				TextEditors.Add(newWindow);
				DocumentPane.Children.Add(newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
			var editor = (TextEditor)currentWindow.Content;
			editor.RequestFocus();
			return editor;
		}

		private void TextFileDeleted(TextFileModel obj)
		{
			var deletedEditor = TextEditors.FirstOrDefault(
				o => ((TextEditorViewModel)o.ViewModel).TextFile == obj
				);
			if (deletedEditor != null)
				deletedEditor.Close();
		}

		private void TextEditorClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (sender is DockingWindow window)
			{
				TextEditors.Remove(window);
				window.Closing -= TextEditorClosing;
				window.IsActiveChanged -= NewWindow_IsActiveChanged;
				if (window.ViewModel is TextEditorViewModel vm)
				{
					vm.TextFile.OnDelete -= TextFileDeleted;
				}
			}
		}

		internal TemporaryTextEditor OpenTemporaryTextEditor(string body, string title)
		{
			var editor = new TemporaryTextEditor();
			var viewModel = new TemporaryTextEditorViewModel()
			{
				Title = title,
				Text = body
			};
			var newWindow = new DockingWindow(editor, viewModel);
			newWindow.CanClose = true;
			newWindow.CanHide = false;
			viewModel.UpdateFontSettings();

			DocumentPane.Children.Add(newWindow);

			//アクティベート
			newWindow.IsActive = true;
			editor.RequestFocus();
			return editor;
		}

		//テキストエディタのフォントを更新
		internal void UpdateTextEditorFonts()
		{
			foreach(var item in EventEditors)
			{
				if( item.ViewModel is TextEditorViewModelBase vm)
				{
					vm.UpdateFontSettings();
				}
			}

			foreach (var item in TextEditors)
			{
				if (item.ViewModel is TextEditorViewModelBase vm)
				{
					vm.UpdateFontSettings();
				}
			}

			((SatoriteWindow)Satorite.Content).UpdateFontSettings();
			//NOTE: テンポラリエディタはすぐ閉じるだろうし一旦は考えてない
		}

		internal void UpdateTextEditorHilights()
		{
			foreach (var item in EventEditors)
			{
				if (item.ViewModel is TextEditorViewModelBase vm)
				{
					vm.UpdateHilightSettings();
				}
			}

			foreach (var item in TextEditors)
			{
				if (item.ViewModel is TextEditorViewModelBase vm)
				{
					vm.UpdateHilightSettings();
				}
			}

			((SatoriteWindow)Satorite.Content).UpdateHilighting();
		}

		internal void OpenGhost(string ghostPath, string shellDirectoryName = "master", string executablePath = null)
		{
			//ゴーストのロード
			var ghost = new GhostModel(ghostPath);
			RootWindow.Title = string.Format("{0} - さとりすとv2", ghost.GhostDescriptName);

			//メインの起動
			var init = new MainViewModelInitializeData();
			init.Ghost = ghost;
			init.ShellDirectoryName = shellDirectoryName;
			init.RunningExecutablePath = executablePath;
			
			mainViewModel = new MainViewModel(this, init);
			DataContext = mainViewModel;
			ReflectControlViewModel(mainViewModel);

			//無事に開けたらヒストリーに足す
			MainViewModel.EditorSettings.TemporarySettings.AddHistory(ghost);
			MainViewModel.EditorSettings.SaveTemporarySettings();

			//テキストエディタ類を全部閉じる（開くより前のゴーストインスタンスに紐づく為）
			foreach (var e in TextEditors.ToArray())
				e.Close();
			foreach (var e in EventEditors.ToArray())
				e.Close();

			//開けたらスタートメニューを閉じる
			StartMenuVisibleMenu.IsChecked = false;

			//ログ表示
			var logStr = string.Format("ゴースト「{0}」を開きました。 ({1})", ghost.GhostDescriptName, ghost.FullPath);
			LogMessage.AddLog(logStr);
		}

		//各コントロールのViewModelの再バインド
		private void ReflectControlViewModel(MainViewModel mainVm)
		{
			FileEventTree.ViewModel = mainVm.FileEventTreeViewModel;
			EventList.ViewModel = mainVm.EventListViewModel;
			SearchMenu.ViewModel = mainViewModel.SearchMenuViewModel;
			SearchResult.ViewModel = mainVm.SearchResultViewModel;
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
			RecvEventLog.ViewModel = mainVm.RecvEventLogViewModel;
			InsertPalette.ViewModel = mainVm.InsertPaletteViewModel;
			Satorite.ViewModel = mainVm.SatoriteViewModel;
		}

		private void ReflectVisibleMenuDataContext()
		{
			//デシリアライズ時、DockingWindowも作り直し食らうので
			//ちょっと微妙だけど、バインディングの繋ぎ変えを一括で行う
			FileEventTreeVisibleMenu.DataContext = FileEventTree;
			EventListVisibleMenu.DataContext = EventList;
			SearchMenuVisibleMenu.DataContext = SearchMenu;
			SearchResultVisibleMenu.DataContext = SearchResult;
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
			RecvEventLogVisibleMenu.DataContext = RecvEventLog;
			InsertPaletteVisibleMenu.DataContext = InsertPalette;
			SatoriteVisibleMenu.DataContext = Satorite;
			//
			
		}

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
				case SearchMenuViewModel.ContentId:
					SearchMenu = (DockingWindow)e.Model;
					break;
				case SearchResultViewModel.ContentId:
					SearchResult = (DockingWindow)e.Model;
					break;
				case RecvEventLogViewModel.ContentId:
					RecvEventLog = (DockingWindow)e.Model;
					break;
				case InsertPaletteViewModel.ContentId:
					InsertPalette = (DockingWindow)e.Model;
					break;
				case SatoriteViewModel.ContentId:
					Satorite = (DockingWindow)e.Model;
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
				if(!vm.AskSave(true))
				{
					e.Cancel = true;
					return;
				}
			}

			//ウインドウを閉じるときに、複製する
			MainViewModel.EditorSettings.TemporarySettings.SerializedDockingLayout = SerializeDockingLayout();
		}

		//アクティブなテキストエディタをアクティブにする
		//挿入機能等で別のコントロールをアクティブにしている想定で、テキストエディタをアクティブにに戻す
		public void ActivateActiveEditor()
		{
			if(!ActiveEditor.IsActive)
			{
				ActiveEditor.IsActive = true;
				var content = ActiveEditor.Content;
				if(content is EventEditor ev)
				{
					ev.RequestFocus();
				}
				else if(content is TemporaryTextEditor tte)
				{
					tte.RequestFocus();
				}
				else if(content is TextEditor te)
				{
					te.RequestFocus();
				}
			}
		}
	}

	//MainViewModel起動オプション
	internal class MainViewModelInitializeData
	{
		public GhostModel Ghost { get; set; }
		public string ShellDirectoryName { get; set; }
		public string RunningExecutablePath { get; set; }
	}

	//ワークスペースは切り離せるのが望ましそう。Dockのビューモデルとかもくっついてそうだし
	internal class MainViewModel : NotificationObject
	{
		private bool isUpdateAvailable;
		private string updateVersionLabel;
		private UpdateReleaseInfo updateInfo;

		public static EditorSettings EditorSettings { get; private set; }
		public MainWindow MainWindow { get; }
		public GhostModel Ghost { get; private set; }
		public bool IsGhostEnable { get; private set; }
		public LogMessage LogMessage => Core.LogMessage.Instance;
		public SurfacePreviewViewModel SurfacePreview { get; }
		

		//それぞれのドッキングウィンドウ
		public FileEventTreeViewModel FileEventTreeViewModel { get; }
		public EventListViewModel EventListViewModel { get; }
		public SearchMenuViewModel SearchMenuViewModel { get; }
		public SearchResultViewModel SearchResultViewModel { get; }
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
		public RecvEventLogViewModel RecvEventLogViewModel { get; }
		public InsertPaletteViewModel InsertPaletteViewModel { get; }
		public SatoriteViewModel SatoriteViewModel { get; }
		

		public List<EventEditorViewModel> EventEditors { get; }
		public SatoriConfWrapper SatoriConfViewModel { get; }

		//汎用コマンド
		public ActionCommand SaveFileCommand { get; }
		public ActionCommand ReopenGhostCommand { get; }
		public ActionCommand AddSatoriDictionaryFileCommand { get; }
		public ActionCommand AddTextFileCommand { get; }
		public ActionCommand OpenGhostDirectoryCommand { get; }
		public ActionCommand OpenGhostDictionaryDirectoryCommand { get; }
		public ActionCommand OpenGhostShellDirectoryCommand { get; }
		public ActionCommand BootSSPCommand { get; }
		public ActionCommand NewGhostCommand { get; }
		public ActionCommand EditGeneralSettingsCommand { get; }
		public ActionCommand EditInsertPaletteCommand { get; }
		public ActionCommand EditUploadSettingCommand { get; }
		public ActionCommand EditTextEditorFontCommand { get; }
		public ActionCommand OpenSatolistDirectoryCommand { get; }
		public ActionCommand ReloadShioriCommand { get; }
		public ActionCommand ShowSearchBoxCommand { get; }
		public ActionCommand ExportNarCommand { get; }
		public ActionCommand MakeUpdateFileCommand { get; }
		public ActionCommand UploadGhostCommand { get; }
		public ActionCommand GenerateSurfacePreviewCommand { get; }
		public ActionCommand SelectPreviewShellCommand { get; }
		public ActionCommand SelectPreviewShellDirectoryCommand { get; }
		public ActionCommand ShowLogMessageCommand { get; }
		public ActionCommand ClearLogMessageCommand { get; }
		public ActionCommand NetworkUpdateCommand { get; }

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
				if (Ghost == null)
					return false;

				foreach (var item in SaveLoadPanes)
					if (item.IsChanged)
						return true;

				foreach (var item in Ghost.Dictionaries)
					if (item.IsChanged)
						return true;

				return false;
			}
		}

		public bool IsUpdateAvailable
		{
			get => isUpdateAvailable;
			set
			{
				isUpdateAvailable = value;
				NotifyChanged();
			}
		}

		public string UpdateVersionLabel
		{
			get => updateVersionLabel;
			set
			{
				updateVersionLabel = value;
				NotifyChanged();
			}
		}

		public MainViewModel(MainWindow mainWindow, MainViewModelInitializeData initializeData = null)
		{
			updateVersionLabel = string.Empty;

			initializeData = initializeData ?? new MainViewModelInitializeData();
			MainWindow = mainWindow;
			Ghost = initializeData.Ghost;

			IsGhostEnable = Ghost != null;
			string shellPath = null;

			//サーフェスプレビューデータを読込
			SurfacePreview = new SurfacePreviewViewModel(this);

			//エディタ設定のロード
			if (EditorSettings == null)
			{
				EditorSettings = new EditorSettings();

				if (EditorSettings.LoadErrors.Count > 0)
				{
					var errDialog = new ErrorListDialog(null, true);
					errDialog.DataContext.Title = "設定ファイル読込エラー";
					errDialog.DataContext.Description = "さとりすとの設定ファイルの読込でエラーが発生しました。\r\nファイルが破損している可能性があります。\r\nこのまま編集すると読込に失敗した設定が初期状態に戻りますが、続行しますか？";
					errDialog.DataContext.SetErrorItems(EditorSettings.LoadErrors);
					if (errDialog.ShowDialog() != true)
					{
						Environment.Exit(1);
					}
				}

				//同じタイミングでリストデータ周辺をロード
				try
				{
					DataModelManager.Load();
				}
				catch(Exception ex)
				{
					MessageBox.Show("さとりすとの起動に必要な設定ファイルのロードに失敗したため起動できません。\r\nさとりすとを上書き再インストールすると解決するかもしれません。\r\n\r\n" + ex, "エラー");
					Environment.Exit(1);
				}

				//ネットワークアップデート
				CheckNetworkUpdate(EditorSettings.GeneralSettings.IsEnablePreReleaseUpdate);
			}

			//ゴーストのローカル情報のロード
			if (Ghost != null)
			{
				EditorSettings.LoadGhostTemporarySettings(Ghost);

				//起動ベースウェアの位置を特定できたら保存しておく
				if (!string.IsNullOrEmpty(initializeData.RunningExecutablePath))
				{
					EditorSettings.GhostTemporarySettings.LastBootExecutePath = initializeData.RunningExecutablePath;
					EditorSettings.SaveGhostTemporarySettings(Ghost);
				}

				SurfacePreview.LoadShells();
			}

			EventEditors = new List<EventEditorViewModel>();
			FileEventTreeViewModel = new FileEventTreeViewModel(this);
			EventListViewModel = new EventListViewModel(this);
			SearchMenuViewModel = new SearchMenuViewModel(this);
			SearchResultViewModel = new SearchResultViewModel(this);
			SurfaceViewerViewModel = new SurfaceViewerViewModel(this);
			SurfacePaletteViewModel = new SurfacePaletteViewModel(this);
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
			RecvEventLogViewModel = new RecvEventLogViewModel(this);
			InsertPaletteViewModel = new InsertPaletteViewModel(this);
			SatoriteViewModel = new SatoriteViewModel(this);

			SaveFileCommand = new ActionCommand(
				o => AskSave(false),
				o => Ghost != null
				);

			ReopenGhostCommand = new ActionCommand(
				o =>
				{
					if(AskDiscard())
					{
						MainWindow.OpenGhost(Ghost.FullPath, initializeData.ShellDirectoryName);
					}
				},
				o => Ghost != null
				);

			AddSatoriDictionaryFileCommand = new ActionCommand(
				o =>
				{
					var saveDialog = new SaveFileDialog();
					saveDialog.Filter = "里々辞書ファイル(dic*.txt)|dic*.txt";
					saveDialog.InitialDirectory = DictionaryUtility.NormalizeWindowsPath(Ghost.FullDictionaryPath);
					saveDialog.AddExtension = true;
					saveDialog.OverwritePrompt = false;
					saveDialog.FileName = "dic_ghost.txt";
					saveDialog.FileOk += (sender, e) =>
					{
						
						if (!DictionaryUtility.IsChildPath(Ghost.FullDictionaryPath, saveDialog.FileName))
						{
							//ghost/master以下、辞書としてのみ追加が可能
							MessageBox.Show("ゴーストの辞書フォルダにのみファイルを追加できます。");
							e.Cancel = true;
							return;
						}

						if (System.IO.File.Exists(saveDialog.FileName))
						{
							MessageBox.Show("すでに存在するファイルは新規作成できません。");
							e.Cancel = true;
							return;
						}

						//既存のファイル名を拒否する。ファイルシステムかさとりすと上に存在するファイルは拒否
						if (Ghost.Dictionaries.Any(f => f.FullPath == saveDialog.FileName))
						{
							MessageBox.Show("さとりすとに読み込まれているファイルは新規作成できません。");
							e.Cancel = true;
							return;
						}

						//里々の辞書形式成約
						if(!Regex.IsMatch(System.IO.Path.GetFileName(saveDialog.FileName), "^dic.+\\.txt$"))
						{
							MessageBox.Show("里々の辞書は「dic*.txt」のファイル名の形式で保存する必要があります。");
							e.Cancel = true;
						}
					};

					if(saveDialog.ShowDialog() == true)
					{
						//ファイルを追加する
						Ghost.AddNewDictionary(saveDialog.FileName);
					}
				},
				o => Ghost != null
				);

			AddTextFileCommand = new ActionCommand(
				o =>
				{
					throw new NotImplementedException();

					var saveDialog = new SaveFileDialog();
					saveDialog.Filter = "テキストファイル(*.txt)|*.txt";
					saveDialog.AddExtension = true;
					saveDialog.OverwritePrompt = false;
					saveDialog.FileName = "text.txt";
					saveDialog.FileOk += (sender, e) =>
					{
						//既存のファイル名を拒否する。ファイルシステムかさとりすと上に存在するファイルは拒否
					};
				},
				o => Ghost != null
				);

			OpenGhostDirectoryCommand = new ActionCommand(
				o =>
				{
					try
					{
						Process.Start(Ghost.FullPath);
					}
					catch { }
				},
				o => Ghost != null
				);

			OpenGhostDictionaryDirectoryCommand = new ActionCommand(
				o =>
				{
					try
					{
						Process.Start(Ghost.FullDictionaryPath);
					}
					catch { }
				},
				o => Ghost != null
				);

			OpenGhostShellDirectoryCommand = new ActionCommand(
				o =>
				{
					try
					{
						Process.Start(DictionaryUtility.ConbinePath(Ghost.FullPath, "shell"));
					}
					catch { }
				},
				o => Ghost != null
				);

			BootSSPCommand = new ActionCommand(
				o =>
				{
					try
					{
						try
						{ 
							if (!string.IsNullOrEmpty(EditorSettings.GhostTemporarySettings.LastBootExecutePath))
							{
								SSPBootUtility.Boot(Ghost.FullPath, EditorSettings.GhostTemporarySettings.LastBootExecutePath);
								return;
							}
						}
						catch(System.IO.FileNotFoundException)
						{
							//起動に使用するSSPが見つからない場合、推測モードにフォールバック
						}

						//最後に起動したSSPで起動できなかった場合、フォルダを上へ遡る形でSSPの場所を推測して起動する
						var executablePath = SSPBootUtility.Boot(Ghost.FullPath);

						//起動に成功したらパスを格納して次回以降の起動に使用する
						EditorSettings.GhostTemporarySettings.LastBootExecutePath = executablePath;
						EditorSettings.SaveGhostTemporarySettings(Ghost);
					}
					catch(System.IO.FileNotFoundException)
					{
						MessageBox.Show("起動に使用するSSPが見つかりませんでした。\r\nいちど、先にSSPを起動した状態でさとりすとを開き、スタートメニューから起動中のゴーストを開くことで起動中のSSPを認識するようになります。");
					}
					catch
					{
						MessageBox.Show("起動に失敗しました。");
					}
				},
				o => Ghost != null
				);

			NewGhostCommand = new ActionCommand(
				o =>
				{
					var dialog = new NewGhostDialog(this);
					if( dialog.ShowDialog() == true)
					{
						//ゴーストの作成が実際に行われたのでそれを開く
						MainWindow.OpenGhost(dialog.DataContext.EffectiveCreatePath);
					}
				},
				o => Ghost == null	//開いてない場合のみ
				);

			EditGeneralSettingsCommand = new ActionCommand(
				o =>
				{
					var dialog = new GeneralSettingsDialog(this);
					if(dialog.ShowDialog() == true)
					{
						EditorSettings.GeneralSettings = dialog.DataContext.Model;
						EditorSettings.SaveGeneralSettings();

						//エディタの更新
						MainWindow.UpdateTextEditorHilights();
					}
				}
				);

			EditInsertPaletteCommand = new ActionCommand(
				o =>
				{
					var dialog = new TextEditorInsertPaletteSettingsDialog(this);
					using (var vm = new TextEditorInsertPaletteSettingsDialogViewModel(dialog, InsertPalette))
					{
						dialog.DataContext = vm;
						if(dialog.ShowDialog() == true)
						{
							InsertPalette = vm.Items.First().ToModel();
							EditorSettings.SaveInsertPalette();
							InsertPaletteViewModel.NotifyModelUpdated();
						}
					}
				}
				);

			EditUploadSettingCommand = new ActionCommand(
				o =>
				{
					var d = new Dialog.UploadSettingDialog(EditorSettings.UploadSettings, this);
					if( d.ShowDialog() == true)
					{
						EditorSettings.UploadSettings = d.DataContext.GetItems();
						EditorSettings.SaveUploadSettings();
					}
				}
				);

			EditTextEditorFontCommand = new ActionCommand(
				o =>
				{
					var fontDialog = new System.Windows.Forms.FontDialog();
					fontDialog.AllowScriptChange = false;
					fontDialog.AllowVerticalFonts = false;
					fontDialog.AllowSimulations = false;
					fontDialog.AllowVectorFonts = true;
					fontDialog.FontMustExist = true;

					if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					{
						EditorSettings.GeneralSettings.TextEditorFontName = fontDialog.Font.Name;
						EditorSettings.GeneralSettings.TextEditorFontSize = fontDialog.Font.SizeInPoints;
						MainWindow.UpdateTextEditorFonts();
						EditorSettings.SaveGeneralSettings();
					}
				}
				);

			OpenSatolistDirectoryCommand = new ActionCommand(
				o =>
				{
					try
					{
						Process.Start(Environment.CurrentDirectory);
					}
					catch { }
				}
				);

			ReloadShioriCommand = new ActionCommand(
				o => GhostRuntimeRequest.ReloadShiori(Ghost),
				o => Ghost != null
				);

			ShowSearchBoxCommand = new ActionCommand(
				o =>
				{
					MainWindow.SearchMenu.Show();
					MainWindow.SearchMenu.IsActive = true;
					MainWindow.Dispatcher.BeginInvoke(new Action(() => { ((SearchMenu)MainWindow.SearchMenu.Content).SearchTextBox.Focus(); }), System.Windows.Threading.DispatcherPriority.Render);
				}
				);

			ExportNarCommand = new ActionCommand(
				o =>
				{
					//保存ダイアログを出す
					if(AskSave(true))
					{
						//ファイル保存先の選択
						//TODO: developer_options.txt を実装してのテストはできてないので要確認
						var saveDialog = new SaveFileDialog();
						saveDialog.Filter = "narゴーストアーカイブ(*.nar)|*.nar|zip圧縮ファイル(*.zip)|*.zip|すべてのファイル|*.*";
						saveDialog.InitialDirectory = DictionaryUtility.NormalizeWindowsPath(Ghost.FullPath);
						saveDialog.AddExtension = true;
						saveDialog.OverwritePrompt = true;
						saveDialog.FileName = "ghost.nar";

						if(saveDialog.ShowDialog() == true)
						{
							var progressDialog = new ProgressDialog(this);
							progressDialog.DataContext.Title = "narファイルの作成";
							progressDialog.DataContext.SetMessage("narファイルを作成します。");

							var task = Task.Run(() =>
							{
								try
								{
									NarUtility.CreateNar(Ghost.FullPath, saveDialog.FileName);

									MainWindow.Dispatcher.Invoke(() =>
									{
										progressDialog.DataContext.SetMessage("作成完了しました。", 100.0);
									});
								}
								catch
								{
									//TODO: エラー内容表示？ 折角ログ領域ある
									MainWindow.Dispatcher.Invoke(() =>
									{
										progressDialog.DataContext.SetMessage("失敗しました。");
									});
								}
							});
							progressDialog.SetTask(task);
							progressDialog.ShowDialog();
							
						}
					}
				},
				o => Ghost != null
				);

			MakeUpdateFileCommand = new ActionCommand(
				o =>
				{
					if(AskSave(true))
					{
						var progressDialog = new ProgressDialog(this);
						progressDialog.DataContext.Title = "更新ファイルの作成";
						progressDialog.DataContext.SetMessage("更新ファイルを作成します。");
						var task = Task.Run(() =>
					   {
						   try
						   {
							   NarUtility.CreateUpdateFile(Ghost.FullPath);
							   MainWindow.Dispatcher.Invoke(() =>
							   {
								   progressDialog.DataContext.SetMessage("作成完了しました。", 100.0);
							   });
						   }
						   catch
						   {
							   MainWindow.Dispatcher.Invoke(() =>
							   {
								   progressDialog.DataContext.SetMessage("失敗しました。");
							   });
						   }
					   });
						progressDialog.SetTask(task);
						progressDialog.ShowDialog();
					}
				},
				o => Ghost != null
				);

			UploadGhostCommand = new ActionCommand(
				o =>
				{
					if(!(EditorSettings.UploadSettings?.Any() ?? false))
					{
						MessageBox.Show("アップロード設定がありません。\r\n「設定」→「アップロード設定」でアップロード設定を構成してください。", "さとりすと");
						return;
					}

					if (AskSave(true))
					{
						//ゴーストアップロード
						var dialog = new UploadDialog(EditorSettings.UploadSettings, this, EditorSettings.GhostTemporarySettings);
						dialog.ShowDialog();
						if(dialog.IsUploadStarted)
						{
							//アップロードを開始した場合は、設定を更新する
							EditorSettings.GhostTemporarySettings.LastUploadSettingId = dialog.DataContext.SelectedSettingId;
							EditorSettings.GhostTemporarySettings.LastUploadUseDiff = dialog.DataContext.IsDiffUpload;
							EditorSettings.GhostTemporarySettings.LastUploadUseFiles = dialog.DataContext.IsUploadFiles;
							EditorSettings.GhostTemporarySettings.LastUploadUseNar = dialog.DataContext.IsUploadNar;

							//保存。どこかでまとめてやる？
							EditorSettings.SaveGhostTemporarySettings(Ghost);
						}

					}
				},
				o => Ghost != null
				);

			GenerateSurfacePreviewCommand = new ActionCommand(
				o =>
				{
					//ゴーストの起動をチェック
					/*
					if(!SakuraFMOReader.Exists(Ghost))
					{
						MessageBox.Show("SSPで起動中のゴーストからサーフェスプレビューを作る機能です。\r\nゴーストを起動した状態でこの機能を使用して下さい。");
						return;
					}
					*/

					//編集中のゴーストが起動していたら、シェルをリロードする。きせかえの状態を吐かせるため
					if (Ghost != null)
					{
						if (SakuraFMOReader.Exists(Ghost))
						{
							Satorite.SendSSTP(Ghost, @"\![reload,shell]\m[{0},{1},{2}]", true, true);
							LogMessage.AddLog("起動中のゴーストの状態をプレビューに反映するため、動作中のシェルをリロードしました。");
						}
					}

					var gen = new Core.SurfacePreviewImageGenerator();
					var dialog = new ProgressDialog(this);
					dialog.DataContext.Title = "サーフェスプレビューの作成中";
					dialog.DataContext.StableMessage = "SSPと通信しています。処理中はゴーストを操作しないでください。";

					var task = gen.GenerateShellOnly(this, SurfacePreview.SelectedShellPath, (success) =>
					{
						dialog.DataContext.Progress = 100.0;
						if (success)
						{
							dialog.DataContext.SetMessage("完了しました。");
						}
					},
					(progress) =>
					{

						dialog.DataContext.SetProgress(progress);

					}, dialog.Cancellation.Token);

					dialog.SetTask(task, true);
					dialog.ShowDialog();

					//再読み込み
					SurfacePreview.ReloadPreviewData();
					SurfacePaletteViewModel.UpdateSurfacePreviewData();
					SurfaceViewerViewModel.UpdateSurfacePreviewData();
				},
				o => SurfacePreview.SelectedShell != null
				);

			SelectPreviewShellCommand = new ActionCommand(
				o =>
				{
					//選択変更
					SurfacePreview.SelectedShell = (SurfacePreviewViewModel.SurfacePreviewViewModelShellItem)o;

					//再読み込み
					SurfacePaletteViewModel.UpdateSurfacePreviewData();
					SurfaceViewerViewModel.UpdateSurfacePreviewData();
					GenerateSurfacePreviewCommand.NotifyCanExecuteChanged();
				}
				);

			SelectPreviewShellDirectoryCommand = new ActionCommand(
				o =>
				{
					//選択
					var result = SurfacePreview.SelectShellFromPath();
					if (!result)
						MessageBox.Show("シェルを開けませんでした。", "さとりすと");

					//再読み込み
					SurfacePaletteViewModel.UpdateSurfacePreviewData();
					SurfaceViewerViewModel.UpdateSurfacePreviewData();
					GenerateSurfacePreviewCommand.NotifyCanExecuteChanged();
				}
				);

			ShowLogMessageCommand = new ActionCommand(
				o =>
				{
					if(mainWindow.LogList.Items.Count > 0)
						mainWindow.LogList.ScrollIntoView(mainWindow.LogList.Items[mainWindow.LogList.Items.Count - 1]);
					mainWindow.LogPopup.IsOpen = true;
				}
				);

			ClearLogMessageCommand = new ActionCommand(
				o =>
				{
					LogMessage.ClearLog();
				}
				);

			NetworkUpdateCommand = new ActionCommand(
				o =>
				{
					BootNetworkUpdator();
				}
				);

			//読込エラーが発生している場合に通知
			List<ErrorListDialogItemViewModel> errorItems = new List<ErrorListDialogItemViewModel>();
			foreach (var err in SaveLoadPanes.Where(o => o.LoadState == EditorLoadState.LoadFailed))
			{
				var item = new ErrorListDialogItemViewModel()
				{
					Title = err.SaveFilePath,
					Description = "ファイルの読込または解析に失敗しました。"
				};
				errorItems.Add(item);
			}

			//エラーダイアログ表示
			if(errorItems.Count > 0)
			{
				//エラーダイアログの表示
				MainWindow.Dispatcher.BeginInvoke(
					new Action(() =>
					{
						var errDialog = new ErrorListDialog(this, false);
						errDialog.DataContext.Title = "ゴースト読込エラー";
						errDialog.DataContext.Description = "ゴーストの一部ファイルのロードに失敗しています。\r\n上書き保存でデータが消える可能性もあるので、一度さとりすとを閉じてファイルの状態を確認してからゴーストを開き直すことをおすすめします。";
						errDialog.DataContext.SetErrorItems(errorItems);
						errDialog.ShowDialog();
					}
					));
			}
		}


		//イベントエディタのオープン
		public void OpenEventEditor(EventModel ev)
		{
			MainWindow.OpenEventEditor(ev);
		}

		public void OpenEventEditor(InlineEventModel ev)
		{
			var eventEditor = MainWindow.OpenEventEditor(ev.ParentEvent);
			if (eventEditor.DataContext is EventEditorViewModel vm)
			{
				vm.MoveCaretToLine(ev.InlineEvent.AnalyzeLineIndex);
			}
		}

		//テキストエディタのオープン
		public void OpenTextEditor(TextFileModel textFile)
		{
			MainWindow.OpenTextEditor(textFile);
		}

		//テキストエディタのオープン、必要な位置へカレットを移動
		public void OpenTextEditor(TextFileModel textFile, int moveCaretLine)
		{
			var textEditor = MainWindow.OpenTextEditor(textFile);
			if(textEditor.DataContext is TextEditorViewModel vm)
			{
				vm.MoveCaretToLine(moveCaretLine);
			}
		}

		//テンポラリ用途のテキストエディタのオープン
		public void OpenTemporaryTextEditor(string body, string title)
		{
			MainWindow.OpenTemporaryTextEditor(body, title);
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

		//保存ダイアログを呼ぶ
		public bool AskSave(bool isAutomaticSaveDialog)
		{
			//ゴーストを開いてない
			if (Ghost == null)
				return true;

			if (isAutomaticSaveDialog)
			{
				//保存が不要
				if (!IsChanged)
					return true;
			}

			List<ISaveFileObject> objects = new List<ISaveFileObject>();
			objects.AddRange(SaveLoadPanes);
			if(Ghost != null)
				objects.AddRange(Ghost.Dictionaries);

			var dialog = new SaveFileListDialog(this);
			var dialogViewModel = new SaveFileListViewModel(objects);
			dialogViewModel.IsEnableDiscard = isAutomaticSaveDialog;
			dialog.DataContext = dialogViewModel;
			dialog.ShowDialog();

			if(dialog.Result == MessageBoxResult.Yes)
			{
				//現時点ではエラーはダイアログに表示するだけにしておく
				var errorList = new List<ErrorListDialogItemViewModel>();

				//保存
				foreach( var saveItem in dialogViewModel.Items.Where(o => o.IsSave))
				{
					bool success = saveItem.SaveItem.Save();
					if (!success)
					{
						var error = new ErrorListDialogItemViewModel();
						error.Description = "保存に失敗しました。";
						error.Title = saveItem.SaveFilePath;
						errorList.Add(error);
					}
				}

				if(errorList.Count > 0)
				{
					var errDialog = new ErrorListDialog(this, false);
					errDialog.DataContext.Description = "保存に失敗したファイルがあります。";
					errDialog.DataContext.Title = "保存";
					errDialog.DataContext.SetErrorItems(errorList);
					errDialog.ShowDialog();
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

		//破棄の確認。保存するのではなく破棄だけ。
		public bool AskDiscard()
		{
			//変更点がない
			if (!IsChanged)
				return true;

			var result = MessageBox.Show("変更点を破棄してゴーストを読み込み直してもよろしいですか？", "さとりすと", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
				return true;
			return false;
		}

		//アクティブなエディタに挿入
		public void InsertToActiveEditor(string str, bool isActivate = true)
		{
			if(MainWindow.ActiveTextEditor != null)
			{
				MainWindow.ActiveTextEditor.Document.Insert(MainWindow.ActiveTextEditor.CaretOffset, str);

				if(isActivate)
				{
					MainWindow.ActivateActiveEditor();
				}
			}
		}

		private void CheckNetworkUpdate(bool acceptReleaseBuild)
		{
#if DEPLOY || true
			Task.Run(() =>
			{
				var release = UpdateChecker.GetRelease(acceptReleaseBuild);
				if(release != null)
				{
					updateInfo = release;

					//githubにあるほうが自分よりあたらしければアップデートを通知する
					if (release.VersionIndex > Version.VersionIndex)
					{
						MainWindow.Dispatcher.BeginInvoke(new Action(() =>
					  {
						  IsUpdateAvailable = true;
						  UpdateVersionLabel = release.Label;
					  }
						));
					}
				}
			}
			);
#endif

		}

		//ネットワーク更新を行う
		private void BootNetworkUpdator()
		{
			try
			{
				//さとりすとのアップデータをテンポラリに切り離して隔離
				string temporaryUpdatorPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Satolist2Updator");
				System.IO.Directory.CreateDirectory(temporaryUpdatorPath);

				//更新に必要なファイル
				string[] updatorFiles =
				{
					"SatolistUpdator.exe"
				};

				foreach (var f in updatorFiles)
				{
					System.IO.File.Copy(f, System.IO.Path.Combine(temporaryUpdatorPath, f), true);
				}

				//現在の実行ファイル名を確認
				var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

				//アップデータを起動
				IsUpdateAvailable = false;  //多重実行できないように
				ProcessStartInfo info = new ProcessStartInfo();
				info.FileName = System.IO.Path.Combine(temporaryUpdatorPath, "SatolistUpdator.exe");
				info.Arguments = string.Format(@"""{0}"" ""{1}""", executablePath, updateInfo.ZipURL);
				info.WorkingDirectory = Environment.CurrentDirectory;
				Process.Start(info);
			}
			catch { }
		}

	}

	//サーフェスプレビュー共通ビューモデル
	internal class SurfacePreviewViewModel : NotificationObject
	{
		//サーフェスシェルフォルダ列挙とか
		public MainViewModel Main { get; }
		private Core.SurfacePreviewMetaData surfacePreviewData;
		private ObservableCollection<SurfacePreviewViewModelShellItem> shells;
		private SurfacePreviewViewModelShellItem selectedShell;
		private ShellImageCache imageCache;

		public ReadOnlyObservableCollection<SurfacePreviewViewModelShellItem> Shells
		{
			get => new ReadOnlyObservableCollection<SurfacePreviewViewModelShellItem>(shells);
		}

		//シェル読込用のキャッシュオブジェクト
		public ShellImageCache ImageCache => imageCache;

		public SurfacePreviewViewModelShellItem SelectedShell
		{
			get => selectedShell;
			set
			{
				if (selectedShell != value)
				{
					selectedShell = value;
					NotifyChanged();
					NotifyChanged(nameof(IsShellEnable));
					LoadSurfacePreviewData(selectedShell);

					foreach(var s in shells)
					{
						s.IsSelected = s == selectedShell;
					}

					if (!SelectedShell.IsTemporary)
					{
						//選択を保存
						MainViewModel.EditorSettings.GhostTemporarySettings.SurfacePreviewShellDirectory = selectedShell.DirectoryName;
						MainViewModel.EditorSettings.SaveGhostTemporarySettings(Main.Ghost);
					}
				}
			}
		}

		public string SelectedShellPath
		{
			get
			{
				if(SelectedShell !=null)
				{
					return SelectedShell.DirectoryFullPath;
				}
				return null;
			}
		}

		//サーフェスプレビューデータ
		public Core.SurfacePreviewMetaData SurfacePreviewData
		{
			get => surfacePreviewData;
			set
			{
				surfacePreviewData = value;
				NotifyChanged();
			}
		}

		//シェルが存在してるか
		public bool IsExistsShell
		{
			get => shells.Count > 0;
		}

		//シェルが選択されているか
		public bool IsShellEnable
		{
			get => selectedShell != null;
		}

		public SurfacePreviewViewModel(MainViewModel main)
		{
			Main = main;
			shells = new ObservableCollection<SurfacePreviewViewModelShellItem>();
		}

		public void LoadShells()
		{
			var shellDirectoryPath = DictionaryUtility.ConbinePath(Main.Ghost.FullPath, "shell");
			if (!System.IO.Directory.Exists(shellDirectoryPath))
			{
				//shellが存在していない
				return;
			}

			var shellDirs = System.IO.Directory.GetDirectories(shellDirectoryPath);

			foreach (var dir in shellDirs)
			{
				var s = AnalyzeShellDirectory(dir);
				if (s != null)
					shells.Add(s);
			}

			//アイテム選択
			SelectedShell = Shells.FirstOrDefault(o => o.DirectoryName == MainViewModel.EditorSettings.GhostTemporarySettings.SurfacePreviewShellDirectory);
			if (SelectedShell == null)
				SelectedShell = Shells.FirstOrDefault(o => o.DirectoryName == "master");
			if (SelectedShell == null)
				SelectedShell = Shells.FirstOrDefault();
		}

		public SurfacePreviewViewModelShellItem AnalyzeShellDirectory(string path)
		{
			var nDir = DictionaryUtility.NormalizePath(path);
			var descriptPath = DictionaryUtility.ConbinePath(nDir, "descript.txt");
			if (System.IO.File.Exists(descriptPath))
			{
				try
				{
					var parser = new CsvBuilder();
					parser.Deserialize(System.IO.File.ReadAllText(descriptPath, Constants.EncodingShiftJis));

					var type = parser.GetValue("type");
					var name = parser.GetValue("name");

					if (type == "shell" && !string.IsNullOrEmpty(name))
					{
						var s = new SurfacePreviewViewModelShellItem();
						s.DirectoryName = System.IO.Path.GetFileName(path);
						s.DirectoryFullPath = nDir;
						s.ShellName = name;
						return s;
					}
				}
				catch { }
			}
			return null;
		}

		//ダイアログを表示してシェルを選択
		public bool SelectShellFromPath()
		{
			var dialog = new CommonOpenFileDialog();
			dialog.IsFolderPicker = true;

			if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
			{
				var shell = AnalyzeShellDirectory(dialog.FileName);
				if (shell == null)
					return false;

				shell.IsTemporary = true;	//一時的な選択としてゴーストローカルデータには反映しない
				SelectedShell = shell;
			}
			return true;
		}

		public void ReloadPreviewData()
		{
			LoadSurfacePreviewData(SelectedShell);
		}

		private void LoadSurfacePreviewData(SurfacePreviewViewModelShellItem item)
		{
			if (item != null)
			{
				imageCache = new ShellImageCache(DictionaryUtility.ConbinePath(item.DirectoryFullPath, SurfacePreviewMetaData.SurfacePreviewPath));
				var metadataPath = DictionaryUtility.ConbinePath(item.DirectoryFullPath, SurfacePreviewMetaData.SurfacePreviewPath, SurfacePreviewMetaData.SurfacePreviewMetadataPath);
				try
				{
					SurfacePreviewData = JsonUtility.DeserializeFromFile<Core.SurfacePreviewMetaData>(metadataPath);
				}
				catch
				{
					SurfacePreviewData = null;
				}
			}
			else
			{
				imageCache = null;
				SurfacePreviewData = null;
			}
		}

		

		internal class SurfacePreviewViewModelShellItem : NotificationObject
		{
			private bool isSelected;

			public string DirectoryName { get; set; }
			public string DirectoryFullPath { get; set; }
			public string ShellName { get; set; }
			public bool IsTemporary { get; set; }

			public bool IsSelected
			{
				get => isSelected;
				set
				{
					isSelected = value;
					NotifyChanged();
				}
			}
		}
	}


	
}
