using Newtonsoft.Json;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		public Dictionary<string, GhostLocalDictionarySettings> DictionarySettings { get; set; }

		public GhostLocalSettings()
		{
			DictionarySettings = new Dictionary<string, GhostLocalDictionarySettings>();
			SurfacePreviewShellDirectory = "master";
		}
	}
}