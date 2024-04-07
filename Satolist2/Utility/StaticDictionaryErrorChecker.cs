using Satolist;
using Satolist2.Control;
using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace Satolist2.Utility
{
	//辞書の静的解析エラーチェッカー
	//NOTE: ほとんどLegacyCompat行きの古いコードだけど、今のDictionaryModelに一致させる必要がある関係でこっちにおいてある
	internal class StaticDictionaryErrorChecker
	{
		private static readonly Regex choicePattern = new Regex("\\\\q\\[.+?,(?<target>.*?)[,\\]]");

		//検証結果のエラー情報
		public class ErrorRecord
		{
			public string Message { get; }
			public EventModel Event { get; }

			//EventModel のBody部に対する行インデックス
			public int? LineIndex { get; }

			public ErrorRecord(string message, EventModel ev, int? lineIndex)
			{
				Message = message;
				Event = ev;
				LineIndex = lineIndex;
			}
		}

		#region ReferenceChecker
		public static IEnumerable<ErrorRecord> CheckReferences(GhostModel ghost)
		{
			//TODO: 
			//見つからないジャンプの検索
			//ジャンプ先の名前に変数解決が含まれているとまず無理と判断してそこはスキップ

			//参照データをもとに全部参照する
			foreach (var dic in ghost.Dictionaries)
			{
				if (!dic.IsSatoriDictionary)
					continue;

				IEnumerable<String> refs = null;

				foreach (var ev in dic.InstantDeserializedEvents)
				{
					var references = AnalyzeReference(ev.Body);
					foreach (var target in references)
					{
						if (target.Target.IndexOf('（') >= 0 || target.Target.IndexOf('）') >= 0)
						{
							//参照ターゲット内部にカッコを含む場合はジャンプ先が固定にならないので無視
							//NOTE: いい感じにワイルドカード検索通せるといいけどね
							continue;
						}

						if (!TestReferenceHit(target.Target, ghost))
						{
							//見つからないのでエラーとして報告
							yield return new ErrorRecord($"見つからないジャンプ先「{target.Target}」", ev, target.LineIndex);
						}
					}
				}
			}
		}

		//参照情報
		private class Reference
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

		private static List<Reference> AnalyzeReference(string text)
		{
			var references = new List<Reference>();

			//各行を解析
			var lines = DictionaryUtility.SplitLines(text);
			for(var i = 0; i < lines.Length; i++)
			{
				var line = RemoveSatoriComment(lines[i]);

				//選択肢行かジャンプ行の場合にそれぞれ参照先を扱う
				if(line.FirstOrDefault() == '＞')
				{
					//ジャンプ先、タブの手前が参照になる
					var sp = line.Substring(1).Split(Constants.TabSeparator, 2, StringSplitOptions.None);
					references.Add(new Reference(sp[0], i));
					
				}
				else if(line.FirstOrDefault() == '＿')
				{
					//選択肢、タブのあと側が参照になる
					var sp = line.Substring(1).Split(Constants.TabSeparator, 2, StringSplitOptions.None);
					if(sp.Length > 1)
					{
						references.Add(new Reference(sp[1], i));
					}
				}

				line = GetIgnoreKakko(line);
				GetMatchJumpString(line, references, i);
			}

			return references;
		}

		private static void GetMatchJumpString(string line, List<Reference> references, int lineIndex)
		{
			// \q ジャンプスクリプト先の検出
			var matches = choicePattern.Matches(line);
			foreach(Match m in matches)
			{
				var s = m.Groups["target"].Value;
				if(!s.StartsWith("script:"))	//スクリプトを直接記載する記法の場合には無視
				{
					references.Add(new Reference(s, lineIndex));
				}
			}
		}

		//ジャンプ先検証機能
		private static bool TestReferenceHit(String target, GhostModel ghost)
		{
			foreach (var dic in ghost.Dictionaries)
			{
				if (!dic.IsSatoriDictionary)
					continue;

				foreach (var ev in dic.InstantDeserializedEvents)
				{
					if (ev.Name == target)
					{
						//発見
						return true;
					}

					foreach (var inl in ev.InlineEvents)
					{
						if (inl.InlineEvent.Name == target)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		//カッコに囲まれた部分をカッコごと取り除く
		private static String GetIgnoreKakko(String line)
		{
			String s = line.Replace("φφ", "[satolist-f]");
			s = s.Replace("φ（", "[satolist-open]");
			s = s.Replace("φ）", "[satolist-close]");

			String ret = "";
			int cnt = 0;
			foreach (char c in s)
			{
				if (c == '（')
				{
					cnt++;
				}
				else if (c == '）' && cnt > 0)
				{
					cnt--;
				}

				if (cnt == 0)
				{
					ret = ret + c;
				}
			}

			ret = ret.Replace("[satolist-f]", "φ");
			ret = ret.Replace("[satolist-open]", "（");
			ret = ret.Replace("[satolist-close]", "）");

			return ret;
		}
		#endregion

		#region CommonCheck

		//項目の種類を記録
		private static void CheckKakkoType(Dictionary<char, Dictionary<string, List<EventModel>>> dic, EventModel ev, char header, string name)
		{
			List<EventModel> list;
			if (dic[header].TryGetValue(name, out list))
			{
				list.Add(ev);
			}
			else
			{
				list = new List<EventModel>();
				list.Add(ev);
				dic[header].Add(name, list);
			}
		}

		//
		private static bool CheckKakkoString(String str)
		{
			bool isEscape = false;
			int kakkoCount = 0;
			foreach (char c in str)
			{
				switch (c)
				{
					case '（':
						if (!isEscape)
						{
							kakkoCount++;
						}
						else
						{
							isEscape = false;
						}
						break;

					case '）':
						if (!isEscape)
						{
							if (kakkoCount <= 0)
							{
								//error
								return false;
							}
							kakkoCount--;
						}
						else
						{
							isEscape = false;
						}
						break;
					case 'φ':
						isEscape = !isEscape;
						break;
					default:
						if (isEscape)
						{
							isEscape = false;
						}
						break;
				}
			}

			if (kakkoCount != 0)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		//計算処理っぽいかどうか
		private static bool IsCalcCehck(String target)
		{
			int nest_level = 0;
			String check_str = "";
			bool is_error = false;
			bool is_first = true;   //初回チェックかどうか。初回なら単項演算子無視を行う。
			foreach (char ch in target)
			{
				switch (ch)
				{
					case '）':
						nest_level--;
						break;
					case '（':
						if (nest_level == 0)
						{
							//ここでチェックを走らせる
							if (IsCalcFragment(check_str, is_first))
							{
								return true;
							}
							is_first = false;   //初回チェックを終了
							check_str = "";
						}
						nest_level++;
						break;
					default:
						if (nest_level == 0)
						{
							check_str += ch;
						}
						break;
				}
			}

			//最終的に残った文字列での検証
			if (!is_error && !String.IsNullOrWhiteSpace(check_str))
			{
				if (IsCalcFragment(check_str, is_first))
				{
					return true;
				}
			}

			return false;
		}

		private static readonly Regex pp = new Regex("[=＝!！<＜>＞][=＝]|[&＆]{2}|[\\|｜]{2}|[<＜>＞+＋\\-－/÷*×!！()]");
		private static readonly Regex pp_first = new Regex("^\\-[1234567890１２３４５６７８９０]*$");
		private static readonly Regex digit = new Regex("^[1234567890１２３４５６７８９０]+$");

		private static bool IsCalcFragment(String target, bool is_first)
		{
			Match mt = pp.Match(target);

			if (is_first)
			{
				if (pp_first.IsMatch(target))
				{
					return false;
				}
			}

			if (mt.Success)
			{

				String[] sp = pp.Split(target);
				foreach (String s in sp)
				{
					String check = s;


					if (String.IsNullOrEmpty(check))
					{
						continue;
					}
					if (!digit.IsMatch(check))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		private static bool CheckCalcSetter(String str)
		{
			//算術演算をタブで代入していないかを確認。
			try
			{
				//「＝」代入では？　というときに表示する
				String t = str;
				StringReader read = new StringReader(t);
				String line = null;

				for (int lineIndex = 0; true; lineIndex++)
				{
					line = read.ReadLine();
					if (line == null)
					{
						break;
					}

					if (line.Length < 1)
					{
						continue;
					}

					if (line[0] == '＄')
					{
						int[] pos = new int[3];
						pos[0] = line.IndexOf('＝');
						pos[1] = line.IndexOf('=');
						pos[2] = line.IndexOf('\t');

						int index = -1;
						for (int i = 0; i < pos.Length; i++)
						{
							if (index == -1)
							{
								if (pos[i] > 0)
								{
									index = i;
								}
							}
							else if (pos[i] < pos[index] && pos[i] > 0)
							{
								index = i;
							}
						}


						if (index == 2)
						{
							//この場合にタブ代入
							String[] sp = line.Split(new char[] { '\t' }, 2);
							if (sp.Length == 2)
							{
								String target = sp[1];

								if (IsCalcCehck(target))
								{
									//算術っぽい
									return true;
								}

							}
						}

					}
				}
			}
			catch { }

			return false;
		}

		private static bool CheckLineCommand(String str, bool is_word_defined, Dictionary<char, Dictionary<string, List<EventModel>>> dic, EventModel ev)
		{
			//単語群中で使用できないコマンドを使用していないかを確認。
			//単語群で行コマンドの使用を検出
			try
			{
				String t = str;
				StringReader read = new StringReader(t);
				String line = null;
				bool is_word = is_word_defined;

				for (int lineIndex = 0; true; lineIndex++)
				{
					line = read.ReadLine();
					if (line == null)
					{
						break;
					}

					if (line.Length < 1)
					{
						continue;
					}

					if (line[0] == '＄')
					{
						CheckKakkoType(dic, ev, '＄', line.Substring(1));
					}
					else if (line[0] == '＞')
					{
						CheckKakkoType(dic, ev, '＊', line.Substring(1));
					}

					if (line[0] == '＠')
					{
						CheckKakkoType(dic, ev, '＠', line.Substring(1));
						is_word = true;
					}
					else if (line[0] == '＊')
					{
						CheckKakkoType(dic, ev, '＊', line.Substring(1));
						is_word = false;
					}
					else if (is_word)
					{
						//行命令を判定
						if (line[0] == '＄' || line[0] == '＞' || line[0] == '＿')
						{
							//error
							return true;
						}
						else if (line.IndexOf('：') >= 0)
						{
							//error
							int pos = line.IndexOf('：');
							if (pos == 0)
							{
								return true;
							}
							else
							{
								if (line[pos - 1] != 'φ')
								{
									return true;
								}
							}
						}

					}
					//

				}
			}
			catch { }
			return false;
		}

		//辞書の静的チェック
		public static IEnumerable<ErrorRecord> CommonErrorCheck(GhostModel ghost)
		{
			//イベントのかぶりを検出する
			var event_types = new Dictionary<char, Dictionary<string, List<EventModel>>>();
			event_types.Add('＄', new Dictionary<string, List<EventModel>>());
			event_types.Add('＠', new Dictionary<string, List<EventModel>>());
			event_types.Add('＊', new Dictionary<string, List<EventModel>>());

			foreach (var dic in ghost.Dictionaries)
			{
				if (!dic.IsSatoriDictionary)
					continue;

				//satori-managed
				foreach (var ev in dic.InstantDeserializedEvents)
				{
					if (ev.Type == EventType.Header)
						continue;

					switch (ev.Type)
					{
						case EventType.Sentence:
							CheckKakkoType(event_types, ev, '＊', ev.Name);
							break;
						case EventType.Word:
							CheckKakkoType(event_types, ev, '＠', ev.Name);
							break;
					}

					String judgeNoComment = RemoveSatoriComment(ev.Condition);
					if (!String.IsNullOrWhiteSpace(judgeNoComment))
					{
						if (!CheckKakkoString(judgeNoComment))
						{
							//ERROR
							yield return new ErrorRecord("発生条件/カッコの不一致", ev, null);
						}

					}
					String noComment = "";
					foreach (String line in DictionaryUtility.SplitLines(ev.Body))
					{
						noComment += RemoveSatoriComment(line) + "\r\n";
					}

					if (!CheckKakkoString(noComment))
					{
						//Error
						yield return new ErrorRecord("本文/カッコの不一致", ev, null);
					}

					//算術演算エラー
					if (CheckCalcSetter(noComment))
					{
						yield return new ErrorRecord("本文/計算式を【タブ】で代入しようとしています。=を使用してください", ev, null);
					}

					//行コマンドエラー
					if (CheckLineCommand(noComment, ev.Type == EventType.Word, event_types, ev))
					{
						yield return new ErrorRecord("本文/単語群（＠）では「＞＿＄：」は使えません。", ev, null);
					}
				}
			}

			//名前被りチェック
			var counter = new Dictionary<string, int>();
			//またがっている数を列挙
			foreach (var item in event_types)
			{
				foreach (var ev in item.Value)
				{
					if (!counter.ContainsKey(ev.Key))
					{
						counter.Add(ev.Key, 0);
					}
					counter[ev.Key]++;
				}
			}
			//２つ以上またいでいるものはNG
			foreach (var p in counter)
			{
				if (p.Value >= 2)
				{
					//辞書構成error
					foreach (var item in event_types)
					{
						if (!item.Value.ContainsKey(p.Key))
						{
							continue;
						}

						var error_events = item.Value[p.Key];
						foreach (var error_ev in error_events)
						{
							yield return new ErrorRecord($"「{item.Key + p.Key}」が他の文・単語群・変数と名前被りしています。", error_ev, null);
						}
					}
				}
			}
		}
		#endregion

		private static string RemoveSatoriComment(string line)
		{
#if false
			//エスケープ考慮なし
			var commentIndex = line.IndexOf("＃");
			if (commentIndex < 0)
			{
				return line;
			}
			else
			{
				return line.Substring(commentIndex);
			}
#else
			//さとりすとv1のエスケープ考慮バージョン
			if (line == null)
			{
				return "";
			}

			int st = 0;
			int spos = 0;
			for (st = 0; st != -1 && st < line.Length; st = spos + 1)
			{
				spos = line.IndexOf('＃', st);
				bool isEscape = false;
				if (spos == -1)
				{
					return line;
				}

				for (int fpos = spos - 1; fpos >= 0; fpos--)
				{
					if (fpos == 'φ')
					{
						isEscape = !isEscape;
					}
				}
				if (!isEscape)
				{
					if (spos == 0)
					{
						return "";
					}
					else
					{
						return line.Substring(0, spos);
					}
				}
			}
			return line;
#endif
		}
	}
}
