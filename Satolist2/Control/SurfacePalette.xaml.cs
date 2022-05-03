using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
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

namespace Satolist2.Control
{
	/// <summary>
	/// SurfacePalette.xaml の相互作用ロジック
	/// </summary>
	public partial class SurfacePalette : UserControl
	{
		public SurfacePalette()
		{
			InitializeComponent();
		}
	}

	internal class SurfacePaletteViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "SurfacePalette";
		private MainViewModel main;
		private ShellImageCache cache;
		private bool isPreviewDataEnable;
		private ObservableCollection<SurfacePaletteItemViewModel> items;
		public ReadOnlyObservableCollection<SurfacePaletteItemViewModel> Items
		{
			get => new ReadOnlyObservableCollection<SurfacePaletteItemViewModel>(items);
		}

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
			this.main = main;
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
			if(main.SurfacePreviewData != null)
			{
				items.Clear();
				IsPreviewDataEnable = true;
				cache = new ShellImageCache(DictionaryUtility.ConbinePath(main.Ghost.FullDictionaryPath, MainViewModel.SurfacePreviewPath));

				foreach (var surface in main.SurfacePreviewData.Items)
				{
					if (!surface.IsEnableSurfacePalette)
						continue;

					var item = new SurfacePaletteItemViewModel(surface.Id, surface.FileName, cache)
					{
						OffsetX = surface.OffsetX,
						OffsetY = surface.OffsetY
					};
					items.Add(item);
				}
			}
			else
			{
				IsPreviewDataEnable = false;
				items.Clear();
			}
		}


		public string DockingTitle => "サーフェス パレット";

		public string DockingContentId => ContentId;
	}

	internal class SurfacePaletteItemViewModel
	{
		public Bitmap Image
		{
			get => imageCache.LoadImage(FileName).Image;
		}
		private ShellImageCache imageCache;
		public string Label { get; set; }
		public string FileName { get; set; }
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public int SizeX { get; set; }
		public int SizeY { get; set; }

		public SurfacePaletteItemViewModel(long id, string fileName, ShellImageCache cache)
		{
			imageCache = cache;
			Label = id.ToString();
			FileName = fileName;
			SizeX = 100;
			SizeY = 100;
		}
	}

	//SurfacePaletteViewModelの示す情報で画像をクリップ
	internal class SurfacePaletteViewModelToImageSourceConverter : IValueConverter
	{
		private static BitmapImageSourceConverter innerConverter = new BitmapImageSourceConverter();
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SurfacePaletteItemViewModel vm)
			{
				if (vm.Image != null)
				{
					System.Drawing.Rectangle r = new System.Drawing.Rectangle(vm.OffsetX, vm.OffsetY, vm.SizeX, vm.SizeY);
					var cloneBitmap = vm.Image.Clone(r, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					return innerConverter.Convert(cloneBitmap, targetType, parameter, culture);
				}
				return null;
			}
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
