using SatolistLegacyCompat.CompatCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist
{
	//さとりすと1側のセーブデータ
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
		public bool isNoAutoSurfacePalette { get; set; }

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

	//さとりすと1側のRootWindow相当の互換クラス
	public class RootWindow
	{
		public SurfacePalette surfacePalette => ProjectCompat.SurfacePalette;
		public SurfaceViewer surfaces => ProjectCompat.SurfaceViewer;

		public SatoriCompat satori { get; set; }
		public OpeningGhostDialog mOpeningGhostDialog { get; set; }

		public SurfaceViewer surfaceViewer { get; set; }

		public RootWindow()
		{
			satori = new SatoriCompat();
		}
	}

	public class SatoriCompat
	{
		public string Directory { get; set; }
	}

	public enum CollisionType
	{
		RECT,
		ELLIPSE,
		CIRCLE,
		POLYGON,
		DISABLED
	}
}
