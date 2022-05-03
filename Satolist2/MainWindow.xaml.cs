using AvalonDock.Controls;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Microsoft.Win32;
using Satolist2.Control;
using Satolist2.Dialog;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			DeserializeLayout(MainViewModel.EditorSettings.TemporarySettings.SerializedDockingLayout);

			//DocumentPane探す
			DocumentPane = FindDocumentPane(DockingManager.Layout);

			//スタートメニューは自動で閉じるので復活する
			StartMenu.Show();

			//さとりすとのSSTP受信用ウインドウを起動
			Closed += MainWindow_Closed;
			Dispatcher.BeginInvoke(new Action(() =>
			{
				Core.SSTPCallBackNativeWindow.Create((new System.Windows.Interop.WindowInteropHelper(this)).Handle);
			}), System.Windows.Threading.DispatcherPriority.Render);
			

#if DEPLOY
			//公開時はデバッグメニューを封じておく。今のところ根本に消すわけではないけど
			DebugMainMenuVisibleMenu.Visibility = Visibility.Collapsed;
			DebugMainMenuVisibleMenu.IsEnabled = false;
			DebugMainMenu.Hide();
#endif
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
				var newWindow = new DockingWindow(new EventEditor(), new EventEditorViewModel(mainViewModel, ev));
				newWindow.CanClose = true;
				newWindow.CanHide = false;

				//閉じたときのイベントを設定
				newWindow.Closing += EventEditorClosing;
				ev.OnRemove += OpendEventRemoved;

				EventEditors.Add(newWindow);
				DocumentPane.Children.Add(newWindow);
				currentWindow = newWindow;
			}

			//アクティベート
			currentWindow.IsActive = true;
			var editor = (EventEditor)currentWindow.Content;
			editor.RequestFocus();
			return editor;
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

		internal TextEditor OpenTextEditor(TextFileModel text)
		{
			if (!text.BodyAvailable)
				throw new Exception();  //多分リストモードのファイルを開こうとしてる

			var currentWindow = TextEditors.FirstOrDefault(o => ((TextEditorViewModel)o.ViewModel).TextFile == text);
			if(currentWindow == null)
			{
				var newWindow = new DockingWindow(new TextEditor(), new TextEditorViewModel(mainViewModel, text));
				newWindow.CanClose = true;
				newWindow.CanHide = false;
				newWindow.Closing += TextEditorClosing;
				text.OnDelete += TextFileDeleted;

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
				if(window.ViewModel is TextEditorViewModel vm)
				{
					vm.TextFile.OnDelete -= TextFileDeleted;
				}
			}
		}

		internal TemporaryTextEditor OpenTemporaryTextEditor(string body, string title)
		{
			var editor = new TemporaryTextEditor();
			var newWindow = new DockingWindow(editor, new TemporaryTextEditorViewModel()
			{
				Title = title,
				Text = body
			});
			newWindow.CanClose = true;
			newWindow.CanHide = false;

			DocumentPane.Children.Add(newWindow);

			//アクティベート
			newWindow.IsActive = true;
			editor.RequestFocus();
			return editor;
		}

		internal void OpenGhost(string ghostPath, string shellDirectoryName = "master", string executablePath = null)
		{
			//ゴーストのロード
			var ghost = new GhostModel(ghostPath);

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

			//開けたらスタートメニューを閉じる
			StartMenu.Hide();
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
				if(!vm.AskSave())
				{
					e.Cancel = true;
					return;
				}
			}

			//ウインドウを閉じるときに、複製する
			MainViewModel.EditorSettings.TemporarySettings.SerializedDockingLayout = SerializeDockingLayout();
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
		public static EditorSettings EditorSettings { get; private set; }

		public MainWindow MainWindow { get; }
		public GhostModel Ghost { get; private set; }

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
		

		public List<EventEditorViewModel> EventEditors { get; }
		public SatoriConfWrapper SatoriConfViewModel { get; }

		//汎用コマンド
		public ActionCommand SaveFileCommand { get; }
		public ActionCommand ReopenGhostCommand { get; }
		public ActionCommand AddSatoriDictionaryFileCommand { get; }
		public ActionCommand AddTextFileCommand { get; }
		public ActionCommand OpenGhostDirectoryCommand { get; }
		public ActionCommand BootSSPCommand { get; }
		public ActionCommand NewGhostCommand { get; }
		public ActionCommand EditGeneralSettingsCommand { get; }
		public ActionCommand EditInsertPaletteCommand { get; }
		public ActionCommand EditUploadSettingCommand { get; }
		public ActionCommand OpenSatolistDirectoryCommand { get; }
		public ActionCommand ReloadShioriCommand { get; }
		public ActionCommand SurfacePreviewTest { get; }
		public ActionCommand ExportNarCommand { get; }
		public ActionCommand MakeUpdateFileCommand { get; }
		public ActionCommand UploadGhostCommand { get; }

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

		public MainViewModel(MainWindow mainWindow, MainViewModelInitializeData initializeData = null)
		{
			initializeData = initializeData ?? new MainViewModelInitializeData();
			MainWindow = mainWindow;
			Ghost = initializeData.Ghost;
			string shellPath = null;

			//エディタ設定のロード
			if(EditorSettings == null)
			{
				EditorSettings = new EditorSettings();
				//同じタイミングでリストデータ周辺をロード
				DataModelManager.Load();
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

				if (!string.IsNullOrEmpty(initializeData.ShellDirectoryName))
				{
					shellPath = Ghost.FullPath + "/shell/" + initializeData.ShellDirectoryName;
				}
			}

			EventEditors = new List<EventEditorViewModel>();
			FileEventTreeViewModel = new FileEventTreeViewModel(this);
			EventListViewModel = new EventListViewModel(this);
			SearchMenuViewModel = new SearchMenuViewModel(this);
			SearchResultViewModel = new SearchResultViewModel(this);
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
			RecvEventLogViewModel = new RecvEventLogViewModel(this);

			SaveFileCommand = new ActionCommand(
				o => AskSave(),
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
						Process.Start(Ghost.FullDictionaryPath);
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
					var dialog = new NewGhostDialog();
					if( dialog.ShowDialog() == true)
					{
						//ゴーストの作成が実際に行われたのでそれを開く
						MainWindow.OpenGhost(dialog.DataContext.CreatePath);
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
					}
				}
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

			EditUploadSettingCommand = new ActionCommand(
				o =>
				{
					var d = new Dialog.UploadSettingDialog(EditorSettings.UploadSettings);
					if( d.ShowDialog() == true)
					{
						EditorSettings.UploadSettings = d.DataContext.GetItems();
						EditorSettings.SaveUploadSettings();
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

			SurfacePreviewTest = new ActionCommand(
				o =>
				{
					var gen = new Core.SurfacePreviewImageGenerator();
					gen.Generate(null, this);
				},
				o => Ghost != null
				);

			ExportNarCommand = new ActionCommand(
				o =>
				{
					//保存ダイアログを出す
					if(AskSave())
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
							var progressDialog = new ProgressDialog();
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
					if(AskSave())
					{
						var progressDialog = new ProgressDialog();
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

					if (AskSave())
					{
						//ゴーストアップロード
						var dialog = new UploadDialog(EditorSettings.UploadSettings, Ghost, EditorSettings.GhostTemporarySettings);
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
		public bool AskSave()
		{
			//ゴーストを開いてない
			if (Ghost == null)
				return true;

			//保存が不要
			if (!IsChanged)
				return true;

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


	}
}
