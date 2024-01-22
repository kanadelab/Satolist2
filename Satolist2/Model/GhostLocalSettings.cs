using Newtonsoft.Json;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Satolist2.Model
{
	public class GhostLocalDictionarySettings
	{
		public enum ListMode
		{
			Default,
			ForceList,
			ForceText
		}

		[JsonProperty]
		public string Note { get; set; }
		[JsonProperty]
		public ListMode Mode { get; set; }

		public GhostLocalDictionarySettings()
		{
			Note = string.Empty;
			Mode = ListMode.Default;
		}

	}

	//ゴースト単位で保存される一時的データ
	//sspに便乗してghost/master/profileにいれちゃう
	public class GhostLocalSettings
	{
		//ゴーストのprofile格納
		public const string GhostLocalSettingPath = "profile/satolist/ghost.json";

		public static GhostLocalSettings Load(string itemPath)
		{
			var path = Utility.DictionaryUtility.ConbinePath(itemPath, GhostLocalSettingPath);
			if (System.IO.File.Exists(path))
			{
				return JsonUtility.DeserializeFromFile<GhostLocalSettings>(path);
			}
			return null;
		}

		public static void Save(string itemPath, GhostLocalSettings setting)
		{
			try
			{
				var path = Utility.DictionaryUtility.ConbinePath(itemPath, GhostLocalSettingPath);
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
				JsonUtility.SerializeToFile(path, setting);
			}
			catch { }
		}

		//前回アップロードに使用した設定
		[JsonProperty]
		public string LastUploadSettingId { get; set; }
		//前回アップロードで差分更新を使用した(ftp)
		[JsonProperty]
		public bool LastUploadUseDiff { get; set; }
		//前回アップロードでnar更新を使用した
		[JsonProperty]
		public bool LastUploadUseNar { get; set; }
		//前回アップロードで更新ファイルアップロードを使用した
		[JsonProperty]
		public bool LastUploadUseFiles { get; set; }
		//最後に起動を確認した実行ファイル
		[JsonProperty]
		public string LastBootExecutePath { get; set; }
		[JsonProperty]
		public string SurfacePreviewShellDirectory { get; set; }
		[JsonProperty]
		public bool IsRuntimeBasedSurfaceViewerEnabled { get; set; }

		[JsonProperty]
		public Dictionary<string, Tuple<int,int>> DefaultSurfacePaletteOffset { get; }

		[JsonProperty]
		public Dictionary<string, GhostLocalDictionarySettings> DictionarySettings { get; set; }

		public GhostLocalSettings()
		{
			DefaultSurfacePaletteOffset = new Dictionary<string, Tuple<int, int>>();
			DictionarySettings = new Dictionary<string, GhostLocalDictionarySettings>();
			SurfacePreviewShellDirectory = "master";
		}


		//サーフェスパレットのデフォルトオフセットを設定
		public void SetSurfacePaletteOffset(string shellName, int x, int y)
		{
			DefaultSurfacePaletteOffset[shellName] = new Tuple<int, int>(x, y);
		}

		public void GetSurfacePaletteOffset(string shellPath, out int x, out int y)
		{
			Tuple<int, int> result;
			if (DefaultSurfacePaletteOffset.TryGetValue(shellPath, out result))
			{
				x = result.Item1;
				y = result.Item2;
			}
			else
			{
				x = 0;
				y = 0;
			}
		}
	}
}