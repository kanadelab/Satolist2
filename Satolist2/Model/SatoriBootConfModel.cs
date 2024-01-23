using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Model
{
	//satori_bootconf.txt による里々の文字コード指定パーサ
	public class SatoriBootConfModel
	{
		private const string IsUtf8AllSettings = "is_utf8_all";
		private const string IsUtf8DicSettings = "is_utf8_dic";
		private const string IsUtf8ReplaceSettings = "is_utf8_replace";
		private const string IsUtf8SavedataSettings = "is_utf8_savedata";
		private const string IsUtf8CharactersiniSettings = "is_utf8_charactersini";
		private const string IsUtf8SatolistDeveloperOptionsSettings = "is_utf8_satolist_developer_options";
		private const string IsUtf8SatolistDeleteSettings = "is_utf8_satolist_delete";

		public bool IsUtf8Dic { get; private set; }
		public bool IsUtf8Replace { get; private set; }
		public bool IsUtf8Savedata { get; private set; }
		public bool IsUtf8CharactersIni { get; private set; }
		public bool IsUtf8SatolistDeveloperOptions { get; private set; }
		public bool IsUtf8SatolistDelete { get; private set; }

		//実際に有効なエンコーディング
		public Encoding DicEncoding => IsUtf8Dic ? Encoding.UTF8 : Constants.EncodingShiftJis;
		public Encoding ReplaceEncoding => IsUtf8Replace ? Encoding.UTF8 : Constants.EncodingShiftJis;
		public Encoding SavedataEncoding => IsUtf8Savedata ? Encoding.UTF8 : Constants.EncodingShiftJis;
		public Encoding CharactersIniEncoding => IsUtf8CharactersIni ? Encoding.UTF8 : Constants.EncodingShiftJis;
#if true
		public Encoding SatolistDeveloperOptionsEncoding => IsUtf8SatolistDeveloperOptions ? Encoding.UTF8 : Constants.EncodingShiftJis;
		public Encoding SatolistDeleteEncoding => IsUtf8SatolistDelete ? Encoding.UTF8 : Constants.EncodingShiftJis;
#else
		public Encoding SatolistDeveloperOptionsEncoding => Constants.EncodingShiftJis;
		public Encoding SatolistDeleteEncoding => Constants.EncodingShiftJis;
#endif


		private bool StringToBool(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;

			if(int.TryParse(s, out int result))
			{
				return result != 0;
			}
			return s.Equals("true", StringComparison.OrdinalIgnoreCase);
		}

		public void Load(string path)
		{
			if (!File.Exists(path))
				return;

			var builder = new CsvBuilder();
			builder.Deserialize(File.ReadAllText(path));

			//各種設定をチェック
			if (StringToBool(builder.GetValue(IsUtf8AllSettings)))
			{
				IsUtf8Dic = true;
				IsUtf8Replace = true;
				IsUtf8Savedata = true;
				IsUtf8CharactersIni = true;
			}

			if(builder.TryGetValue(IsUtf8DicSettings, out string dic))
			{
				IsUtf8Dic = StringToBool(dic);
			}

			if (builder.TryGetValue(IsUtf8ReplaceSettings, out string rep))
			{
				IsUtf8Replace = StringToBool(rep);
			}

			if (builder.TryGetValue(IsUtf8SavedataSettings, out string sav))
			{
				IsUtf8Savedata = StringToBool(sav);
			}

			if (builder.TryGetValue(IsUtf8CharactersiniSettings, out string ch))
			{
				IsUtf8CharactersIni = StringToBool(ch);
			}

#if true
			if(builder.TryGetValue(IsUtf8SatolistDeveloperOptionsSettings, out string dev))
			{
				IsUtf8SatolistDeveloperOptions = StringToBool(dev);
			}

			if(builder.TryGetValue(IsUtf8SatolistDeleteSettings, out string del))
			{
				IsUtf8SatolistDelete = StringToBool(del);
			}
#endif
		}
	}
}
