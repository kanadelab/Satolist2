using Satolist2.Core;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
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
	}

	internal class RuntimeBasedSurfaceViewerViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		public const string ContentId = "RuntimeBasedSurfaceViewer";
		public const int CaptureScopeCount = 3;	//\p[x] までのウインドウを確保するか

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
		private RuntimeBasedSurfaceViewerBindItemViewModel[] bindItems;

		public RuntimeBasedSurfaceViewer Control { get; private set; }
		public MainViewModel Main { get; private set; }

		public string DockingTitle => "RuntimeBasedSurfaceViewer";
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

		public RuntimeBasedSurfaceViewerViewModel(MainViewModel main)
		{
			Main = main;
			windowItems = new Dictionary<int, WindowItem>();
			currentScope = -1;
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
#if !DEPLOY
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

			runtime?.Dispose();
			runtime = null;

			//有効なシェルが指定されてる場合にランタイムを起動する
			if(Directory.Exists(Main.SurfacePreview.SelectedShellPath))
			{
				runtime = TemporaryGhostRuntimeEx.PrepareShell(Main.SurfacePreview.SelectedShellPath);
				runtime.Boot();

				//TODO: ここで待機、ウラでちゃんと待つようにすること
				while (true)
				{
					Thread.Sleep(1000);
					runtimeGhostFMORecord = SakuraFMOReader.Read(runtime.Ghost);
					if(runtimeGhostFMORecord != null && runtimeGhostFMORecord.HWndList.Length >= CaptureScopeCount )
					{
						break;
					}
				}

				//立ち絵のウインドウを取得
				for (int i = 0; i < CaptureScopeCount; i++)
				{
					var item = new WindowItem(Control.FormsHostGrid);
					BindGhostWindow(item.FormsPanel.Handle, runtimeGhostFMORecord.HWndList[i]);
					windowItems.Add(i, item);
					item.FormsHost.Visibility = Visibility.Collapsed;
				}
			}
#endif
		}

		private void BindGhostWindow(IntPtr hostHwnd, IntPtr childHwnd)
		{
			//レイヤードウインドウのフラグを付け外ししないとうまくいかないので注意
			{
				IntPtr es = Win32Import.GetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE);
				es = new IntPtr((long)es & ~Win32Import.WS_EX_LAYERED);
				var rees = Win32Import.SetWindowLongPtr(childHwnd, Win32Import.GWL_EXSTYLE, es);
			}

			IntPtr result = Win32Import.SetParent(childHwnd, hostHwnd);

			Win32Import.RECT rc = new Win32Import.RECT();
			Win32Import.GetClientRect(childHwnd, ref rc);
			int width = rc.right - rc.left;
			int height = rc.bottom - rc.top;

			var arr = Win32Import.SetWindowPos(childHwnd, hostHwnd, 0, 0, 0, 0, Win32Import.SWP_NOSIZE | Win32Import.SWP_NOZORDER);

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

		//選択中のサーフェスのスコープが変更
		private void NotifyChangeScope()
		{
			//現在のスコープを更新
			currentScope = selectedSurface?.Scope ?? -1;

			//不要パネルを非表示
			foreach(var item in windowItems)
			{
				item.Value.FormsHost.Visibility = Visibility.Collapsed;
			}

			if(selectedSurface != null && selectedSurface.Scope < CaptureScopeCount)
			{
				//現在のスコープのウインドウを表示
				windowItems[selectedSurface.Scope].FormsHost.Visibility = Visibility.Visible;

				//きせかえリストを変更
				bindItems = previewData.Descript.GetBindParts(selectedSurface.Scope).Select(o => new RuntimeBasedSurfaceViewerBindItemViewModel(this, o)).ToArray();
				BindList = CollectionViewSource.GetDefaultView(bindItems);
			}
			else
			{
				//スコープ制限
				bindItems = Array.Empty<RuntimeBasedSurfaceViewerBindItemViewModel>();
				BindList = CollectionViewSource.GetDefaultView(bindItems);
			}
		}

		//選択中のサーフェスが変更
		public void NotifyChangeSurface()
		{
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

				//surface
				script.Append(string.Format(@"\s[{0}]", selectedSurface.Id));

				//binds
				foreach(var bind in bindItems)
				{
					script.Append(
						string.Format(@"\![bind,{0},{1},{2}]", bind.Category, bind.Name, bind.IsEnabled ? 1 : 0)
						);
				}

				try
				{
					//送信
					Satorite.SendSSTP(runtimeGhostFMORecord, script.ToString(), true, true);
				}
				catch { }
			}
		}

		private class WindowItem
		{
			public WindowsFormsHost FormsHost { get; set; }
			public System.Windows.Forms.Panel FormsPanel { get; set; }
			public IntPtr RuntimeHwnd { get; set; }

			public WindowItem(Grid parentGrid)
			{
				FormsHost = new WindowsFormsHost();
				FormsPanel = new System.Windows.Forms.Panel();
				parentGrid.Children.Add(FormsHost);
				FormsHost.Child = FormsPanel;
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

	//プレビューのきせかえ情報
	internal class RuntimeBasedSurfaceViewerBindItemViewModel : NotificationObject
	{
		private bool isEnabled;
		public BindPartModel Model { get; }
		public RuntimeBasedSurfaceViewerViewModel Parent { get; }

		public string Name => Model.Name;
		public string Category => Model.Category.Name;
		public string Label => string.Format("{0}/{1}", Category, Name);

		public bool IsEnabled
		{
			get => isEnabled;
			set
			{
				if (isEnabled != value)
				{
					isEnabled = value;
					NotifyChanged();
					Parent.NotifyChangeSurface();
				}
			}
		}

		public RuntimeBasedSurfaceViewerBindItemViewModel(RuntimeBasedSurfaceViewerViewModel parent, BindPartModel bindPart)
		{
			Parent = parent;
			Model = bindPart;
			isEnabled = Model.IsDefault;
		}
	}
}
