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
		private const string TemporarySettingsPath = "settings/temporary.json";
		public const string GeneralSettingPath = "settings/general.json";
		private const string LegacySettingPath = "settings/legacy.json";

		//ロードエラー定型文
		private const string LoadErrorMessage = "設定ファイルのロードに失敗しました。";

		//デフォルトフォント
		public const string DefaultFont = "ＭＳ ゴシック";
		public const int DefaultFontSize = 14;

		public InsertItemPaletteModel InsertPalette { get; set; }
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
			LoadGeneralSettings();
		}

		//ゴースト依存の情報をロード
		//基本的にはロードできてなくても初期状態にしてたいして差し支えない情報群
		public void LoadGhostTemporarySettings(GhostModel ghost)
		{
			try
			{
				GhostTemporarySettings = GhostLocalSettings.Load(ghost.FullDictionaryPath);
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
				GhostLocalSettings.Save(ghost.FullDictionaryPath, GhostTemporarySettings);
			}
		}

		private void LoadInsertPalette()
		{
			try
			{
				if (JsonUtility.ExistsFileWithBackup(InsertPaettePath))
				{
					//TODO: エラー対応?
					//エラーの場合は殆ど起動失敗と同じになるのが注意？強制終了でもいい？
					InsertPalette = JsonUtility.DeserializeFromFileWithBackup<InsertItemPaletteModel>(InsertPaettePath);
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
				JsonUtility.SerializeToFileWithBackup(InsertPaettePath, InsertPalette);
			}
			catch
			{
			}
		}

		private void LoadTemporarySettings()
		{
			//ここは消えても対して気にしない方向
			try
			{
				if (JsonUtility.ExistsFileWithBackup(TemporarySettingsPath))
				{
					TemporarySettings = JsonUtility.DeserializeFromFileWithBackup<TemporarySettings>(TemporarySettingsPath);
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
				JsonUtility.SerializeToFileWithBackup(TemporarySettingsPath, TemporarySettings);
			}
			catch
			{
			}

			//互換設定もこのタイミングで保存してしまう。同じタイミングが良さそう
			SaveLegacySettings();
		}

		//基本設定の読込
		public void LoadGeneralSettings()
		{
			try
			{
				if (JsonUtility.ExistsFileWithBackup(GeneralSettingPath))
				{
					GeneralSettings = JsonUtility.DeserializeFromFileWithBackup<GeneralSettings>(GeneralSettingPath);
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
				if (JsonUtility.ExistsFileWithBackup(GeneralSettingPath))
				{
					return JsonUtility.DeserializeFromFileWithBackup<GeneralSettings>(GeneralSettingPath);
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
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(GeneralSettingPath));
			try
			{
				JsonUtility.SerializeToFileWithBackup(GeneralSettingPath, GeneralSettings);
			}
			catch { }
		}

		//互換性設定の保存
		public static void LoadLegacySettings()
		{
			try
			{
				if(JsonUtility.ExistsFileWithBackup(LegacySettingPath))
				{
					SatolistLegacyCompat.CompatCore.ProjectCompat.Deserialize(
						JsonUtility.DeserializeFromFileWithBackup<JObject>(LegacySettingPath) ?? new JObject()
						);
				}
			}
			catch
			{
				SatolistLegacyCompat.CompatCore.ProjectCompat.Deserialize(
						new JObject()
						);
			}
		}

		//互換性設定の保存
		public static void SaveLegacySettings()
		{
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LegacySettingPath));
			try
			{
				JsonUtility.SerializeToFileWithBackup(LegacySettingPath, SatolistLegacyCompat.CompatCore.ProjectCompat.Serialize());
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
		public bool IsHilightCurrentLine { get; set; }
		[JsonProperty]
		public bool IsIndent { get; set; }
		[JsonProperty]
		public bool IsSurfacePaletteInserTypeSakuraScript { get; set; }
		[JsonProperty]
		public bool IsSurfacePaletteInsertTypeSatoriHalfWidth { get; set; }
		[JsonProperty]
		public bool IsShowTextEditorToolTip { get; set; }
		[JsonProperty]
		public bool IsShowTextEditorShioriEcho { get; set; }
		[JsonProperty]
		public bool IsShowTextEditorStatusBar { get; set; }

		//カラー
		[JsonProperty]
		public Dictionary<string, uint> TextEditorColors { get; set; }

		//ファイルイベントツリー
		//タイムスタンプソート(そうでない場合は名前ソート)
		[JsonProperty]
		public bool IsFileEventTreeTimestampSort { get; set; }

		//GhostBackup
		[JsonProperty(PropertyName = "Testing_IsEnableGhostBackupWithSave")]
		public bool IsEnableGhostBackupWithSave { get; set; }
		[JsonProperty(PropertyName = "Testing_GhostBackupWithSaveIntervalMinutes")]
		public int GhostBackupWithSaveIntervalMinutes { get; set; }
		[JsonProperty(PropertyName = "Testing_GhostBackupExpiryDays")]
		public int GhostBackupExpiryDays { get; set; }
		[JsonProperty(PropertyName = "Testing_GhostBackupMaxFileSizeKb")]
		public int GhostBackupMaxFileSizeKb { get; set; }
		
		//試験的
		[JsonProperty]
		public string TextEditorBackgroundImagePath { get; set; }
		[JsonProperty]
		public double TextEditorOffsetX { get; set; }
		[JsonProperty]
		public double TextEditorOffsetY { get; set; }
		[JsonProperty]
		public bool IsEnableLegacyCompat { get; set; }
		[JsonProperty]
		public string OverrideTextEditorEngine { get; set; }

		public GeneralSettings()
		{
			//デフォルト値
			UseOwnedSSTP = false;
			IsTextModeDefault = false;
			ListedDictionaryInsertEmptyLineCount = 1;
			TextEditorColors = new Dictionary<string, uint>();

			IsEnableUpdateCheck = true;
			IsEnablePreReleaseUpdate = false;
			IsShowTextEditorToolTip = true;

			TextEditorFontName = EditorSettings.DefaultFont;
			TextEditorFontSize = EditorSettings.DefaultFontSize;

			IsEnableGhostBackupWithSave = false;	//テスト。本実装にする場合はデフォルト有効に変更。
			GhostBackupExpiryDays = 30;
			GhostBackupWithSaveIntervalMinutes = 60;
			GhostBackupMaxFileSizeKb = 10 * 1000;

#if true
			OverrideTextEditorEngine = "AvalonEdit";
#else
			//レガシー機能をデフォルトで有効化。評価版につき。
			OverrideTextEditorEngine = "Azuki";
			IsEnableLegacyCompat = true;
#endif
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
