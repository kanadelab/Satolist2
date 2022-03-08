using Satolist2.Dialog;
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
	public class TextFileModel : NotificationObject, ISaveFileObject
	{
		private string body;
		private bool bodyAvailable; //false ならbodyは無効。里々辞書形式でデシリアライズされているなど直接テキストファイルとして編集できない
		private bool isChanged;		//変更検出

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
			get => DictionaryUtility.MakeRelativePath(Ghost.FullDictionaryPath, FullPath);
		}

		public string Body
		{
			get => bodyAvailable ? body : null;
			set
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

		public bool IsChanged
		{
			get => isChanged;
			protected set
			{
				isChanged = value;
				NotifyChanged();
			}
		}

		public string SaveFilePath => RelativeName;

		public TextFileModel(GhostModel ghost, string fullPath)
		{
			Ghost = ghost;
			FullPath = fullPath;
			bodyAvailable = true;
			body = string.Empty;
			if(!string.IsNullOrEmpty(fullPath))
				LoadFile();
		}

		protected virtual void LoadFile()
		{
			body = File.ReadAllText(FullPath, Constants.EncodingShiftJis);
			bodyAvailable = true;
		}

		public void Save()
		{
			throw new NotImplementedException();
		}
	}

	//里々の辞書ファイル
	public class DictionaryModel : TextFileModel
	{
		private ObservableCollection<EventModel> events;
		private bool isSerialized;                          //リスト化解除されたか

		public bool IsInlineEventAnalyze { get; set; }

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

						//イベントを消す
						events.Clear();
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

		//ゴーストの辞書をロードする用
		public DictionaryModel(GhostModel ghost, string fullPath): base(ghost, fullPath)
		{
			events = new ObservableCollection<EventModel>();
			LoadDictionary();
		}

		public DictionaryModel() : base(null, null)
		{
			events = new ObservableCollection<EventModel>();
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

		//イベントを追加
		//すでにどこかに所属している場合は所属が外れる
		public void AddEvent(EventModel ev)
		{
			if(ev.Dictionary != null)
			{
				//削除イベントは発生しない
				//ObservableCollectionからは外れる
				RemoveEventInternal(ev);
			}

			events.Add(ev);
			ev.Dictionary = this;
		}

		//イベントの削除
		public void RemoveEvent(EventModel ev)
		{
			Debug.Assert(events.Contains(ev));
			if (events.Contains(ev))
			{
				ev.RaiseRemoveEvent();
				ev.Dictionary = null;
				RemoveEventInternal(ev);
			}
		}

		//内部的なイベントの削除
		private void RemoveEventInternal(EventModel ev)
		{
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

			//1行あける
			return string.Join(Constants.NewLine, serializedEvents);
		}

		public void Deserialize(string text)
		{
			var lines = text.Split(Constants.NewLineSeparator, StringSplitOptions.None);

			var eventLines = new List<string>();
			string eventName = null;
			string eventCondition = null;
			EventType eventType = EventType.Header;

			bool inlineEvent = false;
			List<string> escapedLines = new List<string>();

			foreach (var line in lines)
			{
				//NOTE: エスケープ文字を解釈するかどうか悩ましいところ。１行に解釈まとめられないのでそのまま読んでしまう。
				EventType nextType;
				string nextName, nextCondition;
				if (DictionaryUtility.SplitEventHeader(line, out nextType, out nextName, out nextCondition) && !inlineEvent)
				{
					//フラッシュ
					var ev = new EventModel(eventType, eventName, eventCondition, string.Join(Constants.NewLine, eventLines), IsInlineEventAnalyze);
					AddEvent(ev);

					eventType = nextType;
					eventName = nextName;
					eventCondition = nextCondition;
					eventLines.Clear();
				}
				else
				{
					inlineEvent = false;
					if (line == Constants.InlineEventSeparator)	//インラインイベントを検出したら次の行はフラッシュせず同一の中に続ける
						inlineEvent = true;
					else
						eventLines.Add(line);
				}
			}

			var lastEvent = new EventModel(eventType, eventName, eventCondition, string.Join(Constants.NewLine, eventLines), IsInlineEventAnalyze);
			AddEvent(lastEvent);
		}

		//変更済みとしてマーク
		public void MarkChanged()
		{
			IsChanged = true;
		}

	}

	//里々の項目１つ分
	public class EventModel : NotificationObject
	{
		private bool initialized;
		private DictionaryModel dictionary;
		private string name;
		private string condition;
		private string body;
		private EventType type;
		private bool disabled;
		private bool isInlineEvent;

		private ObservableCollection<InlineEventModel> inlineEvents = new ObservableCollection<InlineEventModel>();

		//項目名
		public string Name
		{
			get => name;
			set
			{
				if (name != value)
					MarkChanged();
				name = value;
				NotifyChanged();
				NotifyChanged(nameof(Identifier));
			}
		}

		//実行条件
		public string Condition
		{
			get => condition;
			set
			{
				if (condition != value)
					MarkChanged();
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
				if (body != value)
					MarkChanged();
				body = value;

				//インラインイベントはインラインイベントをネストしない
				if (!isInlineEvent)
				{
					//Bodyが変わったタイミングでインラインイベントの解析を走らせる
					DictionaryModel tempDict = new DictionaryModel();
					tempDict.IsInlineEventAnalyze = true;
					tempDict.Deserialize(Body);

					List<InlineEventModel> newInlineEvents = new List<InlineEventModel>();
					foreach (var ev in tempDict.Events)
					{
						if(ev.type != EventType.Header)
							newInlineEvents.Add(new InlineEventModel(this, ev));
					}

					//差分を作る
					List<InlineEventModel> removedEvents = new List<InlineEventModel>();

					//一致するものを除去して余ったものが差分
					foreach (var item in inlineEvents)
					{
						var same = newInlineEvents.FirstOrDefault(o => o.Equals(item));
						if (same != null)
						{
							//内容を更新
							item.InlineEvent.Body = same.InlineEvent.Body;
							item.InlineEvent.Condition = same.InlineEvent.Condition;

							//重複を取り除く
							newInlineEvents.Remove(same);
						}
						else
						{
							removedEvents.Add(item);
						}
					}

					//removedを取り除く
					foreach (var r in removedEvents)
						inlineEvents.Remove(r);

					//新規を追加
					foreach (var r in newInlineEvents)
						inlineEvents.Add(r);
				}

				NotifyChanged();
				NotifyChanged(nameof(BodyPreview));
			}
		}

		//さとりすとによって無効化されているか
		public bool Disabled
		{
			get => disabled;
			set
			{
				if (disabled != value)
					MarkChanged();
				disabled = value;
				NotifyChanged();
			}
		}

		public DictionaryModel Dictionary
		{
			get => dictionary;
			internal set
			{
				dictionary = value;
				NotifyChanged();
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
				if (type != value)
					MarkChanged();
				type = value;
				NotifyChanged();
				NotifyChanged(nameof(Identifier));
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

		public ReadOnlyObservableCollection<InlineEventModel> InlineEvents
		{
			get => new ReadOnlyObservableCollection<InlineEventModel>(inlineEvents);
		}

		public event Action<EventModel> OnRemove;

		public EventModel(EventType type, string name, string condition, string body, bool isInline = false)
		{
			isInlineEvent = isInline;
			Type = type;
			Name = name;
			Condition = condition;
			Body = body;
			initialized = true;
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

		//イベントを削除
		public void RaiseRemoveEvent()
		{
			OnRemove?.Invoke(this);
		}

		//変更済みとしてマーク
		private void MarkChanged()
		{
			if(initialized)	//初期化ステップでは無視
			{
				Dictionary?.MarkChanged();
			}
		}
	}

	//インラインイベント(イベントエディタ内に記述するイベント)のモデル
	public class InlineEventModel
	{
		public EventModel ParentEvent { get; }
		public EventModel InlineEvent { get; }

		public string Identifier
		{
			get => InlineEvent.Identifier;
		}
		
		public InlineEventModel(EventModel parentEvent, EventModel inlineEvent)
		{
			ParentEvent = parentEvent;
			InlineEvent = inlineEvent;
		}

		public bool Equals(InlineEventModel ev)
		{
			return ReferenceEquals(ParentEvent, ev.ParentEvent) && ev.Identifier == Identifier;
		}
	}

	

	
}
