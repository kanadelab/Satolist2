using Satolist2.Core;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
	/// SurfaceViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class SurfaceViewer : UserControl
	{
		internal new SurfaceViewerViewModel DataContext
		{
			get => (SurfaceViewerViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public SurfaceViewer()
		{
			InitializeComponent();
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//ダブルクリックでスクリプト挿入
			if (sender is ListViewItem listViewItem)
			{
				SurfacePaletteViewModel.InsertSurfaceToActiveEditor(((SurfaceViewerItemViewModel)listViewItem.DataContext).Id);
			}
			e.Handled = true;
		}
	}

	internal class SurfaceViewerViewModel : NotificationObject, IDockingWindowContent, IDockingWindowActive, IControlBindedReceiver
	{
		public const string ContentId = "SurfaceViewer";
		private SurfaceViewer control;
		private bool isPreviewDataEnable;
		private Core.SurfacePreviewMetaData previewData;
		private ICollectionView surfaceList;
		private SurfaceViewerItemViewModel selectedSurface;
		private SurfaceViewerItemViewModel[] items;
		private bool isDockingWindowActive;

		public CollisionEditorViewModel CollisionEditorViewModel { get; }
		public MainViewModel Main { get; }

		public SurfaceViewerItemViewModel SelectedSurface
		{
			get => selectedSurface;
			set
			{
				if (selectedSurface != value)
				{
					selectedSurface = value;
					NotifyChanged();
					NotifyChanged(nameof(SelectedSurfaceBitmap));
					NotifyChanged(nameof(SelectedSurfaceBaseSize));
					CollisionEditorViewModel.NotifyUpdateCollisionType();
				}
			}
		}

		public Bitmap SelectedSurfaceBitmap
		{
			get
			{
				if (SelectedSurface != null)
				{
					var path = SelectedSurface.Model.FileName;
					var loadedImage = Main.SurfacePreview.ImageCache.LoadImage(path);
					return loadedImage.Image;
				}
				return null;
			}
		}

		public System.Drawing.Size SelectedSurfaceBaseSize
		{
			get
			{
				if(selectedSurface != null)
				{
					return new System.Drawing.Size(selectedSurface.Model.BaseSizeWidth, selectedSurface.Model.BaseSizeHeight);
				}
				return default;
			}
		}

		public bool IsPreviewDataEnable
		{
			get => isPreviewDataEnable;
			private set
			{
				isPreviewDataEnable = value;
				NotifyChanged();
			}
		}

		public ICollectionView SurfaceList
		{
			get => surfaceList;
			private set
			{
				surfaceList = value;
				NotifyChanged();
			}
		}


		public ActionCommand CopyToClipBoardCommand { get; }
		public ActionCommand CloseCollisionToolCommand { get; }
		public ActionCommand SetSurfacePaletteDefaultPositionCommand { get; }

		public SurfaceViewerViewModel(MainViewModel main)
		{
			Main = main;
			CollisionEditorViewModel = new CollisionEditorViewModel(main);

			if (main.Ghost != null)
			{
				CollectionViewSource.GetDefaultView(main.SurfacePreview.SurfacePreviewData);
				UpdateSurfacePreviewData();
			}
		}

		public void UpdateSurfacePreviewData()
		{
			if (Main.SurfacePreview.SurfacePreviewData != null)
			{
				IsPreviewDataEnable = true;
				previewData = Main.SurfacePreview.SurfacePreviewData;
				items = previewData.Items.Select(o => new SurfaceViewerItemViewModel(this, o)).ToArray();

				SurfaceList = CollectionViewSource.GetDefaultView(items);
				SurfaceList.Filter = (o) =>
				{
					var item = ((SurfaceViewerItemViewModel)o).Model;
					return item.IsEnableSurfaceViewer;
				};
				SurfaceList.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
			}
			else
			{
				//プレビューデータが正しくない
				IsPreviewDataEnable = false;
				SurfaceList = CollectionViewSource.GetDefaultView(Array.Empty<Core.SurfacePreviewMetaDataRecord>());
			}
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (SurfaceViewer)control;
			CollisionEditorViewModel.Control = this.control.CollisionEditor;
		}

		public static void InsertSurfaceToActiveEditor(long id)
		{
			MainWindow.Instance.InsertToActiveEditorSurfaceChange(id);
		}

		public void SendRuntimeChangeSurface(SurfaceViewerItemViewModel item)
		{
			var insertStr = string.Format(@"\p[{0}]\s[{1}]", item.Scope, item.Id.ToString());
			try
			{
				Satorite.Default.SendSSTP(Main.Ghost, insertStr, false, false);
			}
			catch(GhostNotFoundException ex)
			{
				ex.PrintErrorLog();
			}
		}

		public string DockingTitle => "サーフェスビューワ";

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
	}

	internal class SurfaceViewerItemViewModel : NotificationObject
	{
		public Core.SurfacePreviewMetaDataRecord Model { get; }
		public ActionCommand InsertSurfaceCommand { get; }
		public ActionCommand RuntimeChangeSurfaceCommand { get; }

		public long Id => Model.Id;
		public int Scope => Model.Scope;
		public string Label => Model.Label;

		public SurfaceViewerItemViewModel(SurfaceViewerViewModel parent, Core.SurfacePreviewMetaDataRecord surface)
		{
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
	
	internal class SurfaceViewerPolygonPointViewModel : NotificationObject
	{
		private System.Windows.Point point;
		private int index;
		private bool isLastVertex;

		public System.Windows.Point Point
		{
			get => point;
			set
			{
				point = value;
				NotifyChanged();
			}
		}

		public int Index
		{
			get => index;
			set
			{
				index = value;
				NotifyChanged();
			}
		}

		public bool IsLastVertex
		{
			get => isLastVertex;
			set
			{
				isLastVertex = value;
				NotifyChanged();
			}
		}
	}

}
