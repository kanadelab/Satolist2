using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Satolist;
using System.Windows.Controls;
using SatolistLegacyCompat.CompatControls;
using System.ComponentModel;

namespace SatolistLegacyCompat.CompatCore
{
	//Satolist2からの設定値ビュー
	public static class ProjectCompat
	{
		public static CompatContol SurfaceViewerControl { get; private set; }
		private static LegacySurfaceViewer SurfaceViewerHost => (LegacySurfaceViewer)SurfaceViewerControl.WpfControl;
		private static RootWindow CompatRoot { get; set; }

		public static bool IsInitialized { get; private set; }

		public static JObject Serialize()
		{
			return JObject.FromObject(ProjectData.Instance);
		}

		public static void Deserialize(JObject data)
		{
			ProjectData.Instance = data.ToObject<ProjectData>();
		}

		//シェルロードを互換システムに通知
		public static void NotifyShellDirectory(string dir)
		{
			SurfaceViewerHost.SurfaceViewer.LoadShell(dir);
		}

		//ゴーストのロードを互換システムに通知
		public static void NotifyGhostDirectory(string dir)
		{
			CompatRoot.satori.Directory = dir;
		}

		//UI初期化
		public static void InitializeControls(bool isEnabled)
		{
			CompatRoot = new RootWindow();

			SurfaceViewerControl = new CompatContol();
			SurfaceViewerControl.Id = "SurfaceViewer";
			SurfaceViewerControl.Label = "サーフェスビューワ(さとりすと 1.x)";

			//有効な場合に限り、実体のコントロールの生成を行う
			//有効でない場合はドッキングコンテンツの識別むけに最低限の生成となる
			if(isEnabled)
			{
				SurfaceViewerControl.WpfControl = new CompatControls.LegacySurfaceViewer(CompatRoot);

				IsInitialized = true;
			}
		}
	}

	public class CompatContol
	{
		public Control WpfControl { get; internal set; }
		public string Label { get; internal set; }
		public string Id { get; internal set; }
	}
}
