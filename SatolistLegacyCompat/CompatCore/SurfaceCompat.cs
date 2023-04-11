using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist
{
	public enum CollisionType
	{
		RECT,
		ELLIPSE,
		CIRCLE,
		POLYGON,
		DISABLED
	}

	public class ProjectData
	{
		public static ProjectData Instance { get; internal set; }
		public bool isShowSurfaceBind { get; set; }
		public bool isSurfaceShowCollision { get; set; }
		public bool isPlaySurfaceAnimation { get; set; }
		public bool isUseSurfacesScope { get; set; }
		public int surfaceViewerSizeX { get; set; }
		public int surfaceViewerSizeY { get; set; }
		public bool isUseSurfaceInsert { get; set; }
		public bool isUseSurfaceInsertSatori { get; set; }
		public bool isSurfaceUseNoSetting { get; set; }

		static ProjectData()
		{
			Instance = new ProjectData();
		}

		ProjectData()
		{
			isShowSurfaceBind = true;
			isSurfaceShowCollision = false;
			isPlaySurfaceAnimation = false;
			isUseSurfacesScope = true;
			isUseSurfaceInsert = false;
			isUseSurfaceInsertSatori = false;
			isSurfaceUseNoSetting = false;
			surfaceViewerSizeX = 800;
			surfaceViewerSizeY = 600;
		}
	}

	public class RootWindow
	{
		public SurfacePaletteCompat surfacePalette { get; set; }
		public SatoriCompat satori { get; set; }

		public SurfaceViewer surfaceViewer { get; set; }

		public IWin32Window owner { get; set; }

		public RootWindow()
		{
			surfacePalette = new SurfacePaletteCompat();
			satori = new SatoriCompat();
		}
	}

	public class SurfacePaletteCompat
	{
		public Font mUseFont { get; set; }
		public Brush mUseBrush { get; set; }

		public void UpdateOptionStatus()
		{
		}

		public void UpdateMenuEnable(bool enable_directory)
		{
		}

		public SurfacePaletteCompat()
		{
			mUseFont = new System.Drawing.Font("ＭＳ ゴシック", 8, FontStyle.Regular);
			mUseBrush = System.Drawing.Brushes.Black;
		}
	}

	public class SatoriCompat
	{
		public string Directory { get; set; }
	}
}
