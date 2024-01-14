using Satolist2.Core;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
	}

	//viewmodel
	internal class RuntimeBasedSurfaceViewerViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver, IDisposable
	{
		public const string ContentId = "RuntimeBasedSurfaceViewer";
		public const int CaptureScopeCount = 3; //\p[x] までのウインドウを確保するか
		public const int ScriptExecutedMessage = 0x0401;

		private Dictionary<int, WindowItem> windowItems;
		private int currentScope;

		private TemporaryGhostRuntimeEx runtime;
		private SakuraFMORecord runtimeGhostFMORecord;

		private bool isPreviewDataEnable;
		private RuntimeBasedSurfacePreviewMetaData previewData;

		private ICollectionView surfaceList;
		private RuntimeBasedSurfaceViewerItemViewModel selectedSurface;
		private RuntimeBasedSurfaceViewerItemViewModel[] items;

		private ICollectionView bindList;
		private RuntimeBasedSurfaceViewerBindCategoryViewModel[] bindItems;

		private bool isRuntimeBooting;
		private bool isRuntimeAvailable;
		private bool isMakeCollisionMode;

		private SSTPCallBackNativeWindow callbackWindow;
		private CancellationTokenSource runtimeBootCanceller;
		private Task runtimeBootTask;

		private Bitmap surfaceBitmapForMakeCollision;

		public RuntimeBasedSurfaceViewer Control { get; private set; }
		public MainViewModel Main { get; private set; }
		public double CurrentScale { get; private set; }

		//判定作成(静止画)モード切り替えコマンド
		public ICommand ToggleMakeCollisionCommand { get; }

		public string DockingTitle => "サーフェスビューワv3";
		public string DockingContentId => ContentId;

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

		public RuntimeBasedSurfaceViewerItemViewModel SelectedSurface
		{
			get => selectedSurface;
			set
			{
				if (selectedSurface != value)
				{
					selectedSurface = value;
					NotifyChangeSurface();
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
			get => IsRuntimeAvailable;
			set
			{
				isRuntimeAvailable = value;
				NotifyChanged();
			}
		}

		//コリジョン作成(静止画)モード
		public bool IsMakeCollisionMode
		{
			get => isMakeCollisionMode;
			set
			{
				isMakeCollisionMode = value;
				NotifyChanged();
				NotifyChanged(nameof(IsRuntimeMode));
			}
		}

		//ランタイム表示モード
		public bool IsRuntimeMode
		{
			get => !isMakeCollisionMode;
		}

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
			windowItems = new Dictionary<int, WindowItem>();
			currentScope = -1;
			CurrentScale = 1.0;

			ToggleMakeCollisionCommand = new ActionCommand(
				(e) =>
				{
					//試しに切り替え
					IsMakeCollisionMode = !IsMakeCollisionMode;
					SendToRuntime();
				});
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is RuntimeBasedSurfaceViewer ctrl)
			{
				Control = ctrl;

				if (Main.Ghost != null)
				{
					UpdateSurfacePreviewData();
				}
			}
		}

		public void UpdateSurfacePreviewData()
		{
#if SURFACE_VIEWER_V3

			if (callbackWindow == null)
			{
				callbackWindow = new SSTPCallBackNativeWindow(Main.MainWindow.HWnd);
				callbackWindow.RegisterCallback(ScriptExecutedMessage, OnScriptExecuted);
			}

			if (Main.SurfacePreview.RuntimeBasedSurfacePreviewData != null)
			{
				IsPreviewDataEnable = true;
				previewData = Main.SurfacePreview.RuntimeBasedSurfacePreviewData;
				items = previewData.Surfaces.Records.Select(o => new RuntimeBasedSurfaceViewerItemViewModel(this, o.Key, o.Value)).ToArray();

				SurfaceList = CollectionViewSource.GetDefaultView(items);
				SurfaceList.Filter = (o) =>
				{
					return ((RuntimeBasedSurfaceViewerItemViewModel)o).IsViewerVisible;
				};
				SurfaceList.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

				NotifyChangeScope();
			}
			else
			{
				//プレビューデータが正しくない
				IsPreviewDataEnable = false;
				SurfaceList = CollectionViewSource.GetDefaultView(Array.Empty<Core.SurfacePreviewMetaDataRecord>());
				BindList = CollectionViewSource.GetDefaultView(Array.Empty<RuntimeBasedSurfaceViewerBindItemViewModel>());
			}

			//ランタイムをクローズ
			runtime?.Dispose();
			runtime = null;

			//初期化
			foreach(var item in windowItems)
			{
				item.Value.Dispose();
			}
			windowItems.Clear();

			//起動中の処理があればキャンセル
			runtimeBootCanceller?.Cancel();
			runtimeBootCanceller?.Dispose();
			runtimeBootTask?.Wait();
			runtimeBootCanceller = new CancellationTokenSource();

			//有効なシェルが指定されてる場合にランタイムを起動する
			if (Directory.Exists(Main.SurfacePreview.SelectedShellPath))
			{
				IsRuntimeBooting = true;

				//Sakura FMOを汚染しないようにGUIDでFMO名を決定して使用する
				runtime = TemporaryGhostRuntimeEx.PrepareShell(Main.SurfacePreview.SelectedShellPath, Guid.NewGuid().ToString());
				runtime.Boot();

				var cancelToken = runtimeBootCanceller.Token;

				var runtimeBootTask = Task.Run(() =>
				{
					try
					{
						//FMOの構築待ち
						while (true)
						{
							Thread.Sleep(100);
							cancelToken.ThrowIfCancellationRequested();
							runtimeGhostFMORecord = SakuraFMOReader.Read(runtime.Ghost, runtime.FMOName);
							if (runtimeGhostFMORecord != null && runtimeGhostFMORecord.HWndList.Length >= CaptureScopeCount)
							{
								break;
							}
						}

						//メインスレッドで表示更新系
						Control.Dispatcher.Invoke(new Action(() =>
						{
							//立ち絵のウインドウを取得
							for (int i = 0; i < CaptureScopeCount; i++)
							{
								var item = new WindowItem(Control.FormsHostGrid, runtimeGhostFMORecord.HWndList[i], this);
								windowItems.Add(i, item);
								item.Visibility = Visibility.Collapsed;
							}

							//起動完了
							IsRuntimeBooting = false;
							IsRuntimeAvailable = true;
						}));

						
					}
					catch
					{
						//TODO: エラー時処理、起動タイムアウトなど？
						//TODO: 終了時のタスクキャンセルなど
					}
				});

				
			}
#endif
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

			if(selectedSurface != null && selectedSurface.Scope < CaptureScopeCount)
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
			//TODO: ウインドウ切替時の対応がいる
			if (selectedSurface == null)
				return;

			if(currentScope != selectedSurface.Scope)
			{
				NotifyChangeScope();
			}
			SendToRuntime();
		}

		//ランタイムに変更を送信
		private void SendToRuntime()
		{
			//選択中の情報を送る
			if (runtimeGhostFMORecord != null && selectedSurface != null && selectedSurface.Scope < CaptureScopeCount)
			{
				StringBuilder script = new StringBuilder();

				//scope
				script.Append(string.Format(@"\p[{0}]", selectedSurface.Scope));
				//script.Append(string.Format(@"\p[{0}]", 1));

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
							string.Format(@"\![bind,{0},{1},{2}]", bind.Category, bind.Name, bind.IsEnabled ? 1 : 0)
							);
					}
				}

				if(IsMakeCollisionMode)
				{
					script.Append(string.Format(@"\![execute,dumpsurface,test,{0},{1}]", selectedSurface.Scope, selectedSurface.Id));
				}

				script.Append(string.Format(@"\m[{0},0,0]", ScriptExecutedMessage));

				try
				{
					//送信
					Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true, callbackWindow.HWnd);
				}
				catch { }
			}
		}

		//スケール合わせ
		public bool UpdateScale(double requestScale)
		{
			//整数パーセンテージで変更がある場合にのみ更新
			if ((int)(requestScale * 100.0) != (int)(CurrentScale * 100.0))
			{
				CurrentScale = requestScale;
				if (runtimeGhostFMORecord != null && selectedSurface != null && selectedSurface.Scope < CaptureScopeCount)
				{
					StringBuilder script = new StringBuilder();

					//scope
					script.Append(string.Format(@"\p[{0}]", selectedSurface.Scope));

					//scale
					script.Append(string.Format(@"\![set,scaling,{0}]", (int)(CurrentScale * 100.0)));

					script.Append(string.Format(@"\m[{0},0,0]", ScriptExecutedMessage));

					try
					{
						//送信
						Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true, callbackWindow.HWnd);
					}
					catch { }
					return true;
				}
			}
			return false;
		}

		public void PreviewWindowSizeChanged()
		{
			if (selectedSurface == null)
				return;

			//同じようにサイズ補正
			windowItems[selectedSurface.Scope].ResetWindowPosition();
		}

		private void OnScriptExecuted(int msg, IntPtr wparam, IntPtr lparam)
		{
			if (IsMakeCollisionMode)
			{
				try
				{
					//出力されたビットマップを拾ってくる
					var imagePath = DictionaryUtility.ConbinePath(runtime.RuntimeDirectory.FullPath, "ssp", "ghost", "temporaryghost", "ghost", "master", "test", "surface" + selectedSurface.Id.ToString() + ".png");
					SurfaceBitmapForMakeCollision = (Bitmap)Bitmap.FromFile(imagePath);
				}
				catch { }
			}
			else
			{
				//スクリプトの実行後、ウインドウのサイズが変更されている可能性があるので位置を更新する
				//TODO: かならずしも表示が更新されたタイミングで呼ばれるわけではないようなので、再検討が必要そう
				windowItems[selectedSurface.Scope].ResetWindowPosition();
			}
		}

		public void Dispose()
		{
			runtime?.Dispose();

			//起動中の処理があればキャンセル
			runtimeBootCanceller?.Cancel();
			runtimeBootCanceller?.Dispose();
			runtimeBootTask?.Wait();
		}

		private class WindowItem : IDisposable
		{
			private WindowsFormsHost formsHost;
			public bool isWindowConnected;

			public RuntimeBasedSurfaceViewerViewModel Parent { get;}
			public Grid ParentGrid { get; }
			public System.Windows.Forms.Panel FormsPanel { get; set; }
			public IntPtr RuntimeHwnd { get; set; }
			
			public Visibility Visibility
			{
				get => formsHost.Visibility;
				set
				{
					if(formsHost.Visibility != value)
					{
						formsHost.Visibility = value;
						ResetWindowPosition();
					}
				}
			}

			public WindowItem(Grid parentGrid, IntPtr runtimeHwnd, RuntimeBasedSurfaceViewerViewModel parent)
			{
				Parent = parent;
				ParentGrid = parentGrid;
				RuntimeHwnd = runtimeHwnd;

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

			public void ResetWindowPosition()
			{
				//Visilibityを子ウインドウ側にも適用
				if (Visibility == Visibility.Visible)
				{
					FormsPanel.Visible = true;

					//サイズ調整
					Win32Import.RECT rc = new Win32Import.RECT();
					Win32Import.GetClientRect(RuntimeHwnd, ref rc);

					//dpiScale値を取得
					var dpiScale = PresentationSource.FromVisual(Parent.Main.MainWindow).CompositionTarget.TransformToDevice.M11;

					//TODO: サイズがなんども更新されちゃうのでもうちょっといい決定方法を考えたい
					//SSP側のウインドウサイズと描画ターゲットGridのサイズを比較してスケールを決定する
					//このときSSP側は常にdpiスケール1倍の見た目なので、さとりすと側でさとりすとのdpiスケールと一致するようにサイズを補正する
					int surfaceHeight = (int)((rc.bottom - rc.top) / Parent.CurrentScale);
					int gridHeight = (int)(ParentGrid.ActualHeight * dpiScale);

					Debug.WriteLine($"surfaceHeight: {surfaceHeight}");
					//Debug.WriteLine($"surfaceHeight: {surfaceHeight}");

					//dpiスケールを最大としてスケール
					double requestScale = (double)gridHeight / (double)surfaceHeight;
					requestScale = Math.Min(requestScale, dpiScale);

					//一旦スケール合わせを優先する
					if (Parent.UpdateScale(requestScale))
						return;

					//var arr = Win32Import.SetWindowPos(RuntimeHwnd, IntPtr.Zero, 0, 0, 0, 0, Win32Import.SWP_NOSIZE | Win32Import.SWP_NOZORDER | Win32Import.SWP_NOACTIVE | Win32Import.SWP_SHOWWINDOW);
				}
				else
				{
					FormsPanel.Visible = false;
					//var arr = Win32Import.SetWindowPos(RuntimeHwnd, IntPtr.Zero, 0, 0, 0, 0, Win32Import.SWP_NOSIZE | Win32Import.SWP_NOZORDER | Win32Import.SWP_NOACTIVE | Win32Import.SWP_HIDEWINDOW);
				}
			}

			private void BindGhostWindow(IntPtr hostHwnd, IntPtr childHwnd)
			{
				//レイヤードウインドウのフラグを付け外ししないとうまくいかないので注意
				{
					IntPtr es = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE);
					es = new IntPtr(((long)es & ~Win32Import.WS_EX_LAYERED) | Win32Import.WS_EX_NOACTIVATE);
					var rees = Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE, es);
				}

				int a = Win32Import.IsWindow(childHwnd);
				int b = Win32Import.IsWindow(hostHwnd);
				

				//SetParentでアクティブを奪われるので対策が必要？
				IntPtr result = Win32Import.SetParent(childHwnd, hostHwnd);
				if (System.Runtime.InteropServices.Marshal.GetLastWin32Error() != 0)
					throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

				Win32Import.RECT rc = new Win32Import.RECT();
				Win32Import.GetClientRect(childHwnd, ref rc);
				int width = rc.right - rc.left;
				int height = rc.bottom - rc.top;

				var arr = Win32Import.SetWindowPos(childHwnd, IntPtr.Zero, 0, 0, 0, 0, Win32Import.SWP_NOSIZE | Win32Import.SWP_NOZORDER | Win32Import.SWP_NOACTIVE);

				{
					IntPtr es = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE);
					es = new IntPtr((long)es | Win32Import.WS_EX_LAYERED);
					var rees = Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE, es);
				}

				{
					IntPtr ws = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_STYLE);
					ws = new IntPtr((long)ws | Win32Import.WS_CHILD);
					var res = Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_STYLE, ws);
				}
			}
		}
	}

	//プレビューのサーフェス情報
	internal class RuntimeBasedSurfaceViewerItemViewModel : NotificationObject
	{
		public LiteSurfaceRecord Model { get; }

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
