using Satolist;
using System;
using System.Collections.Generic;
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

namespace SatolistLegacyCompat.CompatControls
{
	/// <summary>
	/// LegacySurfaceViewer.xaml の相互作用ロジック
	/// </summary>
	public partial class LegacySurfaceViewer : UserControl
	{
		public Satolist.SurfaceViewer SurfaceViewer { get; }
		public LegacySurfaceViewer(RootWindow compatRoot)
		{
			InitializeComponent();

			SurfaceViewer = new Satolist.SurfaceViewer(compatRoot);
			SurfaceViewer.TopLevel = false;
			SurfaceViewer.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			SurfaceViewer.Show();
			SurfaceViewer.BringToFront();
			FormsHost.Child = SurfaceViewer;
		}
	}
}
