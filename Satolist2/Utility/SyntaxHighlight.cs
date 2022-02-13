using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media;

namespace Satolist2.Utility
{
	enum ScriptSyntax
	{
		Escape,
		ScriptTag,
		ChangeSurface,
		Jump,
		Choice,
		Anchor,
		Word,
		Variable,
		Sentence,
		Getter,
		Random,
		Argument,
		Comment,
		Tab,
		Error,
		Function,
		Saori,
		Replace,
		ReplaceAfter
	}

	class SatoriSyntaxRuleSet : HighlightingRuleSet
	{
		private readonly List<SyntaxDefinition> Definitions = new List<SyntaxDefinition>()
		{
			new SyntaxDefinition("\\\\s\\[.+?\\]", ScriptSyntax.ChangeSurface),
			new SyntaxDefinition("\\\\s[0-9]", ScriptSyntax.ChangeSurface),
			new SyntaxDefinition("（[1234567890１２３４５６７８９０\\-－]+）", ScriptSyntax.ChangeSurface),

			new SyntaxDefinition("φ.", ScriptSyntax.Escape),

			new SyntaxDefinition("\\\\!\\[(raise|embed|get,property|timerraise|notify),.+?\\]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\[!pibncjmfx\\&]\\[.+?\\]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\_[wblvums]\\[.+?\\]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\__w\\[\\d+?\\]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\&\\[.+?\\]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\[01456huncvCxt\\*e\\-\\+]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\_[nqs\\?\\+V]", ScriptSyntax.ScriptTag),
			new SyntaxDefinition("\\\\w[0-9]", ScriptSyntax.ScriptTag),

			new SyntaxDefinition("^[＿].+", ScriptSyntax.Choice),
			new SyntaxDefinition("\\\\q\\[.+?\\]", ScriptSyntax.Choice),
			new SyntaxDefinition("\\\\__q\\[.+?\\].+?\\\\__q", ScriptSyntax.Choice),

			new SyntaxDefinition("^[＞].+", ScriptSyntax.Jump),

			new SyntaxDefinition("\\\\_a\\[.+?\\].+?\\\\_a", ScriptSyntax.Anchor),

			new SyntaxDefinition("^＊[^\t]*", ScriptSyntax.Sentence),

			new SyntaxDefinition("^＠[^\t]+", ScriptSyntax.Word),

			new SyntaxDefinition("^＄.+[\t＝\\=]", ScriptSyntax.Variable),

			new SyntaxDefinition("\\b(単語の追加|追加単語の削除|追加単語の全削除|合成単語群|バイト値)\\b", ScriptSyntax.Function),
			new SyntaxDefinition("\\b(call|nop|remember|set|sync|when|whenlist|loop|times|while|for|vncall|equal)\\b", ScriptSyntax.Function),

			new SyntaxDefinition("\\b(Value\\d+|教わること|引数区切り追加|引数区切り削除|文「.+」の重複回避|ウィンドウハンドル\\d+|スクリプトの一番頭|呼び出し回数制限|ジャンプ回数制限|デバッグ|SAORI引数の計算|BalloonOffset\\d+|Log|RequestLog|OperationLog|ResposeLog)\\b", ScriptSyntax.Getter),
			new SyntaxDefinition("\\b(スコープ切り替え時|さくらスクリプトによるスコープ切り替え時|.+タイマ|全タイマ解除|次から[0123456789０１２３４５６７８９]+回目のトーク|次のトーク|トーク予約のキャンセル|デフォルトサーフェス\\d+|会話時サーフェス戻し|サーフェス加算値\\d+|辞書フォルダ|辞書リロード|＄自動セーブ間隔|手動セーブ|セーブデータ暗号化|自動挿入ウェイトタイプ|自動挿入ウェイトの倍率|喋り間隔|喋り間隔誤差|今回は喋らない|見切れてても喋る|自動アンカー|今回は自動アンカー|トーク中のなでられ反応|なでられ持続秒数|なでられ反応回数)\\b", ScriptSyntax.Getter),
			new SyntaxDefinition("（(現在[時分秒]|起動回数|起動[時分秒]|累計[時分秒]|ＯＳ起動[時分秒]|単純累計[時分秒]|単純起動[時分秒]|単純ＯＳ起動[時分秒]|最終トークからの経過秒|ゴースト起動時間累計ミリ秒|サーフェス\\d+?|前回終了時サーフェス\\d+?|文「.+?」の数|文「.+?」の存在|単語群「.+?」の数|単語群「.+?」の存在|変数「.+?」の存在)）", ScriptSyntax.Getter),
			new SyntaxDefinition("（(里々のバージョン|本体の所在|Sender|Status|Charset|[ARSＡＲＳ]の数|隣で起動しているゴースト|起動しているゴースト数|.+?の存在|.+?のサーフェス|予約トーク|次から.+回目の予約トーク|トーク「.+?」の予約有無)）", ScriptSyntax.Getter),

			new SyntaxDefinition("（Reference(\\d+?|（/+?）)）", ScriptSyntax.Argument),

			new SyntaxDefinition("（乱数([－\\-]?[01234567890０１２３４５６７８９０]+?|（.+?）)～([－\\-]?[01234567890０１２３４５６７８９０]+?|（.+?）)）", ScriptSyntax.Random),
			new SyntaxDefinition("（乱数（.+?））", ScriptSyntax.Random),

			new SyntaxDefinition("＃.*", ScriptSyntax.Comment),

			new SyntaxDefinition("\t", ScriptSyntax.Tab)
		};

		//色情報
		private Dictionary<ScriptSyntax, int> HilightColors = new Dictionary<ScriptSyntax, int>()
		{
			{ ScriptSyntax.Escape, 0x888844 },
			{ ScriptSyntax.ScriptTag, 0x800000 },
			{ ScriptSyntax.ChangeSurface, 0x0080ff },
			{ ScriptSyntax.Jump, 0x008000 },
			{ ScriptSyntax.Choice, 0x008000 },
			{ ScriptSyntax.Anchor, 0x008000 },
			{ ScriptSyntax.Word, 0x0000ff },
			{ ScriptSyntax.Sentence, 0x0000ff },
			{ ScriptSyntax.Variable, 0x888844 },
			{ ScriptSyntax.Getter, 0x800000 },
			{ ScriptSyntax.Random, 0x008000 },
			{ ScriptSyntax.Argument, 0x008000 },
			{ ScriptSyntax.Comment, 0x108010 },
			{ ScriptSyntax.Tab, 0x000000 },
			{ ScriptSyntax.Function, 0x0080ff },
			{ ScriptSyntax.Saori, 0x0080ff },
			{ ScriptSyntax.Replace, 0x404070 },
			{ ScriptSyntax.ReplaceAfter, 0x404070 }
		};

		public SatoriSyntaxRuleSet()
		{
			Name = "Satori";
			foreach (var def in Definitions)
			{
				var rule = new HighlightingRule();
				var col = System.Drawing.Color.FromArgb(HilightColors[def.syntaxType]);
				var brush = new SimpleHighlightingBrush(System.Windows.Media.Color.FromArgb(255, col.R, col.G, col.B));

				rule.Color = new HighlightingColor();
				rule.Color.Foreground = brush;
				rule.Regex = new System.Text.RegularExpressions.Regex(def.pattern);
				Rules.Add(rule);
			}
		}
	}

	class SatoriSyntaxHilighter : IHighlightingDefinition
	{
		public string Name => "Satori";

		public HighlightingRuleSet MainRuleSet { get; }

		public IEnumerable<HighlightingColor> NamedHighlightingColors => throw new NotImplementedException();

		public IDictionary<string, string> Properties => throw new NotImplementedException();

		public HighlightingColor GetNamedColor(string name)
		{
			throw new NotImplementedException();
		}

		public HighlightingRuleSet GetNamedRuleSet(string name)
		{
			return MainRuleSet;
		}

		public SatoriSyntaxHilighter()
		{
			MainRuleSet = new SatoriSyntaxRuleSet();
		}
	}

	struct SyntaxDefinition
	{
		public string pattern;
		public ScriptSyntax syntaxType;

		public SyntaxDefinition(string pattern, ScriptSyntax type)
		{
			this.pattern = pattern;
			this.syntaxType = type;
		}
	}

}
