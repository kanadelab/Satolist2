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
		private SurfaceRenderer renderer;
		private ShellImageCache cache;

		public ObservableCollection<SurfacePaletteItemViewModel> Items { get; }

		public SurfacePaletteViewModel(string shellPath)
		{
			Items = new ObservableCollection<SurfacePaletteItemViewModel>();

			if (string.IsNullOrEmpty(shellPath))
				return;

			//TODO: SurfacePaletteと共有したい
			shell = new ShellAnalyzer();
			shell.Load(shellPath);
			cache = new ShellImageCache(shell.ShellDirectoryPath);
			renderer = new SurfaceRenderer();

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

				renderer.Rendering(shell, id, cache, 100, 100, offsetX, offsetY);
				Items.Add(new SurfacePaletteItemViewModel(id, renderer.Image));
			}
		}


		public string DockingTitle => "サーフェス パレット";

		public string DockingContentId => ContentId;
	}

	internal class SurfacePaletteItemViewModel
	{
		public Bitmap Image { get; set; }
		public string Label { get; set; }

		public SurfacePaletteItemViewModel(long id, Bitmap image)
		{
			Label = id.ToString();
			Image = image;
		}
	}
}
