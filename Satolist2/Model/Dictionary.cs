using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Satolist2.Model
{
	//ゴーストデータのモデル
	//編集データのルートとなる
	public class GhostModel
	{
		private string fullPath;
		private ObservableCollection<DictionaryModel> dictionaries;

		//ゴーストのフルパス（readme.txt 等のフル階層）
		public string FullPath
		{
			get => fullPath;
		}

		public string FullDictionaryPath
		{
			get => FullPath + "/ghost/master";
		}

		public string FullGhostDescriptPath
		{
			get => FullDictionaryPath + "/descript.txt";
		}

		public string GhostDescriptSakuraName
		{
			get; private set;
		}

		public string GhostDescriptName
		{
			get; private set;
		}

		//ゴーストが持っている辞書データ
		public ReadOnlyObservableCollection<DictionaryModel> Dictionaries
		{
			get => new ReadOnlyObservableCollection<DictionaryModel>(dictionaries);
		}

		public GhostModel(string path)
		{
			fullPath = Path.GetFullPath(path);
			dictionaries = new ObservableCollection<DictionaryModel>();

			//ファイル巡回
			var files = Directory.GetFiles(FullDictionaryPath, "*.*", SearchOption.AllDirectories);
			foreach(var f in files)
			{
				if(Regex.IsMatch(Path.GetFileName(f), "^dic.*\\.txt$"))
				{
					var dict = new DictionaryModel(this, Path.GetFullPath(f));
					dictionaries.Add(dict);
				}
			}

			//descriptを探す
			//TODO: descript editor ができたら一時的でなくてもよさそう
			var tempDescript = new CsvBuilder();
			tempDescript.DeserializeFromFile(FullGhostDescriptPath);
			GhostDescriptSakuraName = tempDescript.GetValue("sakura.name");
			GhostDescriptName = tempDescript.GetValue("name");

		}
	}

	//テキストファイルデータ
	public class TextFileModel : NotificationObject
	{
		private string body;
		private bool bodyAvailable;	//false ならbodyは無効。里々辞書形式でデシリアライズされているなど直接テキストファイルとして編集できない

		public GhostModel Ghost { get; }

		//フルパス
		public string FullPath
		{
			get;
		}

		//純粋なファイル名
		public string Name
		{
			get => System.IO.Path.GetFileName(FullPath);
		}

		//辞書の相対パス
		public string RelativeName
		{
			get
			{
				var dictRootUri = new Uri(Ghost.FullDictionaryPath + "/") ;
				var dictUri = new Uri(FullPath);
				return dictRootUri.MakeRelativeUri(dictUri).ToString();
			}
		}

		public string Body
		{
			get => bodyAvailable ? body : null;
			protected set
			{
				bodyAvailable = true;
				body = value;
				NotifyChanged();
				NotifyChanged(nameof(BodyAvailable));
			}
		}

		public bool BodyAvailable
		{
			get => bodyAvailable;
			set
			{
				bodyAvailable = value;
				NotifyChanged();
				NotifyChanged(nameof(Body));
			}
		}

		public TextFileModel(GhostModel ghost, string fullPath)
		{
			Ghost = ghost;
			FullPath = fullPath;
			bodyAvailable = true;
			body = string.Empty;
			LoadFile();
		}

		protected virtual void LoadFile()
		{
			body = File.ReadAllText(FullPath, Constants.EncodingShiftJis);
			bodyAvailable = true;
		}
	}

	//里々の辞書ファイル
	public class DictionaryModel : TextFileModel
	{
		private ObservableCollection<EventModel> events;
		private bool isSerialized;							//リスト化解除されたか


		public ReadOnlyObservableCollection<EventModel> Events
		{
			get => new ReadOnlyObservableCollection<EventModel>(events);
		}

		public bool IsSerialized
		{
			get => isSerialized;
			set
			{
				if(isSerialized != value)
				{
					isSerialized = value;
					if(value)
					{
						Body = Serialize();
						BodyAvailable = true;
					}
					else
					{
						Deserialize(Body);
						BodyAvailable = false;
					}
				}
				NotifyChanged();
			}
		}

		public DictionaryModel(GhostModel ghost, string fullPath): base(ghost, fullPath)
		{
			events = new ObservableCollection<EventModel>();
			LoadDictionary();
		}

		protected override void LoadFile()
		{
			//nop
		}
		protected void LoadDictionary()
		{
			//TODO: エスケープやインラインなどややこしげなモノも読むようにする
			var text = File.ReadAllText(FullPath, Constants.EncodingShiftJis);
			Deserialize(text);
			isSerialized = false;
			BodyAvailable = false;
		}

		//新規でイベントを追加
		//どこにも追加されてない、完全新規のものである必要
		public void AddEvent(EventModel ev)
		{
			events.Add(ev);
		}

		//イベントの削除
		public void RemoveEvent(EventModel ev)
		{
			Debug.Assert(events.Contains(ev));
			events.Remove(ev);
		}


		//辞書のシリアライズ
		public string Serialize()
		{
			var serializedEvents = new List<string>();
			
			foreach(var ev in events)
			{
				serializedEvents.Add(ev.Serialize());
			}

			return string.Join(Constants.NewLine, serializedEvents);
		}

		public void Deserialize(string text)
		{
			var lines = text.Split(Constants.NewLineSeparator, StringSplitOptions.None);

			var eventLines = new List<string>();
			string eventName = null;
			string eventCondition = null;
			EventType eventType = EventType.Header;

			foreach (var line in lines)
			{
				EventType nextType;
				string nextName, nextCondition;
				if (DictionaryUtility.SplitEventHeader(line, out nextType, out nextName, out nextCondition))
				{
					//フラッシュ
					var ev = new EventModel(eventType, eventName, eventCondition, string.Join(Constants.NewLine, eventLines));
					events.Add(ev);

					eventType = nextType;
					eventName = nextName;
					eventCondition = nextCondition;
					eventLines.Clear();
				}
				else
				{
					eventLines.Add(line);
				}
			}
		}

	}

	//里々の項目１つ分
	public class EventModel : NotificationObject
	{
		private string name;
		private string condition;
		private string body;
		private EventType type;

		//項目名
		public string Name
		{
			get => name;
			set
			{
				name = value;
				NotifyChanged();
			}
		}

		//実行条件
		public string Condition
		{
			get => condition;
			set
			{
				condition = value;
				NotifyChanged();
			}
		}

		//本文
		public string Body
		{
			get => body;
			set
			{
				body = value;
				NotifyChanged();
				NotifyChanged(nameof(BodyPreview));
			}
		}

		//プレビュー表示用の改行をとりのぞいた本文
		public string BodyPreview
		{
			get => Body.Replace(Constants.NewLine, " ");
		}

		public EventType Type
		{
			get => type;
			set
			{
				type = value;
				NotifyChanged();
			}
		}

		//識別子を取得、表示名とも兼ねる？
		public string Identifier
		{
			get
			{
				switch (Type)
				{
					case EventType.Sentence:
						return Constants.SentenceHead + Name;
					case EventType.Word:
						return Constants.WordHead + Name;
					case EventType.Header:
						return "<辞書ヘッダ>";
				}
				throw new NotImplementedException();
			}
		}

		public EventModel(EventType type, string name, string condition, string body)
		{
			Type = type;
			Name = name;
			Condition = condition;
			Body = body;
		}

		//イベントのシリアライズ
		public string Serialize()
		{
			var header = DictionaryUtility.SerializeEventHeader(type, name, condition);
			if (header == null)
				return body;
			else
				return header + Constants.NewLine + body;

		}

	}

	

	
}
