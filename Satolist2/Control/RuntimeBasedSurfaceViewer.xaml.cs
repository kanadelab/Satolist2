using Microsoft.VisualBasic;
using Microsoft.Xaml.Behaviors.Media;
using Satolist2.Core;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.ServiceModel.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Satolist2.SurfacePreviewViewModel;

namespace Satolist2.Control
{
	/// <summary>
	/// RuntimeBasedSurfaceViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class RuntimeBasedSurfaceViewer : UserControl
	{
		internal new RuntimeBasedSurfaceViewerViewModel DataContext
		{
			get => (RuntimeBasedSurfaceViewerViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public RuntimeBasedSurfaceViewer()
		{
			InitializeComponent();
		}

		private void FormsHostGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if(DataContext is RuntimeBasedSurfaceViewerViewModel vm)
			{
				vm.PreviewWindowSizeChanged();
			}
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//ダブルクリックでスクリプト挿入
			if (sender is ListViewItem listViewItem)
			{
				SurfacePaletteViewModel.InsertSurfaceToActiveEditor(((RuntimeBasedSurfaceViewerItemViewModel)listViewItem.DataContext).Id);
			}
			e.Handled = true;
		}
	}

	//viewmodel
	internal class RuntimeBasedSurfaceViewerViewModel : NotificationObject, IDockingWindowContent, IDockingWindowActive, IControlBindedReceiver, IDisposable
	{
		public enum CollisionType
		{
			Invalid,
			Rect,
			Ellipse,
			Circle,         //未対応。Ellipseで代用できるはずなので
			Polygon
		}

		public const string ContentId = "RuntimeBasedSurfaceViewer";
		public const int ScriptExecutedMessage = 0x0401;
		public const int MetadataGeneratedMessage = 0x0402;

		private bool isDockingWindowActive;

		//各スコープウインドウの情報
		private Dictionary<int, WindowItem> windowItems;

		//現在スコープ
		private int currentScope;

		private TemporaryGhostRuntimeEx runtime;
		private SakuraFMORecord runtimeGhostFMORecord;

		private bool isPreviewDataEnable;
		private RuntimeBasedSurfacePreviewMetaData previewData;

		//シェル情報
		private ICollectionView surfaceList;
		private RuntimeBasedSurfaceViewerItemViewModel selectedSurface;
		private long generatingSurfaceId;
		private RuntimeBasedSurfaceViewerItemViewModel[] items;

		//きせかえ情報
		private ICollectionView bindList;
		private RuntimeBasedSurfaceViewerBindCategoryViewModel[] bindItems;

		private HashSet<int> captureScopes;

		private bool isRuntimeExited;
		private bool isRuntimeBooting;
		private bool isRuntimeAvailable;
		private bool isMakeCollisionMode;
		private bool showCollision;
		private bool showBindList;

		private SSTPCallBackNativeWindow callbackWindow;
		private CancellationTokenSource runtimeBootCanceller;
		private Task runtimeBootTask;

		private Bitmap surfaceBitmapForMakeCollision;

		public RuntimeBasedSurfaceViewer Control { get; private set; }
		public MainViewModel Main { get; private set; }
		public CollisionEditorViewModel CollisionEditorViewModel { get; }
		public ActionCommand RuntimeRebootCommand { get; }
		public ActionCommand EnableSurfaceViewerCommand { get; }
		public ActionCommand ReloadShellCommand { get; }

		public double CurrentScale { get; private set; }

		public string DockingTitle => "サーフェスビューワv3(仮)";
		public string DockingContentId => ContentId;
		public bool IsDockingWindowActive
		{
			get => isDockingWindowActive;
			set
			{
				isDockingWindowActive = value;
				NotifyChanged();
			}
		}

		//サーフェスの基準サイズ
		public System.Drawing.Size SelectedSurfaceBaseSize
		{
			get
			{
				if (selectedSurface == null)
					return default;
				if (windowItems.TryGetValue(selectedSurface.Scope, out var windowItem))
					return windowItem.CurrentSurfaceSizeData?.ZeroOriginSize ?? default;
				else
					return default;
			}
		}

		//プレビューデータが有効かどうか
		public bool IsPreviewDataEnable
		{
			get => isPreviewDataEnable;
			private set
			{
				isPreviewDataEnable = value;
				NotifyChanged();
			}
		}

		//サーフェス一覧のビュー
		public ICollectionView SurfaceList
		{
			get => surfaceList;
			private set
			{
				surfaceList = value;
				NotifyChanged();
			}
		}

		//きせかえ一覧のビュー
		public ICollectionView BindList
		{
			get => bindList;
			private set
			{
				bindList = value;
				NotifyChanged();
			}
		}

		//選択中のサーフェス
		public RuntimeBasedSurfaceViewerItemViewModel SelectedSurface
		{
			get => selectedSurface;
			set
			{
				if (selectedSurface != value)
				{
					selectedSurface = value;
					NotifyChangeSurface();
					NotifyChanged(nameof(SelectedSurfaceBaseSize));
				}
			}
		}

		//ゴーストが有効かどうか(ゴーストを読み込まずにシェルだけとかだとfalse)
		public bool IsGhostEnabled
		{
			get => Main.Ghost != null;
		}

		//ゴーストでサーフェスビューワが有効かどうか
		public bool IsEnabled
		{
			get
			{
				if (IsGhostEnabled)
					return MainViewModel.EditorSettings.GhostTemporarySettings.IsRuntimeBasedSurfaceViewerEnabled;
				else
					return true;	//ゴースト単体の場合は必ず有効
			}
				
			set
			{
				if(!IsGhostEnabled)
				{
					return;
				}

				if (IsEnabled != value)
				{
					MainViewModel.EditorSettings.GhostTemporarySettings.IsRuntimeBasedSurfaceViewerEnabled = value;
					MainViewModel.EditorSettings.SaveGhostTemporarySettings(Main.Ghost);
					NotifyChanged();
					UpdateSurfacePreviewData();
				}
			}
		}

		//SSPの起動処理中かどうか
		public bool IsRuntimeBooting
		{
			get => isRuntimeBooting;
			set
			{
				isRuntimeBooting = value;
				NotifyChanged();
			}
		}

		//SSPが起動完了したか
		public bool IsRuntimeAvailable
		{
			get => isRuntimeAvailable;
			set
			{
				isRuntimeAvailable = value;
				NotifyChanged();
				NotifyChanged(nameof(IsRuntimeVisible));
			}
		}

		//SSPが起動後停止したか
		public bool IsRuntimeExited
		{
			get => isRuntimeExited;
			set
			{
				isRuntimeExited = value;
				NotifyChanged();
			}
		}

		//コリジョン作成(静止画)モード
		public bool IsMakeCollisionMode
		{
			get => isMakeCollisionMode;
			set
			{
				if (isMakeCollisionMode != value)
				{
					isMakeCollisionMode = value;
					NotifyChanged();
					NotifyChanged(nameof(IsRuntimeMode));
					NotifyChanged(nameof(IsRuntimeVisible));

					//表示を切り替え
					SyncToRuntime();
				}
			}
		}

		//きせかえリストの表示有無
		public bool ShowBindList
		{
			get => showBindList;
			set
			{
				showBindList = value;
				NotifyChanged();
			}
		}

		//ランタイム表示モード
		public bool IsRuntimeMode
		{
			get => !isMakeCollisionMode;
		}

		//ランタイムを表示するか
		public bool IsRuntimeVisible
		{
			get => IsRuntimeMode && IsRuntimeAvailable;
		}

		//触り判定領域の表示
		public bool ShowCollision
		{
			get => showCollision;
			set
			{
				if(showCollision != value)
				{
					showCollision = value;
					NotifyChanged();
					SyncToRuntime();	//sspに同期
				}
			}
		}

		//判定作成用の静止画
		public Bitmap SurfaceBitmapForMakeCollision
		{
			get => surfaceBitmapForMakeCollision;
			private set
			{
				surfaceBitmapForMakeCollision = value;
				NotifyChanged();
			}
		}

		public RuntimeBasedSurfaceViewerViewModel(MainViewModel main)
		{
			Main = main;
			CollisionEditorViewModel = new CollisionEditorViewModel(main);

			windowItems = new Dictionary<int, WindowItem>();
			showBindList = false;
			currentScope = -1;
			CurrentScale = 1.0;
			captureScopes = null;

			RuntimeRebootCommand = new ActionCommand(
				o =>
				{
					UpdateSurfacePreviewData();
				});

			EnableSurfaceViewerCommand = new ActionCommand(
				o =>
				{
					IsEnabled = true;
				});

			ReloadShellCommand = new ActionCommand(
				o =>
				{
					//ランタイムを再起動せずにきれいにシェルだけさくらスクリプトベースでリロードできるとよい
					//surfaces.txt, deascript.txt のリロード、スコープが増えたりを考える必要があるので一旦全部再起動でいく
					Main.SurfacePreview.ReloadRuntimeBasedSurfaceViewerData();
					UpdateSurfacePreviewData();
				});
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is RuntimeBasedSurfaceViewer ctrl)
			{
				Control = ctrl;
				CollisionEditorViewModel.Control = ctrl.CollisionEditor;

				if (Main.Ghost != null)
				{
					UpdateSurfacePreviewData();
				}
			}
		}

		public void UpdateSurfacePreviewData()
		{
#if SURFACE_VIEWER_V3

			//起動中の処理があればキャンセル
			runtimeBootCanceller?.Cancel();
			runtimeBootCanceller?.Dispose();
			runtimeBootTask?.Wait();
			runtimeBootCanceller = new CancellationTokenSource();

			//ランタイムをクローズ
			if (runtime != null)
			{
				runtime.Exited -= Runtime_Exited;
				runtime.Dispose();
				runtime = null;
			}

			//初期化
			foreach (var item in windowItems)
			{
				item.Value.Dispose();
			}
			windowItems.Clear();
			SelectedSurface = null;
			IsRuntimeAvailable = false;
			IsRuntimeBooting = false;
			IsRuntimeExited = false;

			if (IsEnabled && Main.SurfacePreview.RuntimeBasedSurfacePreviewData != null)
			{
				//ネイティブコールバックを準備
				if (callbackWindow == null)
				{
					callbackWindow = new SSTPCallBackNativeWindow(Main.MainWindow.HWnd);
					callbackWindow.RegisterCallback(ScriptExecutedMessage, OnScriptExecuted);
					callbackWindow.RegisterCallback(MetadataGeneratedMessage, OnMetadataGenerated);
				}

				IsPreviewDataEnable = true;
				previewData = Main.SurfacePreview.RuntimeBasedSurfacePreviewData;
				items = previewData.Surfaces.Records.Select(o => new RuntimeBasedSurfaceViewerItemViewModel(this, o.Key, o.Value)).ToArray();

				//定義されているスコープを列挙、sakuraとkeroは必ずキャプチャする
				captureScopes = new HashSet<int>(previewData.Descript.Model.GetCommonScopes());
				captureScopes.Add(0);
				captureScopes.Add(1);

				SurfaceList = CollectionViewSource.GetDefaultView(items);
				SurfaceList.Filter = (o) =>
				{
					if(o is RuntimeBasedSurfaceViewerItemViewModel model)
					{
						return model.IsViewerVisible && !model.Model.IsImageFileOnly;
					}
					return false;
				};
				SurfaceList.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

				NotifyChangeScope();

				//有効なシェルが指定されてる場合にランタイムを起動する
				if (Directory.Exists(Main.SurfacePreview.SelectedShellPath))
				{
					IsRuntimeBooting = true;
					var cancelToken = runtimeBootCanceller.Token;

					var runtimeBootTask = Task.Run(() =>
					{
						try
						{
							//Sakura FMOを汚染しないようにGUIDでFMO名を決定して使用する
							runtime = TemporaryGhostRuntimeEx.PrepareShell(Main.SurfacePreview.SelectedShellPath, Guid.NewGuid().ToString());
							runtime.Exited += Runtime_Exited;
							runtime.Boot();

							//FMOの構築待ち
							while (true)
							{
								Thread.Sleep(100);
								cancelToken.ThrowIfCancellationRequested();
								runtimeGhostFMORecord = SakuraFMOReader.Read(runtime.Ghost, runtime.FMOName);
								if (runtimeGhostFMORecord != null)
								{
									if (runtimeGhostFMORecord.HWndList.Length >= captureScopes.Count)
									{
										break;
									}

									//SSPは起動してるが、シェル設定に対してウインドウが足りてない場合はスクリプトを実行して生成を促す
									MakeWindowRequestToRuntime(captureScopes);
								}
							}

							//メインスレッドで表示更新系
							Control.Dispatcher.Invoke(new Action(() =>
							{
								var sortedScopes = new List<int>(captureScopes);
								sortedScopes.Sort();

								//HWndListにスコープ順にハンドルが並んでるはず
								for (int i = 0; i < sortedScopes.Count; i++)
								{
									var scope = sortedScopes[i];
									var item = new WindowItem(Control.FormsHostGrid, runtimeGhostFMORecord.HWndList[i], this);
									windowItems.Add(scope, item);
									item.Visibility = Visibility.Collapsed;

									//非表示を要求
									Satorite.SendSSTP(runtimeGhostFMORecord, string.Format(@"\p[{0}]\s[-1]", i), true, true, callbackWindow.HWnd);
								}

								//起動完了
								IsRuntimeBooting = false;
								IsRuntimeAvailable = true;
							}));


						}
						catch
						{
							runtime.Kill();
						}
					});
				}
			}
			else
			{
				//プレビューデータが正しくない
				IsPreviewDataEnable = false;
				SurfaceList = CollectionViewSource.GetDefaultView(Array.Empty<Core.SurfacePreviewMetaDataRecord>());
				BindList = CollectionViewSource.GetDefaultView(Array.Empty<RuntimeBasedSurfaceViewerBindItemViewModel>());
			}

#endif
		}

		private void Runtime_Exited(object sender, EventArgs e)
		{
			IsRuntimeBooting = false;
			IsRuntimeAvailable = false;
			IsRuntimeExited = true;
			if (runtime != null)
			{
				runtime.Exited -= Runtime_Exited;
				runtime.Dispose();
				runtime = null;
			}

			//ランタイムが死んだらウインドウをクリアする必要がある
			foreach (var item in windowItems)
			{
				item.Value.Dispose();
			}
			windowItems.Clear();
		}

		//選択中のサーフェスのスコープが変更
		private void NotifyChangeScope()
		{
			//現在のスコープを更新
			currentScope = selectedSurface?.Scope ?? -1;

			//不要パネルを非表示
			foreach(var item in windowItems)
			{
				item.Value.Visibility = Visibility.Collapsed;
			}

			if(selectedSurface != null && captureScopes.Contains(selectedSurface.Scope))
			{
				if (windowItems.TryGetValue(selectedSurface.Scope, out var item))
				{
					//現在のスコープのウインドウを表示
					item.Visibility = Visibility.Visible;

					//きせかえリストを変更
					bindItems = previewData.Descript.GetBindCategories(selectedSurface.Scope)
						.Select(o => new RuntimeBasedSurfaceViewerBindCategoryViewModel(this, o)).ToArray();
					BindList = CollectionViewSource.GetDefaultView(bindItems);
				}
			}
			else
			{
				//スコープ制限
				bindItems = Array.Empty<RuntimeBasedSurfaceViewerBindCategoryViewModel>();
				BindList = CollectionViewSource.GetDefaultView(bindItems);
			}
		}

		//選択中のサーフェスが変更
		public void NotifyChangeSurface()
		{
			if (!IsRuntimeAvailable)
				return;
			if (selectedSurface == null)
				return;

			if(currentScope != selectedSurface.Scope)
			{
				NotifyChangeScope();
			}
			SyncToRuntime();
		}

		//SSPにウインドウを生成させるため、スコープ切り替えを送る
		private void MakeWindowRequestToRuntime(IEnumerable<int> scopes)
		{
			StringBuilder script = new StringBuilder();
			foreach (var s in scopes)
			{
				script.Append(string.Format(@"\p[{0}]\s[-1]", s));
			}
			try
			{
				//送信
				Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true, callbackWindow.HWnd);
			}
			catch { }
		}

		//サイズ情報を取得するためのもととなるリクエスト
		private void RequestMetadataImages(int scopeId, long surfaceId)
		{
			StringBuilder script = new StringBuilder();
			script.Append(string.Format(@"\![execute,dumpsurface,test,{0},{1}]", scopeId, surfaceId));
			script.Append(string.Format(@"\![execute,dumpsurface,test,{0},{1},surfacezero,,1]", scopeId, surfaceId));
			script.Append(string.Format(@"\m[{0},0,0]", MetadataGeneratedMessage));

			try
			{
				//送信
				generatingSurfaceId = surfaceId;
				Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true, callbackWindow.HWnd);
			}
			catch { }
		}

		//スケールの再計算
		private bool UpdateScale()
		{
			var windowItem = windowItems[selectedSurface.Scope];
			windowItem.CurrentSurfaceId = selectedSurface.Id;

			if (windowItem.CurrentSurfaceSizeData == null)
				return false;

			//dpiScale値を取得
			var dpiScale = PresentationSource.FromVisual(Main.MainWindow).CompositionTarget.TransformToDevice.M11;

			int surfaceHeight = windowItem.CurrentSurfaceSizeData.SurfaceSize.Height;
			int gridHeight = (int)(Control.FormsHostGrid.ActualHeight * dpiScale);

			//dpiスケールを最大としてスケール
			double requestScale = (double)gridHeight / (double)surfaceHeight;
			requestScale = Math.Min(requestScale, dpiScale);

			//パーセンテージで変化がある場合にのみ更新
			if ((int)(requestScale * 100.0) != (int)(CurrentScale * 100.0))
			{
				CurrentScale = requestScale;
				return true;
			}
			return false;
		}

		//ランタイムに変更を送信
		private void SyncToRuntime()
		{
			//選択中の情報を送る
			if (IsRuntimeAvailable && selectedSurface != null && captureScopes.Contains(selectedSurface.Scope))
			{
				var windowItem = windowItems[selectedSurface.Scope];
				windowItem.CurrentSurfaceId = selectedSurface.Id;
				windowItem.RepairWindowBind();

				//データがない
				if (!windowItem.HasSurfaceImageSize(selectedSurface.Id))
				{
					RequestMetadataImages(selectedSurface.Scope, selectedSurface.Id);
					return;
				}

				//スケール更新
				UpdateScale();

				StringBuilder script = new StringBuilder();

				//scope
				script.Append(string.Format(@"\p[{0}]", selectedSurface.Scope));

				//scale
				script.Append(string.Format(@"\![set,scaling,{0}]", (int)(CurrentScale * 100.0)));

				//surface
				script.Append(string.Format(@"\s[{0}]", selectedSurface.Id));

				//binds
				foreach (var category in bindItems)
				{
					foreach (var bind in category.Items)
					{
						script.Append(
							string.Format("\\![bind,\"{0}\",\"{1}\",{2}]", bind.Category, bind.Name, bind.IsEnabled ? 1 : 0)
							);
					}
				}

				//出力された2つのサーフェス画像からオフセットを算出して、シェルの基準位置ではなく実際のゴーストの位置を基準に表示する
				int marginTop = 0;
				if(windowItem.CurrentSurfaceSizeData != null)
				{
					marginTop = (int)(windowItem.CurrentSurfaceSizeData.MarginTop * CurrentScale);
				}

				//位置調整
				//\_w はお守り(たまに位置が合わないときがあるので対策として入れてみた程度…？)
				script.Append(string.Format(@"\_w[1]\![move,--X=0,--Y={0},--base=global,--base-offset=left.top,--move-offset=left.top]", marginTop));

				//判定表示設定
				if(showCollision)
				{
					script.Append(@"\![enter,collisionmode]");
				}
				else
				{
					script.Append(@"\![leave,collisionmode]");
				}

				//コリジョン作成(静止画)モードの場合、画像を出力する
				if(IsMakeCollisionMode)
				{
					script.Append(string.Format(@"\![execute,dumpsurface,test,{0},{1},surfacecol]", selectedSurface.Scope, selectedSurface.Id));
				}

				//完了通知
				script.Append(string.Format(@"\m[{0},0,0]", ScriptExecutedMessage));

				try
				{
					//送信
					generatingSurfaceId = selectedSurface.Id;
					Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true, callbackWindow.HWnd);
				}
				catch { }
			}
		}

		//スケール合わせ
		public void UpdateScale(double requestScale, bool forceSyncToRuntime)
		{
			//整数パーセンテージで変更がある場合にのみ更新
			if ((int)(requestScale * 100.0) != (int)(CurrentScale * 100.0))
			{
				//スケールを設定した後再表示
				CurrentScale = requestScale;
				SyncToRuntime();
			}
			else if(forceSyncToRuntime)
			{
				SyncToRuntime();
			}
		}

		public void PreviewWindowSizeChanged()
		{
			if (selectedSurface == null)
				return;

			//サイズの変更があれば再表示
			if(UpdateScale())
			{
				SyncToRuntime();
			}
		}

		private void OnScriptExecuted(int msg, IntPtr wparam, IntPtr lparam)
		{
			//TODO: さくらスクリプトが実行されるまえに別のサーフェスを選択した場合におかしくなるはずなので修正
			if (IsMakeCollisionMode)
			{
				SurfaceBitmapForMakeCollision?.Dispose();
				SurfaceBitmapForMakeCollision = null;
				try
				{
					//出力されたビットマップを拾ってくる
					var imagePath = DictionaryUtility.ConbinePath(runtime.RuntimeDirectory.FullPath, "ssp", "ghost", "temporaryghost", "ghost", "master", "test", "surfacecol" + generatingSurfaceId.ToString() + ".png");

					//ファイルをロックしないように一旦streamにする
					var bytes = File.ReadAllBytes(imagePath);
					SurfaceBitmapForMakeCollision = (Bitmap)Bitmap.FromStream(new MemoryStream(bytes));
				}
				catch { }
			}
		}

		//メタデータ用サーフェスが出力された
		private void OnMetadataGenerated(int msg, IntPtr wparam, IntPtr lparam)
		{
			var surface = items.FirstOrDefault(o => o.Id == generatingSurfaceId);
			if (surface != null)
			{
				//2つの画像をよみこみ、オリジン基準と画像実体基準のサイズを取得する
				var imagePath = DictionaryUtility.ConbinePath(runtime.RuntimeDirectory.FullPath, "ssp", "ghost", "temporaryghost", "ghost", "master", "test", "surface" + generatingSurfaceId.ToString() + ".png");
				var imagePath2 = DictionaryUtility.ConbinePath(runtime.RuntimeDirectory.FullPath, "ssp", "ghost", "temporaryghost", "ghost", "master", "test", "surfacezero" + generatingSurfaceId.ToString() + ".png");

				try
				{
					using (var surfaceBitmap = (Bitmap)Bitmap.FromFile(imagePath))
					{
						using (var surfaceZeroBitmap = (Bitmap)Bitmap.FromFile(imagePath2))
						{
							var sizeData = new SurfaceSizeData(surfaceBitmap.Size, surfaceZeroBitmap.Size);
							windowItems[surface.Scope].SetSurfaceImageSize(generatingSurfaceId, sizeData);

							//ちょっと不自然だがここで交信かけてやる
							NotifyChanged(nameof(SelectedSurfaceBaseSize));
						}
					}

					//表示
					SyncToRuntime();
				}
				catch
				{
					//IOがおかしいのでちょっとどうしようもない
				}
			}
		}

		public void Dispose()
		{
			runtime?.Dispose();

			//起動中の処理があればキャンセル
			runtimeBootCanceller?.Cancel();
			runtimeBootCanceller?.Dispose();
			runtimeBootTask?.Wait();

			//ウインドウを開放
			foreach (var item in windowItems)
			{
				item.Value.Dispose();
			}
			windowItems.Clear();
		}

		public void SendRuntimeChangeSurface(RuntimeBasedSurfaceViewerItemViewModel item)
		{
			var insertStr = string.Format(@"\p[{0}]\s[{1}]", item.Scope, item.Id.ToString());
			try
			{
				Satorite.SendSSTP(Main.Ghost, insertStr, false, false);
			}
			catch (GhostNotFoundException ex)
			{
				ex.PrintErrorLog();
			}
		}

		private class SurfaceSizeData
		{
			public System.Drawing.Size SurfaceSize { get; }
			public System.Drawing.Size ZeroOriginSize { get; }

			public int MarginTop => SurfaceSize.Height - ZeroOriginSize.Height;

			public SurfaceSizeData(System.Drawing.Size surfaceSize, System.Drawing.Size zeroOriginSize)
			{
				SurfaceSize = surfaceSize;
				ZeroOriginSize = zeroOriginSize;
			}
		}
		
		//キャプチャするウインドウあたりの情報
		private class WindowItem : IDisposable
		{
			private WindowsFormsHost formsHost;
			private Dictionary<long, SurfaceSizeData> surfaceSizeItems;

			public bool isWindowConnected;

			public RuntimeBasedSurfaceViewerViewModel Parent { get;}
			public Grid ParentGrid { get; }
			public System.Windows.Forms.Panel FormsPanel { get; set; }
			public IntPtr RuntimeHwnd { get; set; }
			public long CurrentSurfaceId { get; set; }

			public SurfaceSizeData CurrentSurfaceSizeData
			{
				get
				{
					if(surfaceSizeItems.TryGetValue(CurrentSurfaceId, out var sizeData))
					{
						return sizeData;
					}
					return null;
				}
			}
			
			public Visibility Visibility
			{
				get => formsHost.Visibility;
				set
				{
					if(formsHost.Visibility != value)
					{
						formsHost.Visibility = value;
					}
				}
			}

			public WindowItem(Grid parentGrid, IntPtr runtimeHwnd, RuntimeBasedSurfaceViewerViewModel parent)
			{
				Parent = parent;
				ParentGrid = parentGrid;
				RuntimeHwnd = runtimeHwnd;

				surfaceSizeItems = new Dictionary<long, SurfaceSizeData>();

				formsHost = new WindowsFormsHost();
				FormsPanel = new System.Windows.Forms.Panel();
				ParentGrid.Children.Add(formsHost);
				formsHost.Child = FormsPanel;

				formsHost.Loaded += FormsHost_Loaded;
			}

			public void Dispose()
			{
				formsHost.Loaded -= FormsHost_Loaded;
				formsHost.Child = null;
				ParentGrid.Children.Remove(formsHost);
			}

			public void SetSurfaceImageSize(long surfaceId, SurfaceSizeData sizeData)
			{
				surfaceSizeItems.Add(surfaceId, sizeData);
			}

			public bool HasSurfaceImageSize(long surfaceId)
			{
				return surfaceSizeItems.ContainsKey(surfaceId);
			}

			private void FormsHost_Loaded(object sender, RoutedEventArgs e)
			{
				if(isWindowConnected)
				{
					return;
				}
				//WPF側がロードされたタイミングで設定する
				//ウインドウ切替時に発生するとSetParentが失敗するので要調査
				BindGhostWindow(FormsPanel.Handle, RuntimeHwnd);
				isWindowConnected = true;
			}

			public void SetWindowZeroPosition()
			{
				//0位置設定するだけの実験用
				Win32Import.SetWindowPos(RuntimeHwnd, IntPtr.Zero, 0, 0, 0, 0, Win32Import.SWP_NOSIZE | Win32Import.SWP_NOZORDER | Win32Import.SWP_NOACTIVE | Win32Import.SWP_SHOWWINDOW);
			}

			//うまく親子関係がつながらないときがある？ SSPに取り戻される？ ようなので後天的に接続できるようにする
			public void RepairWindowBind()
			{
				if(Win32Import.GetParent(RuntimeHwnd) != FormsPanel.Handle)
				{
					BindGhostWindow(FormsPanel.Handle, RuntimeHwnd);
				}
			}

            private void BindGhostWindow(IntPtr hostHwnd, IntPtr childHwnd)
			{
				{
					IntPtr es = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE);
					es = new IntPtr(((long)es & ~Win32Import.WS_EX_LAYERED) | Win32Import.WS_EX_NOACTIVATE);
					Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE, es);
				}

				{
					IntPtr ws = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_STYLE);
					ws = new IntPtr(((long)ws | Win32Import.WS_CHILD ) & ~(Win32Import.WS_POPUP));
					Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_STYLE, ws);
				}

				if(Win32Import.IsWindow(childHwnd) == Win32Import.FALSE)
				{
					throw new Exception();
				}

				if (Win32Import.IsWindow(hostHwnd) == Win32Import.FALSE)
				{
					throw new Exception();
				}

				//親子関係を接続
				Win32Import.SetParent(childHwnd, hostHwnd);
				if (Marshal.GetLastWin32Error() != 0)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				{
					IntPtr es = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE);
					es = new IntPtr((long)es | Win32Import.WS_EX_LAYERED);
					Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE, es);
				}

				Win32Import.SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, 0, 0, Win32Import.SWP_NOZORDER | Win32Import.SWP_NOSIZE | Win32Import.SWP_NOACTIVE | Win32Import.SWP_SHOWWINDOW);
			}
		}
	}

	//プレビューのサーフェス情報
	internal class RuntimeBasedSurfaceViewerItemViewModel : NotificationObject
	{
		public LiteSurfaceRecord Model { get; }
		public ActionCommand InsertSurfaceCommand { get; }
		public ActionCommand RuntimeChangeSurfaceCommand { get; }

		public long Id { get; }
		public int Scope => Model.SatolistScope;
		public string Label
		{
			get
			{
				if(string.IsNullOrEmpty(Model.SurfaceTableLabel))
				{
					return Id.ToString();
				}
				else
				{
					return string.Format("{0} [{1}]", Id.ToString(), Model.SurfaceTableLabel);
				}
			}
		}
		public bool IsViewerVisible => Model.SatolistViewerVisible;

		public RuntimeBasedSurfaceViewerItemViewModel(RuntimeBasedSurfaceViewerViewModel parent, long id, LiteSurfaceRecord surface)
		{
			Id = id;
			Model = surface;

			InsertSurfaceCommand = new ActionCommand(
				o =>
				{
					SurfaceViewerViewModel.InsertSurfaceToActiveEditor(Id);
				}
				);

			RuntimeChangeSurfaceCommand = new ActionCommand(
				o =>
				{
					parent.SendRuntimeChangeSurface(this);
				}
				);
		}
	}

	//プレビューのきせかえカテゴリ
	internal class RuntimeBasedSurfaceViewerBindCategoryViewModel : NotificationObject
	{
		private bool isBindChangedProcessing;

		public RuntimeBasedSurfaceViewerViewModel Parent { get; }
		public RuntimeBasedSurfaceViewerBindItemViewModel[] Items { get; }
		public BindCategoryModel Model { get; }
		public string Label { get; }
		public string NodeType => "Category";

		//きせかえの指定が変更
		public void NotifyBindChanged(RuntimeBasedSurfaceViewerBindItemViewModel changedItem)
		{
			if (isBindChangedProcessing)
				return;	//再入防止
			isBindChangedProcessing = true;

			//設定状態をチェック
			if (!Model.IsMultiple && Items.Count(o => o.IsEnabled) >= 2)
			{
				//multipleではない場合に複数選択されていたら、今変更があったもの以外をはずす
				foreach(var item in Items)
				{
					if(!ReferenceEquals(item, changedItem))
					{
						item.IsEnabled = false;
					}
				}
			}

			if (Model.IsMustSelect && !Items.Any(o => o.IsEnabled))
			{
				//mustselectで選択されてない場合は一番上の１つを選択状態にする
				Items.First().IsEnabled = true;
			}

			//変更を送信
			Parent.NotifyChangeSurface();

			isBindChangedProcessing = false;
		}

		public RuntimeBasedSurfaceViewerBindCategoryViewModel(RuntimeBasedSurfaceViewerViewModel parent, BindCategoryModel category)
		{
			Model = category;
			Parent = parent;
			Items = category.Items.Select(o => new RuntimeBasedSurfaceViewerBindItemViewModel(this, o.Value)).ToArray();
			Label = category.Name;
		}
	}

	//プレビューのきせかえ情報
	internal class RuntimeBasedSurfaceViewerBindItemViewModel : NotificationObject
	{
		private bool isEnabled;
		public BindPartModel Model { get; }
		public RuntimeBasedSurfaceViewerBindCategoryViewModel Parent { get; }
		public IEnumerable<object> Items => Array.Empty<object>();
		public string Name => Model.Name;
		public string Category => Model.Category.Name;
		public string Label => Name;
		public string NodeType => "Item";

		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				if (isEnabled != value)
				{
					isEnabled = value;
					NotifyChanged();
					Parent.NotifyBindChanged(this);
				}
			}
		}

		public RuntimeBasedSurfaceViewerBindItemViewModel(RuntimeBasedSurfaceViewerBindCategoryViewModel parent, BindPartModel bindPart)
		{
			Parent = parent;
			Model = bindPart;
			isEnabled = Model.IsDefault;
		}
	}
}
