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
		public const int HistoryCount = 10;	//お気に入りを除くヒストリーの最大許容数

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
			{
				history.IsFavorite = item.IsFavorite;	//ただ移動するだけでも良かった？　とりあえず削除と絡めてみる
				GhostHistory.Remove(item);
			}
			GhostHistory.Insert(0, history);

			//TODO: 多くなりすぎたら捨てる
		}

		public OpenGhostHistory AddHistory(string path, string name, bool isFavorite)
		{
			var history = new OpenGhostHistory(name, path);
			var item = GhostHistory.FirstOrDefault(o => o.IsEquals(history));
			if(item != null)
				throw new Exception();  //知ってるものは追加しないはず
			history.IsFavorite = isFavorite;
			GhostHistory.Insert(0, history);
			return history;
		}

		//ヒストリーを整理
		public void TruncateHistory()
		{
			var items = GhostHistory.Where(o => !o.IsFavorite).ToArray();
			if(items.Length > HistoryCount)
			{
				//捨てる必要性
				var removeItems = items.Skip(HistoryCount).ToArray();
				foreach (var r in removeItems)
					GhostHistory.Remove(r);
			}
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
		public bool IsFavorite { get; set; }

		public OpenGhostHistory(string name, string path)
		{
			Name = string.IsNullOrEmpty(name) ? "(無題のゴースト)" : name;
			Path = DictionaryUtility.RemoveLastSlash(path);
		}

		public bool IsEquals(OpenGhostHistory item)
		{
			return DictionaryUtility.NormalizePath(Path) == DictionaryUtility.NormalizePath(item.Path);
		}
	}

	
}
