using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Satolist2.Utility;

namespace Satolist2.Model
{

	/// <summary>
	/// エディタ設定データ
	/// ゴーストリロードとはまた別の
	/// </summary>
	internal class EditorSettings
	{
		private const string InsertPaettePath = "settings/insertpalette.json";
		private const string UploadSettingPath = "settings/accounts.json";
		private const string TemporarySettingsPath = "settings/temporary.json";
		private const string GeneralSettingPath = "settings/general.json";

		public InsertItemPaletteModel InsertPalette { get; set; }
		public UploadServerSettingModelBase[] UploadSettings { get; set; }
		public TemporarySettings TemporarySettings { get; set; }
		public GhostLocalSettings GhostTemporarySettings { get; set; }
		public GeneralSettings GeneralSettings { get; set; }

		[JsonIgnore]
		public bool IsLoadFailed { get; set; }

		public EditorSettings()
		{
			LoadTemporarySettings();

			//TODO: 起動失敗処理？
			try
			{
				LoadInsertPalette();
				LoadUploadSettings();
			}
			catch
			{
				IsLoadFailed = true;
			}
		}

		//ゴースト依存の情報をロード
		//基本的にはロードできてなくても初期状態にしてたいして差し支えない情報群
		public void LoadGhostTemporarySettings(GhostModel ghost)
		{
			var path = Utility.DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, "profile/satolist.json");
			try
			{
				if (System.IO.File.Exists(path))
				{
					GhostTemporarySettings = JsonUtility.DeserializeFromFile<GhostLocalSettings>(path);
				}
				if (GhostTemporarySettings == null)
					throw new Exception();
			}
			catch
			{
				GhostTemporarySettings = new GhostLocalSettings();
			}
		}

		public void SaveGhostTemporarySettings(GhostModel ghost)
		{
			var path = Utility.DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, "profile/satolist.json");
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			JsonUtility.SerializeToFile(path, GhostTemporarySettings);
		}

		private void LoadInsertPalette()
		{
			if(System.IO.File.Exists(InsertPaettePath))
			{
				//TODO: エラー対応?
				//エラーの場合は殆ど起動失敗と同じになるのが注意？強制終了でもいい？
				InsertPalette = JsonUtility.DeserializeFromFile<InsertItemPaletteModel>(InsertPaettePath);
			}
		}

		void SaveInsertPalette()
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(InsertPaettePath));
			JsonUtility.SerializeToFile(InsertPaettePath, InsertPalette);
		}

		public void LoadUploadSettings()
		{
			if (System.IO.File.Exists(UploadSettingPath))
			{
				var itemArray = JsonUtility.DeserializeFromFile(UploadSettingPath) as JArray;
				var uploadSettings = new List<UploadServerSettingModelBase>();
				foreach (JObject item in itemArray)
				{
					var itemType = item["ItemType"].ToString();
					switch (itemType)
					{
						case FtpServerSettingModel.Type:
							uploadSettings.Add(item.ToObject<FtpServerSettingModel>());
							break;
						case NarnaloaderV2ServerSettingModel.Type:
							uploadSettings.Add(item.ToObject<NarnaloaderV2ServerSettingModel>());
							break;
						default:
							throw new Exception("アップロード設定に不明なエントリがあります");
					}
				}
				UploadSettings = uploadSettings.ToArray();
			}
		}

		public void SaveUploadSettings()
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UploadSettingPath));
			JsonUtility.SerializeToFile(UploadSettingPath, UploadSettings);
		}

		private void LoadTemporarySettings()
		{
			//ここは消えても対して気にしない方向
			try
			{
				var jsonSerializer = new JsonSerializer();
				TemporarySettings = jsonSerializer.Deserialize<TemporarySettings>(new JsonTextReader(new System.IO.StreamReader(TemporarySettingsPath)));

				if (TemporarySettings == null)
					throw new Exception();
			}
			catch
			{
				TemporarySettings = new TemporarySettings();
			}
		}

		public void SaveTemporarySettings()
		{
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TemporarySettingsPath));
				var jsonSerializer = new JsonSerializer();
				using (var writer = new System.IO.StreamWriter(TemporarySettingsPath))
				{
					JsonTextWriter w = new JsonTextWriter(writer);
					w.IndentChar = '\t';
					w.Indentation = 1;
					w.Formatting = Formatting.Indented;
					jsonSerializer.Serialize(w, TemporarySettings);
				}
			}
			catch
			{
			}
		}

		public void LoadGeneralSettings()
		{
			if(System.IO.File.Exists(GeneralSettingPath))
			{

			}
		}

		public void SaveGeneralSettings()
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TemporarySettingsPath));

		}

	}

	//基本設定情報
	public class GeneralSettings
	{
		[JsonProperty]
		public bool UseOwnedSSTP { get; set; }
		[JsonProperty]
		public bool IsTextModeDefault { get; set; }
		[JsonProperty]
		public int ListedDictionaryInsertEmptyLines { get; set; }


		public GeneralSettings Clone()
		{
			return new GeneralSettings()
			{
				UseOwnedSSTP = UseOwnedSSTP,
				IsTextModeDefault = IsTextModeDefault,
				ListedDictionaryInsertEmptyLines = ListedDictionaryInsertEmptyLines
			};
		}

		public bool IsEqlals(GeneralSettings obj)
		{
			if (UseOwnedSSTP != obj.UseOwnedSSTP)
				return false;
			if (ListedDictionaryInsertEmptyLines != obj.ListedDictionaryInsertEmptyLines)
				return false;
			if (IsTextModeDefault != obj.IsTextModeDefault)
				return false;
			return true;

		}
	}
}
