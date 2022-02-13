using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
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
	/// SurfaceViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class SurfaceViewer : UserControl
	{
		public SurfaceViewer()
		{
			InitializeComponent();
		}
	}

	internal class SurfaceViewerViewModel : NotificationObject, IDockingWindowContent
	{
		private string shellPath;
		private ShellAnalyzer shell;
		private SurfaceRenderer renderer;
		private ShellImageCache cache;

		private long selectedSurfaceID;

		public SurfaceViewerViewModel(string shellPath)
		{
			if (string.IsNullOrEmpty(shellPath))
				return;
			ShellPath = shellPath;
		}

		public ShellAnalyzer Shell
		{
			get => shell;
			set
			{
				shell = value;
				NotifyChanged();
				NotifyChanged(nameof(SurfaceIDList));
			}
		}

		public string ShellPath
		{
			get => shellPath;
			set
			{
				shellPath = value;
				NotifyChanged();

				//リロード
				shell = new ShellAnalyzer();
				shell.Load(shellPath);
				cache = new ShellImageCache(shell.ShellDirectoryPath);
				renderer = new SurfaceRenderer();

				NotifyChanged(nameof(SurfaceIDList));
			}
		}

		public IEnumerable<long> SurfaceIDList
		{
			get
			{
				if (shell != null)
				{

					//フィルタリング
					foreach (var id in shell.SurfaceIDList)
					{
						var record = shell.Records[id];

						if (!record.SatolistViewerVisible)
							continue;

						//画像ファイルとしてしか存在しないものはパス
						if (record.IsImageFileOnly)
							continue;

						yield return id;
					}
				}
			}
		}

		public long SelectedSurfaceID
		{
			get => selectedSurfaceID;
			set
			{
				selectedSurfaceID = value;
				NotifyChanged();
				NotifyChanged(nameof(SelectedSurfaceBitmap));
			}
		}

		public Bitmap SelectedSurfaceBitmap
		{
			get
			{
				if (renderer != null)
				{
					renderer.Rendering(shell, selectedSurfaceID, cache);
					return renderer.Image;

				}
				return null;
			}
		}

		public string DockingTitle => "サーフェスビューワ";

		public string DockingContentId => "SurfaceViewer";

	}

	

}
