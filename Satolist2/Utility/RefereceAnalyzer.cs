using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	//ジャンプ先などを解析するための参照アナライザ。
	//ジャンプ先が見つからないトークなどを検索するために使用する
	internal class RefereceAnalyzer
	{
		//private static readonly HashSet<string> LineOperationHead

		public void Analyze(string text)
		{
			//各行を解析
			var lines = DictionaryUtility.SplitLines(text);
			for(var i = 0; i < lines.Length; i++)
			{
				//if(st)
			}
		}

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

		//public bool IsOperationLine
	}
}
