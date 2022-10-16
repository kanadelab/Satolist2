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

		//ゴーストのprofile格納
		private const string GhostLocalSettingPath = "profile/satolist/ghost.json";

		//ロードエラー定型文
		private const string LoadErrorMessage = "設定ファイルのロードに失敗しました。";

		//デフォルトフォント
		public const string DefaultFont = "ＭＳ ゴシック";
		public const int DefaultFontSize = 14;

		public InsertItemPaletteModel InsertPalette { get; set; }
		public UploadServerSettingModelBase[] UploadSettings { get; set; }
		public TemporarySettings TemporarySettings { get; set; }
		public GhostLocalSettings GhostTemporarySettings { get; set; }
		public GeneralSettings GeneralSettings { get; set; }

		[JsonIgnore]
		public List<Dialog.ErrorListDialogItemViewModel> LoadErrors { get; }

		public EditorSettings()
		{
			LoadErrors = new List<Dialog.ErrorListDialogItemViewModel>();
			GhostTemporarySettings = new GhostLocalSettings();	//ゴースト設定のダミーを作成。サーフェスプレビュー設定とかゴースト側に設定するものがとりあえず動くような感じ
			LoadTemporarySettings();
			LoadInsertPalette();
			LoadUploadSettings();
			LoadGeneralSettings();
		
		}

		//ゴースト依存の情報をロード
		//基本的にはロードできてなくても初期状態にしてたいして差し支えない情報群
		public void LoadGhostTemporarySettings(GhostModel ghost)
		{
			var path = Utility.DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, GhostLocalSettingPath);
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
				//エラー
				GhostTemporarySettings = new GhostLocalSettings();
			}
		}

		public void SaveGhostTemporarySettings(GhostModel ghost)
		{
			//ゴーストを開いてない状態で保存しようとしたら蹴る
			//ゴーストを開いてなくてもダミーを用意するので保存だけキャンセルする方向
			if (ghost != null)
			{
				var path = Utility.DictionaryUtility.ConbinePath(ghost.FullDictionaryPath, GhostLocalSettingPath);
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
				JsonUtility.SerializeToFile(path, GhostTemporarySettings);
			}
		}

		private void LoadInsertPalette()
		{
			try
			{
				if (System.IO.File.Exists(InsertPaettePath))
				{
					//TODO: エラー対応?
					//エラーの場合は殆ど起動失敗と同じになるのが注意？強制終了でもいい？
					InsertPalette = JsonUtility.DeserializeFromFile<InsertItemPaletteModel>(InsertPaettePath);
				}
			}
			catch
			{
				var err = new Dialog.ErrorListDialogItemViewModel();
				err.Title = DictionaryUtility.NormalizeFullPath(InsertPaettePath);
				err.Description = LoadErrorMessage;
				LoadErrors.Add(err);
			}
		}

		public void SaveInsertPalette()
		{
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(InsertPaettePath));
				JsonUtility.SerializeToFile(InsertPaettePath, InsertPalette);
			}
			catch
			{
			}
		}

		public void LoadUploadSettings()
		{
			try
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
			catch
			{
				var err = new Dialog.ErrorListDialogItemViewModel();
				err.Title = DictionaryUtility.NormalizeFullPath(UploadSettingPath);
				err.Description = LoadErrorMessage;
				LoadErrors.Add(err);
			}
		}

		public void SaveUploadSettings()
		{
			try
			{
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UploadSettingPath));
				JsonUtility.SerializeToFile(UploadSettingPath, UploadSettings);
			}
			catch { }
		}

		private void LoadTemporarySettings()
		{
			//ここは消えても対して気にしない方向
			try
			{
				if (System.IO.File.Exists(TemporarySettingsPath))
				{
					var jsonSerializer = new JsonSerializer();
					TemporarySettings = jsonSerializer.Deserialize<TemporarySettings>(new JsonTextReader(new System.IO.StreamReader(TemporarySettingsPath)));
				}
			}
			catch
			{
				
				var err = new Dialog.ErrorListDialogItemViewModel();
				err.Title = DictionaryUtility.NormalizeFullPath(TemporarySettingsPath);
				err.Description = LoadErrorMessage;
				LoadErrors.Add(err);
			}

			if(TemporarySettings == null)
				TemporarySettings = new TemporarySettings();
		}

		public void SaveTemporarySettings()
		{
			try
			{
				//ヒストリーの掃除
				TemporarySettings.TruncateHistory();

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

		//基本設定の読込
		public void LoadGeneralSettings()
		{
			try
			{
				if (System.IO.File.Exists(GeneralSettingPath))
				{
					GeneralSettings = JsonUtility.DeserializeFromFile<GeneralSettings>(GeneralSettingPath);
				}
			}
			catch
			{
				var err = new Dialog.ErrorListDialogItemViewModel();
				err.Title = DictionaryUtility.NormalizeFullPath(GeneralSettingPath);
				err.Description = LoadErrorMessage;
				LoadErrors.Add(err);
			}

			if(GeneralSettings == null)
				GeneralSettings = new GeneralSettings();
		}

		//基本設定を読み込む
		//本ロードに入る前に必要なものをここでとりあえず読む想定。設計見直すかもだけど一旦簡単に使いたかったので
		public static GeneralSettings TemporaryLoadGeneralSettings()
		{
			try
			{
				if (System.IO.File.Exists(GeneralSettingPath))
				{
					return JsonUtility.DeserializeFromFile<GeneralSettings>(GeneralSettingPath);
				}
			}
			catch
			{
			}
			return null;
		}

		//基本設定の保存
		public void SaveGeneralSettings()
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TemporarySettingsPath));
			try
			{
				JsonUtility.SerializeToFile(GeneralSettingPath, GeneralSettings);
			}
			catch { }
		}

	}

	//基本設定情報
	public class GeneralSettings
	{
		//基本
		[JsonProperty]
		public bool UseOwnedSSTP { get; set; }
		[JsonProperty]
		public bool IsTextModeDefault { get; set; }
		[JsonProperty]
		public bool IsEnableUpdateCheck { get; set; }
		[JsonProperty]
		public bool IsEnablePreReleaseUpdate { get; set; }
		[JsonProperty]
		public int ListedDictionaryInsertEmptyLineCount { get; set; }
		[JsonProperty]
		public double TextEditorFontSize { get; set; }
		[JsonProperty]
		public string TextEditorFontName { get; set; }
		[JsonProperty]
		public bool IsEnableDarkMode { get; set; }

		//テキストエディタ
		[JsonProperty]
		public bool IsShowLineNumber { get; set; }
		[JsonProperty]
		public bool IsShowEndOfLine { get; set; }
		[JsonProperty]
		public bool IsWardWrap { get; set; }
		[JsonProperty]
		public bool IsIndent { get; set; }

		//カラー
		[JsonProperty]
		public Dictionary<string, uint> TextEditorColors { get; set; }
		
		//試験的
		[JsonProperty]
		public string TextEditorBackgroundImagePath { get; set; }
		[JsonProperty]
		public bool IsSurfacePaletteInserTypeSakuraScript { get; set; }
		[JsonProperty]
		public double TextEditorOffsetX { get; set; }
		[JsonProperty]
		public double TextEditorOffsetY { get; set; }

		public GeneralSettings()
		{
			//デフォルト値
			UseOwnedSSTP = false;
			IsTextModeDefault = false;
			ListedDictionaryInsertEmptyLineCount = 1;
			TextEditorColors = new Dictionary<string, uint>();

			IsEnableUpdateCheck = true;
			IsEnablePreReleaseUpdate = false;

			TextEditorFontName = EditorSettings.DefaultFont;
			TextEditorFontSize = EditorSettings.DefaultFontSize;
		}

		public GeneralSettings Clone()
		{
			return JsonUtility.CloneObject(this);
		}

		public bool IsEqlals(GeneralSettings obj)
		{
			return JsonUtility.SerializableObjectEquals(this, obj);

		}
	}
}
