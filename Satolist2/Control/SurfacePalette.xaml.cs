using Satolist2.Core;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Satolist2.Control
{
	/// <summary>
	/// SurfacePalette.xaml の相互作用ロジック
	/// </summary>
	public partial class SurfacePalette : UserControl
	{
		internal new SurfacePaletteViewModel DataContext
		{
			get => (SurfacePaletteViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		public SurfacePalette()
		{
			InitializeComponent();
			
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(sender is ListViewItem listViewItem)
			{
				DataContext.InsertSurfaceToActiveEditor((SurfacePaletteItemViewModel)listViewItem.DataContext);
			}
			e.Handled = true;
		}
	}

	internal class SurfacePaletteViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "SurfacePalette";
		private bool isPreviewDataEnable;
		private ObservableCollection<SurfacePaletteItemViewModel> items;
		private Task surfaceCreatingTask;
		private CancellationTokenSource cancelObject;

		public ReadOnlyObservableCollection<SurfacePaletteItemViewModel> Items
		{
			get => new ReadOnlyObservableCollection<SurfacePaletteItemViewModel>(items);
		}
		public MainViewModel Main { get; }
		public ActionCommand GenerateSurfacePreviewCommand { get; }
		public bool IsPreviewDataEnable
		{
			get => isPreviewDataEnable;
			set
			{
				isPreviewDataEnable = value;
				NotifyChanged();
			}
		}

		public SurfacePaletteViewModel(MainViewModel main)
		{
			Main = main;
			items = new ObservableCollection<SurfacePaletteItemViewModel>();

			GenerateSurfacePreviewCommand = new ActionCommand(
				o =>
				{

				}
				);

			if(main.Ghost != null)
			{
				UpdateSurfacePreviewData();
			}
		}

		public void UpdateSurfacePreviewData()
		{
			cancelObject?.Cancel();
			cancelObject = null;
			surfaceCreatingTask?.Wait();
			surfaceCreatingTask = null;

			if(Main.SurfacePreview.SurfacePreviewData != null)
			{
				items.Clear();
				IsPreviewDataEnable = true;

				foreach (var surface in Main.SurfacePreview.SurfacePreviewData.Items)
				{
					if (!surface.IsEnableSurfacePalette)
						continue;

					var item = new SurfacePaletteItemViewModel(this, surface, Main.SurfacePreview.ImageCache);
					items.Add(item);
				}

				cancelObject = new CancellationTokenSource();
				surfaceCreatingTask = Task.Run(() => { SurfaceLoadTask(cancelObject.Token); });
			}
			else
			{
				IsPreviewDataEnable = false;
				items.Clear();
			}
		}

		public void InsertSurfaceToActiveEditor(SurfacePaletteItemViewModel item)
		{
			if (MainViewModel.EditorSettings.GeneralSettings.IsSurfacePaletteInserTypeSakuraScript)
			{
				InsertSurfaceToActiveEditorSakuraScript(item);
			}
			else
			{
				InsertSurfaceToActiveEditorSatori(item);
			}
		}

		public void InsertSurfaceToActiveEditorSatori(SurfacePaletteItemViewModel item)
		{
			var insertStr = string.Format("（{0}）", DictionaryUtility.NumberZen2Han(item.Id.ToString()));
			Main.InsertToActiveEditor(insertStr); ;
		}

		public void InsertSurfaceToActiveEditorSakuraScript(SurfacePaletteItemViewModel item)
		{
			var insertStr = string.Format(@"\s[{0}]", item.Id.ToString());
			Main.InsertToActiveEditor(insertStr);
		}

		public void SendRuntimeChangeSurface(SurfacePaletteItemViewModel item)
		{
			var insertStr = string.Format(@"\p[{0}]\s[{1}]", item.Scope, item.Id.ToString());
			try
			{
				Satorite.SendSSTP(Main.Ghost, insertStr, false, false);
			}
			catch(GhostNotFoundException ex)
			{
				ex.PrintErrorLog();
			}
		}

		public void SurfaceLoadTask(CancellationToken cancelObject)
		{
			bool isFirstImage = true;
			foreach(var vm in items)
			{
				var image = vm.ImageCache.LoadImage(vm.FileName);
				if (image.Image != null)
				{
					bool isGenerate = isFirstImage;
					double ex = vm.Expand <= 0.0 ? 1.0 : vm.Expand;

					//ベースサイズとオフセット計算
					var offsetX = vm.OffsetX;
					var offsetY = vm.OffsetY;

					//オフセットがゼロの場合ゴーストローカル設定を使用
					int ghostLocalOffsetX, ghostLocalOffsetY;
					MainViewModel.EditorSettings.GhostTemporarySettings.GetSurfacePaletteOffset(Main.SurfacePreview.SelectedShell.ShellName, out ghostLocalOffsetX , out ghostLocalOffsetY);
					if (offsetX == 0.0)
						offsetX = ghostLocalOffsetX;
					if(offsetY == 0.0)
						offsetY = ghostLocalOffsetY;

					if (vm.BaseSizeX > 0)
						offsetX += (image.Image.Size.Width - vm.BaseSizeX);
					if (vm.BaseSizeY > 0)
						offsetY += (image.Image.Size.Height - vm.BaseSizeY);

					System.Drawing.Rectangle r = new System.Drawing.Rectangle(offsetX, offsetY, (int)(vm.SizeX / ex), (int)(vm.SizeY / ex));

					//0サイズになった場合はデフォルトの100に変換
					if (r.Width <= 0)
						r.Width = 100;
					if (r.Height <= 0)
						r.Height = 100;

					//境界チェック
					if(image.Image.Width < r.X + r.Width)
					{
						if(image.Image.Width < r.Width)
						{
							r.X = 0;
							r.Width = image.Image.Width;
						}
						else
						{
							r.X = image.Image.Width - r.Width;
						}
					}

					if (image.Image.Height < r.Y + r.Height)
					{
						if (image.Image.Height < r.Height)
						{
							r.Y = 0;
							r.Height = image.Image.Height;
						}
						else
						{
							r.Y = image.Image.Height - r.Height;
						}
					}

					var cloneBitmap = image.Image.Clone(r, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					Main.MainWindow.Dispatcher.BeginInvoke(new Action(() =>
				   {
					   vm.Image = cloneBitmap;

					   if (isGenerate)
					   {
						   try
						   {
							   //データがあったら、サムネイルを生成してprofileに保存しておく
							   //スタートメニューでつかう
							   var ghostPreviewPath = DictionaryUtility.ConbinePath(Main.Ghost.FullDictionaryPath, "profile/satolist");
							   System.IO.Directory.CreateDirectory(ghostPreviewPath);
							   vm.Image.Save(DictionaryUtility.ConbinePath(ghostPreviewPath, "preview.png"), System.Drawing.Imaging.ImageFormat.Png);
						   }
						   catch { }
					   }
				   }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);

					//1枚だけサムネ出力
					isFirstImage = false;
				}

				if (cancelObject.IsCancellationRequested)
					return;
			}
		}


		public string DockingTitle => "サーフェス パレット";

		public string DockingContentId => ContentId;
	}

	internal class SurfacePaletteItemViewModel : NotificationObject
	{
		private ShellImageCache imageCache;
		private SurfacePaletteViewModel parent;
		private Bitmap image;

		public Bitmap Image
		{
			get => image;
			set
			{
				image = value;
				NotifyChanged();
			}
		}
		
		public string Label { get; set; }
		public string FileName { get; set; }
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public int SizeX { get; set; }
		public int SizeY { get; set; }
		public int BaseSizeX { get; set; }
		public int BaseSizeY { get; set; }
		public long Id { get; set; }
		public int Scope { get; set; }
		public double Expand { get; set; }
		public ShellImageCache ImageCache => imageCache;

		public ActionCommand InsertSurfaceCommand { get; }
		public ActionCommand InsertSurfaceCommandSakuraScript { get; }
		public ActionCommand RuntimeChangeSurfaceCommand { get; }

		public SurfacePaletteItemViewModel(SurfacePaletteViewModel parent, Core.SurfacePreviewMetaDataRecord surface, ShellImageCache cache)
		{
			this.parent = parent;
			Id = surface.Id;
			imageCache = cache;
			Label = surface.Label;
			FileName = surface.FileName;
			SizeX = surface.FrameX;
			SizeY = surface.FrameY;
			Scope = surface.Scope;
			Expand = surface.Expand;
			OffsetX = surface.OffsetX;
			OffsetY = surface.OffsetY;
			BaseSizeX = surface.BaseSizeWidth;
			BaseSizeY = surface.BaseSizeHeight;

			InsertSurfaceCommand = new ActionCommand(
				o =>
				{
					parent.InsertSurfaceToActiveEditorSatori(this);
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
	
}
