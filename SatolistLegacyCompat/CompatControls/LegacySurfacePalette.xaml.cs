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
	/// LegacySurfacePalette.xaml の相互作用ロジック
	/// </summary>
	public partial class LegacySurfacePalette : UserControl
	{
		public Satolist.SurfacePalette SurfacePalette { get; }
		public LegacySurfacePalette(RootWindow compatRoot)
		{
			InitializeComponent();

			SurfacePalette = new Satolist.SurfacePalette(compatRoot);
			SurfacePalette.TopLevel = false;
			SurfacePalette.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			SurfacePalette.Show();
			SurfacePalette.BringToFront();
			FormsHost.Child = SurfacePalette;
		}
	}
}
