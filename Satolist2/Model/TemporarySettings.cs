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
	//自動保存される系のデータ
	[JsonObject]
	public class TemporarySettings
	{
		[JsonProperty]
		public ObservableCollection<OpenGhostHistory> GhostHistory { get; set; }
		[JsonProperty]
		public string SerializedDockingLayout { get; set; }

		public TemporarySettings()
		{
			GhostHistory = new ObservableCollection<OpenGhostHistory>();
		}

		public void AddHistory(GhostModel ghost)
		{
			var history = new OpenGhostHistory(ghost.GhostDescriptName, ghost.FullPath);
			var item = GhostHistory.FirstOrDefault(o => o.IsEquals(history));
			if (item != null)
				GhostHistory.Remove(item);
			GhostHistory.Insert(0, history);

			//TODO: 多くなりすぎたら捨てる
		}
	}

	[JsonObject]
	public class OpenGhostHistory
	{
		[JsonProperty]
		public string Name { get; set; }
		[JsonProperty]
		public string Path { get; set; }
		[JsonProperty]
		public string IsFavorite { get; set; }

		public OpenGhostHistory(string name, string path)
		{
			Name = string.IsNullOrEmpty(name) ? "(無題のゴースト)" : name;
			Path = path;
		}

		public bool IsEquals(OpenGhostHistory item)
		{
			return DictionaryUtility.NormalizePath(Path) == DictionaryUtility.NormalizePath(item.Path);
		}
	}

	//ゴースト単位で保存される一時的データ
	//sspに便乗してghost/master/profileにいれちゃう
	public class GhostTemporarySettings
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
	}
}
