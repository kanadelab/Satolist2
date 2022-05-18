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

		//全体領域のドラッグ
		private void SurfaceImageArea_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			if (DataContext.CurrentCollisionType == SurfaceViewerViewModel.CollisionType.Polygon)
			{
				//頂点の追加
				DataContext.IsCollisionMaking = true;
				DataContext.AddPolygonPoint(new System.Windows.Point(e.HorizontalOffset, e.VerticalOffset));
			}
			else
			{
				DataContext.DragBeginX = (int)e.HorizontalOffset;
				DataContext.DragBeginY = (int)e.VerticalOffset;
				DataContext.DragMoveX = 0;
				DataContext.DragMoveY = 0;
			}
		}

		private void SurfaceImageArea_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			if (DataContext.CurrentCollisionType == SurfaceViewerViewModel.CollisionType.Polygon)
			{
				//nop
			}
			else
			{
				DataContext.IsCollisionMaking = true;
				DataContext.DragMoveX = e.HorizontalChange;
				DataContext.DragMoveY = e.VerticalChange;
			}
		}

		//ポリゴン頂点のドラッグ
		private void PolygonVertex_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			//nop
		}

		private void PolygonVertex_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			if (sender is Thumb th)
			{
				if (th.DataContext is SurfaceViewerPolygonPointViewModel p)
				{
					var newPoint = new System.Windows.Point(
						p.Point.X + e.HorizontalChange,
						p.Point.Y + e.VerticalChange
						);
					DataContext.SetPolygonPoint(p.Index, newPoint);
				}
			}
		}

		private void PolygonVertex_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is Thumb th)
			{
				//自分自身を消す
				if (th.DataContext is SurfaceViewerPolygonPointViewModel p)
				{
					e.Handled = true;
					DataContext.RemovePolygonPoint(p.Index);
				}
			}
		}

		private void PolygonVertex_Remove(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem th)
			{
				//自分自身を消す
				if (th.DataContext is SurfaceViewerPolygonPointViewModel p)
				{
					e.Handled = true;
					DataContext.RemovePolygonPoint(p.Index);
				}
			}
		}

		//矩形・楕円選択範囲のドラッグ
		private void SelectedArea_DragStarted(object sender, DragStartedEventArgs e)
		{
			//nop
		}

		private void SelectedArea_DragDelta(object sender, DragDeltaEventArgs e)
		{
			//右クリック中は範囲移動として動作させたい
			DataContext.DragBeginX += (int)e.HorizontalChange;
			DataContext.DragBeginY += (int)e.VerticalChange;
		}

		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//サイズ変更までついていけないので選択をリセット
			DataContext.CloseCollisionToolCommand?.Execute(null);
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//ダブルクリックでスクリプト挿入
			if (sender is ListViewItem listViewItem)
			{
				DataContext.InsertSurfaceToActiveEditor((SurfaceViewerItemViewModel)listViewItem.DataContext);
			}
			e.Handled = true;
		}
	}

	internal class SurfaceViewerViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		public enum CollisionType
		{
			Invalid,
			Rect,
			Ellipse,
			Circle,			//未対応。Ellipseで代用できるはずなので
			Polygon
		}

		public const string ContentId = "SurfaceViewer";
		private SurfaceViewer control;
		private bool isPreviewDataEnable;
		private Core.SurfacePreviewMetaData previewData;
		private ICollectionView surfaceList;
		private SurfaceViewerItemViewModel selectedSurface;
		private CollisionType collisionType = CollisionType.Rect;
		private bool isCollisionMaking;
		private SurfaceViewerItemViewModel[] items;

		//ドラッグ位置
		private double dragBeginX;
		private double dragBeginY;
		private double dragMoveX;
		private double dragMoveY;

		public double DragBeginX
		{
			get => dragBeginX;
			set
			{
				dragBeginX = value;
				NotifyChanged();
				NotifyUpdateRange();
			}
		}

		public double DragBeginY
		{
			get => dragBeginY;
			set
			{
				dragBeginY = value;
				NotifyChanged();
				NotifyUpdateRange();
			}
		}

		public double DragMoveX
		{
			get => dragMoveX;
			set
			{
				dragMoveX = value;
				NotifyChanged();
				NotifyUpdateRange();
			}
		}

		public double DragMoveY
		{
			get => dragMoveY;
			set
			{
				dragMoveY = value;
				NotifyChanged();
				NotifyUpdateRange();
			}
		}

		public double RectLeft
		{
			get
			{
				if (DragMoveX > 0)
				{
					return DragBeginX;
				}
				else
				{
					return DragBeginX + DragMoveX;
				}
			}
		}

		public double RectTop
		{
			get
			{
				if (DragMoveY > 0)
				{
					return DragBeginY;
				}
				else
				{
					return DragBeginY + DragMoveY;
				}
			}
		}

		public double RectWidth
		{
			get
			{
				return Math.Abs(DragMoveX);
			}
		}

		public double RectHeight
		{
			get
			{
				return Math.Abs(DragMoveY);
			}
		}

		public CollisionType CurrentCollisionType
		{
			get => collisionType;
			set
			{
				collisionType = value;
				NotifyChanged();
			}
		}

		public bool IsCollisionModeInvalid
		{
			get => CurrentCollisionType == CollisionType.Invalid;
			set
			{
				if (IsCollisionModeInvalid != value)
				{
					//基本的にfalseパターンは考えてない
					if (value)
					{
						CurrentCollisionType = CollisionType.Invalid;
						NotifyUpdateCollisionType();
					}
				}
			}
		}

		public bool IsCollisionModeRect
		{
			get => CurrentCollisionType == CollisionType.Rect;
			set
			{
				if(IsCollisionModeRect != value)
				{
					if(value)
					{
						CurrentCollisionType = CollisionType.Rect;
						NotifyUpdateCollisionType();
					}
				}
			}
		}

		public bool IsCollisionModeEllipse
		{
			get => CurrentCollisionType == CollisionType.Ellipse;
			set
			{
				if (IsCollisionModeEllipse != value)
				{
					if (value)
					{
						CurrentCollisionType = CollisionType.Ellipse;
						NotifyUpdateCollisionType();
					}
				}
			}
		}

		public bool IsCollisionModePolygon
		{
			get => CurrentCollisionType == CollisionType.Polygon;
			set
			{
				if (IsCollisionModePolygon != value)
				{
					if (value)
					{
						CurrentCollisionType = CollisionType.Polygon;
						NotifyUpdateCollisionType();
					}
				}
			}
		}

		public bool IsCollisionMaking
		{
			get => isCollisionMaking;
			set
			{
				//判定モードがinvalidならボックスの表示を避ける
				if (value && IsCollisionModeInvalid)
					return;

				isCollisionMaking = value;
				NotifyChanged();
				NotifyChanged(nameof(IsMovableCollisionMaking));
			}
		}

		public bool IsMovableCollisionMaking
		{
			get
			{
				return IsCollisionMaking &&
					(IsCollisionModeRect || IsCollisionModeEllipse);
			}
		}
		public string SelectedRange
		{
			get
			{
				if ((control?.SurfaceImage?.ActualWidth ?? 0.0) == 0.0)
					return string.Empty;

				//自動拡大縮小によって変更されたシェルのスケールを適用する
				var shellScale = control.SurfaceImage.Width / control.SurfaceImage.ActualWidth;

				switch(CurrentCollisionType)
				{
					case CollisionType.Rect:
					case CollisionType.Ellipse:
						return string.Format("{0},{1},{2},{3}", 
							(int)(RectLeft*shellScale),
							(int)(RectTop*shellScale),
							(int)((RectLeft + RectWidth)*shellScale),
							(int)((RectTop + RectHeight)*shellScale));
					case CollisionType.Polygon:
						return string.Join(",", PolygonPointsViewModel.Select(o => string.Format("{0},{1}",
							(int)(o.Point.X * shellScale),
							(int)(o.Point.Y * shellScale)
							)));
				}
				return string.Empty;
			}
		}

		public PointCollection PolygonPoints
		{
			get
			{
				return new PointCollection(
					PolygonPointsViewModel.Select(o => o.Point)
					);
			}
		}

		//ポリゴンの中身
		public ObservableCollection<SurfaceViewerPolygonPointViewModel> PolygonPointsViewModel
		{
			get;
		}

		public void NotifyUpdateRange()
		{
			NotifyChanged(nameof(SelectedRange));
			NotifyChanged(nameof(RectLeft));
			NotifyChanged(nameof(RectTop));
			NotifyChanged(nameof(RectWidth));
			NotifyChanged(nameof(RectHeight));
			NotifyChanged(nameof(RectHeight));
		}

		public void NotifyUpdateCollisionType()
		{
			//変更があった時一旦コリジョンの表示は消す
			dragBeginX = 0;
			dragBeginY = 0;
			dragMoveX = 0;
			dragMoveY = 0;
			PolygonPointsViewModel.Clear();
			control.CollisionPolygon.Points = PolygonPoints;
			NotifyUpdateRange();
			IsCollisionMaking = false;

			NotifyChanged(nameof(CurrentCollisionType));
			NotifyChanged(nameof(IsCollisionModeInvalid));
			NotifyChanged(nameof(IsCollisionModeRect));
			NotifyChanged(nameof(IsCollisionModeEllipse));
			NotifyChanged(nameof(IsCollisionModePolygon));
			NotifyChanged(nameof(IsMovableCollisionMaking));
		}

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
					NotifyUpdateCollisionType();
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

		public SurfaceViewerViewModel(MainViewModel main)
		{
			Main = main;
			PolygonPointsViewModel = new ObservableCollection<SurfaceViewerPolygonPointViewModel>();

			int index = 0;
			foreach(var p in PolygonPoints)
			{
				PolygonPointsViewModel.Add(new SurfaceViewerPolygonPointViewModel()
				{
					Point = p,
					Index = index
				});
				index++;
			}

			if (main.Ghost != null)
			{
				CollectionViewSource.GetDefaultView(main.SurfacePreview.SurfacePreviewData);
				UpdateSurfacePreviewData();
			}

			CopyToClipBoardCommand = new ActionCommand(
				o =>
				{
					try
					{
						//たまに例外はくので握りつぶす
						Clipboard.SetText(SelectedRange);
					}
					catch { }
				}
				);

			CloseCollisionToolCommand = new ActionCommand(
				//更新で初期状態に遷移する
				o => NotifyUpdateCollisionType()
				);

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

		//ポリゴン頂点を移動
		public void SetPolygonPoint(int index, System.Windows.Point point)
		{
			PolygonPointsViewModel[index].Point = point;

			//頂点がバインドだと反映されてくれないので直接設定している
			control.CollisionPolygon.Points = PolygonPoints;
			NotifyChanged(nameof(SelectedRange));
		}

		//ポリゴン頂点を追加
		public void AddPolygonPoint(System.Windows.Point point)
		{
			if (PolygonPointsViewModel.LastOrDefault() != null)
				PolygonPointsViewModel.LastOrDefault().IsLastVertex = false;

			PolygonPointsViewModel.Add(
				new SurfaceViewerPolygonPointViewModel()
				{
					Index = PolygonPointsViewModel.Count,
					Point = point
				});
			control.CollisionPolygon.Points = PolygonPoints;
			NotifyChanged(nameof(SelectedRange));

			//最後の頂点というフラグをたてる
			PolygonPointsViewModel.LastOrDefault().IsLastVertex = true;
		}

		//最新の頂点を削除
		public void RemovePolygonPoint(int index)
		{
			if (index < PolygonPointsViewModel.Count)
			{
				PolygonPointsViewModel.RemoveAt(index);
				control.CollisionPolygon.Points = PolygonPoints;
				NotifyChanged(nameof(SelectedRange));

				//最後の頂点のフラグを整理
				for(int i = 0; i < PolygonPointsViewModel.Count; i++)
				{
					var item = PolygonPointsViewModel[i];
					item.IsLastVertex = item == PolygonPointsViewModel.Last();
					item.Index = i;
				}
				
			}
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			this.control = (SurfaceViewer)control;
		}

		public void InsertSurfaceToActiveEditor(SurfaceViewerItemViewModel item)
		{
			var insertStr = string.Format("（{0}）", DictionaryUtility.NumberZen2Han(item.Id.ToString()));
			Main.InsertToActiveEditor(insertStr); ;
		}

		public void InsertSurfaceToActiveEditorSakuraScript(SurfaceViewerItemViewModel item)
		{
			var insertStr = string.Format(@"\s[{0}]", item.Id.ToString());
			Main.InsertToActiveEditor(insertStr);
		}

		public void SendRuntimeChangeSurface(SurfaceViewerItemViewModel item)
		{
			var insertStr = string.Format(@"\p[{0}]\s[{1}]", item.Scope, item.Id.ToString());
			Satorite.SendSSTP(Main.Ghost, insertStr, false, false);
		}

		public string DockingTitle => "サーフェスビューワ";

		public string DockingContentId => ContentId;
	}

	internal class SurfaceViewerItemViewModel : NotificationObject
	{
		public Core.SurfacePreviewMetaDataRecord Model { get; }
		public ActionCommand InsertSurfaceCommand { get; }
		public ActionCommand InsertSurfaceCommandSakuraScript { get; }
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
					parent.InsertSurfaceToActiveEditor(this);
				}
				);

			InsertSurfaceCommandSakuraScript = new ActionCommand(
				o =>
				{
					parent.InsertSurfaceToActiveEditorSakuraScript(this);
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
