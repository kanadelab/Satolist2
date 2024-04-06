using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Satolist2.Utility
{
	//ジャンプ先などを解析するための参照アナライザ。
	//ジャンプ先が見つからないトークなどを検索するために使用する
	internal class ReferenceAnalyzer
	{
		private static readonly Regex choicePattern = new Regex("\\\\q\\[.+?,(?<target>.*?)[,\\]]");

		public static List<Reference> Analyze(string text)
		{
			var references = new List<Reference>();

			//各行を解析
			var lines = DictionaryUtility.SplitLines(text);
			for(var i = 0; i < lines.Length; i++)
			{
				var line = RemoveComment(lines[i]);

				//選択肢行かジャンプ行の場合にそれぞれ参照先を扱う
				if(line.FirstOrDefault() == '＞')
				{
					//ジャンプ先、タブの手前が参照になる
					var sp = line.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
					references.Add(new Reference(sp[0], i));
					
				}
				else if(line.FirstOrDefault() == '＿')
				{
					//選択肢、タブのあと側が参照になる
					var sp = line.Split(Constants.TabSeparator, 2, StringSplitOptions.None);
					if(sp.Length > 1)
					{
						references.Add(new Reference(sp[1], i));
					}
				}
			}

			return references;
		}

		public void AnalyzeJumpScript(string line)
		{
			// \q ジャンプスクリプト先の検出
			var matches = choicePattern.Matches(line);
			foreach(Match m in matches)
			{
				var s = m.Groups["target"].Value;
				if(s.StartsWith("script:"))	//スクリプトを直接記載する記法の場合には無視
				{
					//TODO: リファレンスを追加

				}
			}
		}

		//コメント部の削除
		public static string RemoveComment(string line)
		{
			var commentIndex = line.IndexOf("＃");
			if(commentIndex < 0)
			{
				return line;
			}
			else
			{
				return line.Substring(commentIndex);
			}
		}

		//参照情報
		public class Reference
		{
			//参照ターゲット
			public string Target { get; }

			//行インデックス
			public int LineIndex { get; }

			public Reference(string target, int lineIndex)
			{
				Target = target;
				LineIndex = lineIndex;
			}
		}

	}
}
