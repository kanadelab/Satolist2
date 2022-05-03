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
		//TODO: SurfaceViewerと独立して持ってしまっているので一緒にしたい
		private ShellAnalyzer shell;
		private FileBaseSurfaceRenderer renderer;
		private ShellImageCache cache;

		public ObservableCollection<SurfacePaletteItemViewModel> Items { get; }
		public ActionCommand GenerateSurfacePreviewCommand { get; }

		public SurfacePaletteViewModel(string shellPath)
		{
			Items = new ObservableCollection<SurfacePaletteItemViewModel>();

			if (string.IsNullOrEmpty(shellPath))
				return;

			GenerateSurfacePreviewCommand = new ActionCommand(
				o =>
				{

				}
				);

			//TODO: SurfacePaletteと共有したい
			shell = new ShellAnalyzer();
			shell.Load(shellPath);
			cache = new ShellImageCache(shell.ShellDirectoryPath);
			renderer = new FileBaseSurfaceRenderer();

			//存在しているサーフェスをループ
			foreach(var id in shell.SurfaceIDList)
			{
				var record = shell.Records[id];
				if (!record.SatolistPaletteVisible)
					continue;

				//画像ファイルとしてしか存在しないモノはパス
				if (record.IsImageFileOnly)
					continue;

				var offsetX = record.SatolistPaletteOffsetX;
				var offsetY = record.SatolistPaletteOffsetY;

				renderer.Rendering(shell, id, cache/*, 100, 100, offsetX, offsetY*/);
				var imageViewModel = new SurfacePaletteItemViewModel(id, renderer.Image)
				{
					OffsetX = offsetX,
					OffsetY = offsetY,
					SizeX = 100,
					SizeY = 100
				};
				Items.Add(imageViewModel);
			}
		}


		public string DockingTitle => "サーフェス パレット";

		public string DockingContentId => ContentId;
	}

	internal class SurfacePaletteItemViewModel
	{
		public Bitmap Image { get; set; }
		public string Label { get; set; }
		public int OffsetX { get; set; }
		public int OffsetY { get; set; }
		public int SizeX { get; set; }
		public int SizeY { get; set; }

		public SurfacePaletteItemViewModel(long id, Bitmap image)
		{
			Label = id.ToString();
			Image = image;
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
				System.Drawing.Rectangle r = new System.Drawing.Rectangle(vm.OffsetX, vm.OffsetY, vm.SizeX, vm.SizeY);
				var cloneBitmap = vm.Image.Clone(r, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				return innerConverter.Convert(cloneBitmap, targetType, parameter, culture);
			}
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
