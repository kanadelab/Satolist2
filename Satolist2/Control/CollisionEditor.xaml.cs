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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Size = System.Drawing.Size;

namespace Satolist2.Control
{
	/// <summary>
	/// CollisionEditorControl.xaml の相互作用ロジック
	/// </summary>
	public partial class CollisionEditor : UserControl
	{
		public static readonly DependencyProperty SurfaceBitmapProperty = DependencyProperty.Register(nameof(SurfaceBitmap), typeof(Bitmap), typeof(CollisionEditor));
		public static readonly DependencyProperty SurfaceBaseSizeProperty = DependencyProperty.Register(nameof(SurfaceBaseSize), typeof(Size), typeof(CollisionEditor));
		public static readonly DependencyProperty ImageHorizontalAlignmentProperty = DependencyProperty.Register(nameof(ImageHorizontalAlignment), typeof(HorizontalAlignment), typeof(CollisionEditor));

		internal new CollisionEditorViewModel DataContext
		{
			get => (CollisionEditorViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public Bitmap SurfaceBitmap
		{
			get => (Bitmap)GetValue(SurfaceBitmapProperty);
			set => SetValue(SurfaceBitmapProperty, value);
		}

		public Size SurfaceBaseSize
		{
			get => (Size)GetValue(SurfaceBaseSizeProperty);
			set => SetValue(SurfaceBaseSizeProperty, value);
		}

		public HorizontalAlignment ImageHorizontalAlignment
		{
			get => (HorizontalAlignment)GetValue(ImageHorizontalAlignmentProperty);
			set => SetValue(ImageHorizontalAlignmentProperty, value);
		}
		
		public CollisionEditor()
		{
			InitializeComponent();
		}

		//全体領域のドラッグ
		private void SurfaceImageArea_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			if (DataContext.CurrentCollisionType == CollisionEditorViewModel.CollisionType.Polygon)
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
			if (DataContext.CurrentCollisionType == CollisionEditorViewModel.CollisionType.Polygon)
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
	}

	internal class CollisionEditorViewModel : NotificationObject
	{
		public enum CollisionType
		{
			Invalid,
			Rect,
			Ellipse,
			Circle,         //未対応。Ellipseで代用できるはずなので
			Polygon
		}

		public MainViewModel Main { get; }
		public CollisionEditor Control { get; set; }

		public ActionCommand CopyToClipBoardCommand { get; }
		public ActionCommand CloseCollisionToolCommand { get; }
		public ActionCommand SetSurfacePaletteDefaultPositionCommand { get; }

		private CollisionType collisionType = CollisionType.Rect;
		private bool isCollisionMaking;

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
				SetSurfacePaletteDefaultPositionCommand.NotifyCanExecuteChanged();
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
				if (IsCollisionModeRect != value)
				{
					if (value)
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

		public double[] SelectedPoints
		{
			get
			{
				if ((Control?.SurfaceImage?.ActualWidth ?? 0.0) == 0.0)
					return Array.Empty<double>();
				if (Control.SurfaceBitmap == null)
					return Array.Empty<double>();

				//自動拡大縮小によって変更されたシェルのスケールを適用する
				var shellScale = Control.SurfaceImage.Width / Control.SurfaceImage.ActualWidth;
				double baseSizeOffsetX = 0;
				double baseSizeOffsetY = 0;

				if (Control.SurfaceBaseSize.Width > 0)
					baseSizeOffsetX = Control.SurfaceBaseSize.Width - Control.SurfaceBitmap.Width;
				if (Control.SurfaceBaseSize.Height > 0)
					baseSizeOffsetY = Control.SurfaceBaseSize.Height - Control.SurfaceBitmap.Height;

				switch (CurrentCollisionType)
				{
					case CollisionType.Rect:
					case CollisionType.Ellipse:
						return new double[] {
							(RectLeft * shellScale + baseSizeOffsetX),
							(RectTop * shellScale + baseSizeOffsetY),
							((RectLeft + RectWidth) * shellScale + baseSizeOffsetX),
							((RectTop + RectHeight) * shellScale + baseSizeOffsetY)
						};
					case CollisionType.Polygon:
						var list = new List<double>();
						var points = PolygonPointsViewModel.Select(o => new double[] {
							(o.Point.X * shellScale + baseSizeOffsetX),
							(o.Point.Y * shellScale + baseSizeOffsetY)
						});
						foreach (var p in points)
							list.AddRange(p);
						return list.ToArray();

				}
				return Array.Empty<double>();
			}
		}

		public string SelectedRange
		{
			get
			{
				return string.Join(",", SelectedPoints.Select(o => (int)o));
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

		public CollisionEditorViewModel(MainViewModel main)
		{
			Main = main;
			PolygonPointsViewModel = new ObservableCollection<SurfaceViewerPolygonPointViewModel>();

			int index = 0;
			foreach (var p in PolygonPoints)
			{
				PolygonPointsViewModel.Add(new SurfaceViewerPolygonPointViewModel()
				{
					Point = p,
					Index = index
				});
				index++;
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


			SetSurfacePaletteDefaultPositionCommand = new ActionCommand(
				o => SetSurfacePaletteDefaultOffset(),
				//polygonを不許容
				o => IsCollisionMaking && SelectedPoints.Length > 2 && (CurrentCollisionType == CollisionType.Rect || CurrentCollisionType == CollisionType.Circle)
				);
		}

		public void NotifyUpdateRange()
		{
			NotifyChanged(nameof(SelectedRange));
			NotifyChanged(nameof(RectLeft));
			NotifyChanged(nameof(RectTop));
			NotifyChanged(nameof(RectWidth));
			NotifyChanged(nameof(RectHeight));
			NotifyChanged(nameof(RectHeight));
			SetSurfacePaletteDefaultPositionCommand.NotifyCanExecuteChanged();
		}

		public void NotifyUpdateCollisionType()
		{
			//変更があった時一旦コリジョンの表示は消す
			dragBeginX = 0;
			dragBeginY = 0;
			dragMoveX = 0;
			dragMoveY = 0;
			PolygonPointsViewModel.Clear();
			Control.CollisionPolygon.Points = PolygonPoints;
			NotifyUpdateRange();
			IsCollisionMaking = false;

			NotifyChanged(nameof(CurrentCollisionType));
			NotifyChanged(nameof(IsCollisionModeInvalid));
			NotifyChanged(nameof(IsCollisionModeRect));
			NotifyChanged(nameof(IsCollisionModeEllipse));
			NotifyChanged(nameof(IsCollisionModePolygon));
			NotifyChanged(nameof(IsMovableCollisionMaking));
		}

		//サーフェスパレットの表示位置を設定
		public void SetSurfacePaletteDefaultOffset()
		{
			var selectedPoints = SelectedPoints;
			if (selectedPoints.Length > 2)
			{
				MainViewModel.EditorSettings.GhostTemporarySettings.SetSurfacePaletteOffset(Main.SurfacePreview.SelectedShell.ShellName, (int)selectedPoints[0], (int)selectedPoints[1]);
				MainViewModel.EditorSettings.SaveGhostTemporarySettings(Main.Ghost);

				//サーフェスパレットをリロード
				Main.SurfacePaletteViewModel.UpdateSurfacePreviewData();
			}
		}

		//ポリゴン頂点を移動
		public void SetPolygonPoint(int index, System.Windows.Point point)
		{
			PolygonPointsViewModel[index].Point = point;

			//頂点がバインドだと反映されてくれないので直接設定している
			Control.CollisionPolygon.Points = PolygonPoints;
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
			Control.CollisionPolygon.Points = PolygonPoints;
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
				Control.CollisionPolygon.Points = PolygonPoints;
				NotifyChanged(nameof(SelectedRange));

				//最後の頂点のフラグを整理
				for (int i = 0; i < PolygonPointsViewModel.Count; i++)
				{
					var item = PolygonPointsViewModel[i];
					item.IsLastVertex = item == PolygonPointsViewModel.Last();
					item.Index = i;
				}

			}
		}
	}
}
