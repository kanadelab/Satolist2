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
		public bool IsFavorite { get; set; }

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

	
}
