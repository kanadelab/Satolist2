using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

		[JsonProperty]
		public InsertItemPaletteModel InsertPalette { get; set; }

		[JsonIgnore]
		public bool IsLoadFailed { get; set; }

		public EditorSettings()
		{
			try
			{
				LoadInsertPalette();
			}
			catch
			{
				IsLoadFailed = true;
			}
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
			JsonUtility.SerializeToFile(InsertPalette, InsertPaettePath);
		}

	}
}
