using Newtonsoft.Json;
using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//自動保存される系のデータ
	[JsonObject]
	public class TemporarySettings
	{
		private const string FilePath = "settings/temporary.json";

		[JsonIgnore]
		public static TemporarySettings Instance {get; private set;}

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

		//読み込み
		public static void Load()
		{
			try
			{
				var jsonSerializer = new JsonSerializer();
				Instance = jsonSerializer.Deserialize<TemporarySettings>(new JsonTextReader(new System.IO.StreamReader(FilePath)));

				if (Instance == null)
					throw new Exception();
			}
			catch
			{
				Instance = new TemporarySettings();
			}
		}

		public static void Save()
		{
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
				var jsonSerializer = new JsonSerializer();
				using (var writer = new System.IO.StreamWriter(FilePath))
				{
					JsonTextWriter w = new JsonTextWriter(writer);
					w.IndentChar = '\t';
					w.Indentation = 1;
					jsonSerializer.Serialize(w, Instance);
				}
			}
			catch
			{
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
}
