using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Satolist2.Utility;

namespace Satolist2.Model
{
	//ゴーストテンプレートのモデル
	class GhostTemplateModel
	{
		public class GhostTemplateFileModel
		{
			public string RelativePath { get; }
			public string FullPath { get; }
			public bool IsDefaultChecked { get; }
			public bool IsCheckable { get; }
			public string Description { get; }

			public GhostTemplateFileModel(string relativePath, string fullPath, bool isDefaultChecked)
			{
				RelativePath = relativePath;	//TODO: 表示用のラベルを辞書の１行目から取得して表示
				FullPath = fullPath;
				IsDefaultChecked = isDefaultChecked;
				Description = string.Empty;

				//里々辞書かどうかは命名規則により
				if(Constants.SatoriDictionaryPattern.IsMatch(System.IO.Path.GetFileName(relativePath)))
				{
					IsCheckable = true;	//チェックのon/offが可能かどうか(里々辞書は可能)

					//里々辞書の場合はファイルを開いて説明を取得する
					try
					{
						var lines = System.IO.File.ReadAllLines(fullPath, Constants.EncodingShiftJis);
						var text = new StringBuilder();
						foreach(var line in lines)
						{
							if (line.IndexOf(Constants.SentenceHead) == 0 || line.IndexOf(Constants.WordHead) == 0)
								break;
							text.AppendLine(line);
						}
						Description = text.ToString();
					}
					catch
					{
						Description = "[説明の読込に失敗しました。]";
					}
				}
			}
		}

		//「テンプレートを使用しない」を示すテンプレート
		public static readonly GhostTemplateModel EmptyTemplate = new GhostTemplateModel();

		public const string TemplateDescriptFileName = "template.txt";
		public const string TemplateReadmeFileName = "template_readme.txt";
		public const string TemplateVersionKey = "version";
		public const string TemplateCraftmanKey = "craftman";
		public const string TemplateNameKey = "name";
		public const string TemplateCheckDefaultKey = "check_default";

		public const string TemplateFileKey = "file";
		public const string TemplateCheckedKey = "checked";

		private List<GhostTemplateFileModel> files;
		
		public bool IsCheckDefault { get; private set; }
		public string Craftman { get; private set; }
		public string Name { get; private set; }
		public string ReadMe { get; private set; }
		public IEnumerable<GhostTemplateFileModel> Files => files;

		public GhostTemplateModel(string templateDirectory):
			this()
		{
			Craftman = "不明";
			Name = "無名のテンプレート";
			ReadMe = "readmeがありません";

			var descriptPath = DictionaryUtility.ConbinePath(templateDirectory, TemplateDescriptFileName);
			var reader = new CsvBuilder();
			reader.DeserializeFromFile(descriptPath);

			//テンプレートバージョンをチェック
			int templateVersion = 1;
			var versionRecord = reader.Records.FirstOrDefault(o => o.Key == TemplateVersionKey);
			if (versionRecord.Value != null)
			{
				int v;
				if (int.TryParse(versionRecord.Value, out v))
					templateVersion = v;
			}

			switch(templateVersion)
			{
				case 1:
					ParseVersion1Template(reader, templateDirectory);
					break;
				case 2:
					ParseVersion2Template(reader, templateDirectory);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		//カラデータ
		public GhostTemplateModel()
		{
			files = new List<GhostTemplateFileModel>();

			Craftman = string.Empty;
			Name = "(テンプレートを使用しない)";
			ReadMe = "ゴーストテンプレートを使用せずにからっぽのゴーストを作成します。";
		}

		//さとりすとv2スタンダード
		//上書き更新できるように、すべてのファイルをdescriptに列挙するルールをとる
		private void ParseVersion2Template(CsvBuilder descript, string templateDirectory)
		{
			HashSet<string> listupedPath = new HashSet<string>();	//ファイル
			var checkPath = new Dictionary<string, bool>();	//チェックステータス

			foreach(var record in descript.Records)
			{
				if (string.IsNullOrEmpty(record.Key) || string.IsNullOrEmpty(record.Value))
					continue;

				switch(record.Key)
				{
					case TemplateCraftmanKey:
						Craftman = record.Value;
						break;
					case TemplateNameKey:
						Name = record.Value;
						break;
					case TemplateCheckDefaultKey:
						{
							int v;
							if (int.TryParse(record.Value, out v))
								IsCheckDefault = (v != 0);
						}
						break;
					case TemplateFileKey:
						listupedPath.Add(DictionaryUtility.NormalizePath(record.Value));
						break;
					case TemplateCheckedKey:
						{
							var sp = record.Value.Split(Constants.CommaSeparator, StringSplitOptions.None);
							if (sp.Length == 2)
							{
								int v;
								if(int.TryParse(sp[1], out v))
								{
									checkPath[sp[0]] = v != 0;
								}
							}
						}
						break;
				}
			}

			//readmeの取得
			var readmePath = DictionaryUtility.ConbinePath(templateDirectory, TemplateReadmeFileName);
			try
			{
				ReadMe = System.IO.File.ReadAllText(readmePath, Constants.EncodingShiftJis);
			}
			catch
			{
				ReadMe = "[template_readme.txt を取得できませんでした。]";
			}

			//メタ情報をreadmeに足す
			string[] readmeLines =
			{
				string.Format("作者: {0}", Craftman),
				string.Empty,
				ReadMe
			};
			ReadMe = string.Join(Constants.NewLine, readmeLines);

			//テンプレートのファイルを列挙
			var templateDirectoryFiles = System.IO.Directory.GetFiles(templateDirectory, "*.*", System.IO.SearchOption.AllDirectories);
			foreach (var item in templateDirectoryFiles)
			{
				var relativePath = DictionaryUtility.MakeRelativePath(templateDirectory, DictionaryUtility.NormalizePath(item));

				//リストアップされていないなら無視
				if (!listupedPath.Contains(relativePath))
					continue;

				bool defaultChecked = IsCheckDefault;
				if (checkPath.ContainsKey(relativePath))
					defaultChecked = checkPath[relativePath];

				files.Add(new GhostTemplateFileModel(relativePath, item, defaultChecked));
			}
		}

		//過去互換。今思い返すと正直記法がかなり微妙なので互換だけ残しておくような感じ
		private void ParseVersion1Template(CsvBuilder descript, string templateDirectory)
		{
			Dictionary<string, bool> checkedFiles = new Dictionary<string, bool>();
			HashSet<string> ignoreFiles = new HashSet<string>();

			ignoreFiles.Add(TemplateDescriptFileName);
			ignoreFiles.Add(TemplateReadmeFileName);

			string ignoreRecordHead = "ignore_";
			string checkedRecordHead = "check_";

			foreach(var record in descript.Records)
			{
				if (string.IsNullOrEmpty(record.Key) || string.IsNullOrEmpty(record.Value))
					continue;

				switch(record.Key)
				{
					case TemplateCraftmanKey:
						Craftman = record.Value;
						break;
					case TemplateNameKey:
						Name = record.Value;
						break;
					case TemplateCheckDefaultKey:
						{
							int v;
							if (int.TryParse(record.Value, out v))
								IsCheckDefault = (v != 0);
						}
						break;
					default:
						{
							if (record.Key.IndexOf(ignoreRecordHead) == 0)
							{
								int v;
								if (int.TryParse(record.Value, out v) && v != 0)
								{
									ignoreFiles.Add(record.Key.Substring(ignoreRecordHead.Length));
								}
							}
							else if (record.Key.IndexOf(checkedRecordHead) == 0)
							{
								int v;
								if (int.TryParse(record.Value, out v))
								{
									var key = record.Key.Substring(checkedRecordHead.Length);
									if (!checkedFiles.ContainsKey(key))
									{
										checkedFiles.Add(key, v != 0);
									}
								}
							}
						}
						break;
				}
			}

			//readmeの取得
			var readmePath = DictionaryUtility.ConbinePath(templateDirectory, TemplateReadmeFileName);
			try
			{
				ReadMe = System.IO.File.ReadAllText(readmePath, Constants.EncodingShiftJis);
			}
			catch
			{
				ReadMe = "[template_readme.txt を取得できませんでした。]";
			}

			//メタ情報をreadmeに足す
			string[] readmeLines =
			{
				string.Format("作者: {0}", Craftman),
				string.Empty,
				ReadMe
			};
			ReadMe = string.Join(Constants.NewLine, readmeLines);

			//テンプレートのファイルを列挙
			var templateDirectoryFiles = System.IO.Directory.GetFiles(templateDirectory, "*.*", System.IO.SearchOption.AllDirectories);
			foreach(var item in templateDirectoryFiles)
			{
				var relativePath = DictionaryUtility.MakeRelativePath(templateDirectory, DictionaryUtility.NormalizePath(item));

				if (ignoreFiles.Contains(relativePath))
					continue;
				bool defaultChecked = IsCheckDefault;
				if (checkedFiles.ContainsKey(relativePath))
					defaultChecked = checkedFiles[relativePath];

				files.Add(new GhostTemplateFileModel(relativePath, item, defaultChecked));
			}
		}

	}
}
