using AngleSharp.Common;
using Satolist2.Dialog;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.ServiceModel.Channels;
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
		
		public SatoriBootConfModel BootConf
		{
			get; private set;
		}

		//ゴーストのフルパス（readme.txt 等のフル階層）
		public string FullPath
		{
			get => fullPath;
		}

		public string FullDictionaryPath
		{
			get => DictionaryUtility.ConbinePath(FullPath, "ghost/master");
		}

		public string FullGhostDescriptPath
		{
			get => DictionaryUtility.ConbinePath(FullDictionaryPath, "descript.txt");
		}

		public string GhostDescriptSakuraName
		{
			get; private set;
		}

		public string GhostDescriptName
		{
			get; private set;
		}

		public string GhostIconPath
		{
			get; private set;
		}

		public string FullGhostIconPath
		{
			get => DictionaryUtility.ConbinePath(FullDictionaryPath, GhostIconPath);
		}

		//ゴーストが持っている辞書データ
		public CustomizedReadOnlyObservableCollection<DictionaryModel> Dictionaries
		{
			get => new CustomizedReadOnlyObservableCollection<DictionaryModel>(dictionaries);
		}

		public GhostModel(string path)
		{
			fullPath = DictionaryUtility.NormalizeFullPath(Path.GetFullPath(path));
			dictionaries = new ObservableCollection<DictionaryModel>();

			//bootconfを最初にロード
			BootConf = new SatoriBootConfModel();
			BootConf.Load(DictionaryUtility.ConbinePath(FullDictionaryPath, "satori_bootconf.txt"));

			//読み込まない設定ファイル
			var ignoreDictionaryFiles = new HashSet<string>();
			ignoreDictionaryFiles.Add("descript.txt");
			ignoreDictionaryFiles.Add("var.txt");
			ignoreDictionaryFiles.Add("replace.txt");
			ignoreDictionaryFiles.Add("replace_after.txt");
			ignoreDictionaryFiles.Add("satori_license.txt");
			ignoreDictionaryFiles.Add("updates.txt");
			ignoreDictionaryFiles.Add("satori_conf.txt");
			ignoreDictionaryFiles.Add("satori_savedata.txt");
			ignoreDictionaryFiles.Add("satori_savebackup.txt");
			ignoreDictionaryFiles.Add("ssp_shiori_log.txt");

			//ファイル巡回
			var files = Directory.GetFiles(FullDictionaryPath, "*.*", SearchOption.AllDirectories).ToList();
			files.Sort();
			
			foreach(var f in files)
			{
				if(Regex.IsMatch(Path.GetFileName(f), "^dic.*\\.txt$"))
				{
					var dict = new DictionaryModel(this, Path.GetFullPath(f));
					dict.LoadDictionary();
					dictionaries.Add(dict);
				}
				else if((
					Regex.IsMatch(Path.GetFileName(f), "\\.txt$") ||
					Regex.IsMatch(Path.GetFileName(f), "\\.dic$") 
					) && !ignoreDictionaryFiles.Contains(Path.GetFileName(f)))
				{
					var dict = new DictionaryModel(this, Path.GetFullPath(f));
					dict.LoadDictionary();
					dictionaries.Add(dict);
				}
			}

			//descriptから基本情報を得る
			var tempDescript = new CsvBuilder();
			tempDescript.Deserialize(DictionaryUtility.ReadAllTextWithDescriptStyleCharset(FullGhostDescriptPath));
			GhostDescriptSakuraName = tempDescript.GetValue("sakura.name");
			GhostDescriptName = tempDescript.GetValue("name");
			GhostIconPath = tempDescript.GetValue("icon");
		}

		public DictionaryModel AddNewDictionary(string filename)
		{
			var newDictionary = new Model.DictionaryModel(this, DictionaryUtility.NormalizePath(filename));
			newDictionary.IsSerialized = MainViewModel.EditorSettings.GeneralSettings.IsTextModeDefault;    //デフォルト設定のモードにする
			newDictionary.Changed();
			dictionaries.Add(newDictionary);
			return newDictionary;
		}

		public void DeleteDictionary(DictionaryModel dic)
		{
			dictionaries.Remove(dic);
			dic.Deleted();

			//ファイル実体もこの場で削除する
			DictionaryUtility.RecycleFile(dic.FullPath);
		}
	}

	//テキストファイルデータ
	//里々の辞書ファイルとそれ以外と別ファイルにしようと思ったけど… 同じ方が都合が良さそうだったので意味がなかった…
	public class DictionaryModel : NotificationObject, ISaveFileObject
	{
		private string fullPath;
		private string body;
		private bool isChanged;														//変更検出
		private ObservableCollection<EventModel> events;							//リスト化された辞書の内容一覧
		private List<EventModel> instantDeserializedEvents;							//リスト化解除されているが、簡易解析された一覧
		private Dictionary<string, List<EventModel>> internalEventMap;				//リスト化情報または簡易解析のマップ情報
		private bool isSerialized;													//リスト化解除されたか
		private DateTime lastUpdateTime;											//最終更新日時(UTC,大小比較目的なので問題ないはず)

		public GhostModel Ghost { get; }
		public bool IsInlineEventAnalyze { get; set; }
		public string SaveFilePath => RelativeName;

		public EditorLoadState LoadState { get; set; }

		//削除通知
		public event Action<DictionaryModel> OnDelete;

		//フルパス
		public string FullPath
		{
			get => fullPath;
			private set
			{
				fullPath = value;
				NotifyChanged();
				NotifyChanged(nameof(Name));
				NotifyChanged(nameof(RelativeName));
				NotifyChanged(nameof(IsSatoriDictionary));
			}
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

		//ファイル名が里々の辞書ファイルかどうか
		public bool IsSatoriDictionary
		{
			get => DictionaryUtility.IsSatoriDictionaryName(FullPath);
		}

		//ファイルがプレーンテキストとして扱われているか
		public string Body
		{
			get => isSerialized ? body : null;
			set
			{
				body = value;
				ClearEvent();
				Changed();
				NotifyChanged();

				if (!isSerialized)
				{
					isSerialized = true;
					NotifyChanged(nameof(IsSerialized));
				}

				//解析データを無効にする
				instantDeserializedEvents = null;
				internalEventMap.Clear();
			}
		}

		//変更があるか
		public bool IsChanged
		{
			get => isChanged;
			protected set
			{
				isChanged = value;
				NotifyChanged();
			}
		}

		//最終更新日時
		public DateTime LastUpdateTime => lastUpdateTime;

		public DictionaryModel(GhostModel ghost, string fullPath)
		{
			lastUpdateTime = DateTime.UtcNow;			//新規作成を想定して現在時を設定
			LoadState = EditorLoadState.Initialized;
			Ghost = ghost;
			FullPath = fullPath;
			isSerialized = true;
			body = string.Empty;
			events = new ObservableCollection<EventModel>();
			internalEventMap = new Dictionary<string, List<EventModel>>();
			OnDelete += DictionaryModel_OnDelete;
		}


		public DictionaryModel()
		{
			lastUpdateTime = DateTime.UtcNow;			//新規作成を想定して現在時を設定
			LoadState = EditorLoadState.Initialized;
			Ghost = null;
			FullPath = null;
			isSerialized = true;
			body = string.Empty;

			events = new ObservableCollection<EventModel>();
			internalEventMap = new Dictionary<string, List<EventModel>>();
			OnDelete += DictionaryModel_OnDelete;
		}


		//変更としてマーク
		public void Changed()
		{
			IsChanged = true;
		}

		//削除イベント
		internal void Deleted()
		{
			OnDelete?.Invoke(this);
		}

		//リネーム
		public virtual void Rename(string fullName)
		{
			var oldName = FullPath;
			try
			{
				//ファイルを移動
				//新規作成モードなど実体がない場合はエラーになるので許容する
				File.Copy(FullPath, fullName, true);
			}
			catch { }

			FullPath = fullName;
			DictionaryUtility.RecycleFile(oldName);

			//里々じゃなければリスト解除を強制する
			if (!DictionaryUtility.IsSatoriDictionaryName(Name))
			{
				IsSerialized = true;

				//解析データを無効にする
				instantDeserializedEvents = null;
				internalEventMap.Clear();
			}
		}
	
		public CustomizedReadOnlyObservableCollection<EventModel> Events
		{
			get => new CustomizedReadOnlyObservableCollection<EventModel>(events);
		}

		//解析用の一時的なデータを取得
		public IEnumerable<EventModel> InstantDeserializedEvents
		{
			get
			{
				if (IsSatoriDictionary)
				{
					if (isSerialized)
					{
						if (instantDeserializedEvents == null)
							InstantDeserialize();
						return instantDeserializedEvents;
					}
					else
					{
						return Events;
					}
				}
				else
				{
					return Array.Empty<EventModel>();
				}
			}
		}

		//イベント名とイベントの対応リスト
		public ReadOnlyDictionary<string, List<EventModel>> EventNameMap
		{
			get
			{
				if (IsSatoriDictionary)
				{
					if (isSerialized)
					{
						if(instantDeserializedEvents == null)
							InstantDeserialize();
					}
					return new ReadOnlyDictionary<string, List<EventModel>>(internalEventMap);
				}
				else
				{
					//カラを返す
					return new ReadOnlyDictionary<string, List<EventModel>>(new Dictionary<string, List<EventModel>>());
				}
			}
		}


		//リスト化解除されているか
		public bool IsSerialized
		{
			get => isSerialized;
			set
			{
				//里々辞書以外はデシリアライズ不可
				if (!IsSatoriDictionary && !value)
					return;

				if (isSerialized != value)
				{
					bool changed = IsChanged;
					isSerialized = value;
					if(value)
					{
						Body = Serialize();

						//イベントを消す
						ClearEvent();
					}
					else
					{
						Deserialize(body);
					}
					IsChanged = changed;    //この操作だけでは編集ステータスは変わらない
				}
				NotifyChanged();
			}
		}

		private void DictionaryModel_OnDelete(DictionaryModel obj)
		{
			//ファイルが削除扱いにされた。
			//リストモードであればイベントの除去をまとめて行い、リスト系から表示を消す。
			if(!IsSerialized)
				ClearEvent();
		}

		public void LoadDictionary()
		{
			//ファイルをロード
			var text = File.ReadAllText(FullPath, Ghost.BootConf.DicEncoding);
			if (!MainViewModel.EditorSettings.GeneralSettings.IsTextModeDefault && IsSatoriDictionary)
			{
				//リストモード
				Deserialize(text);
				isSerialized = false;
			}
			else
			{
				//テキストモード
				Body = text;
				IsChanged = false;
				isSerialized = true;
			}

			//ソートのために更新日時を取得しておく
			lastUpdateTime = File.GetLastWriteTimeUtc(FullPath);
		}

		//イベントを追加
		//すでにどこかに所属している場合は所属が外れる
		public void AddEvent(EventModel ev)
		{
			if(ev.Dictionary != null)
			{
				//削除イベントは発生しない
				//ObservableCollectionからは外れる
				DetachEvent(ev);
			}
			AddEventInternal(ev);
			ev.Dictionary = this;
			Changed();
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

		//イベントのデタッチ(削除イベントを発しないが、dictionaryからイベントを切り離す)
		public void DetachEvent(EventModel ev)
		{
			Debug.Assert(events.Contains(ev));
			if(events.Contains(ev))
			{
				ev.Dictionary = null;
				RemoveEventInternal(ev);
			}
		}

		//イベントの全削除。削除イベントを使うのでevent.Clear直接触らない
		public void ClearEvent()
		{
			foreach(var ev in events)
			{
				ev.RaiseRemoveEvent();
				ev.NameChanged -= Ev_NameChanged;
				ev.Dictionary = null;
			}
			events.Clear();
			internalEventMap.Clear();
		}

		//イベントの順番変更
		public void MoveIndexEvent(string identifier, string beforeIdentifier)
		{
			{
				if (!events.Any(o => o.Identifier == identifier))
					return;
				if (!events.Any(o => o.Identifier == beforeIdentifier))
					return;

				//位置が変わらないなら無効
				var ids = events.Select(o => o.Identifier).Distinct().ToList();
				if (ids.IndexOf(identifier) == ids.IndexOf(beforeIdentifier) + 1)
					return;
			}

			var moves = events.Where(o => o.Identifier == identifier).ToArray();

			//一時的に外すだけ
			foreach(var item in moves)
			{
				events.Remove(item);
			}

			//挿入位置を決定
			var beforeItem = events.FirstOrDefault(o => o.Identifier == beforeIdentifier);
			var insertIndex = events.IndexOf(beforeItem) + 1;
			
			foreach(var item in moves.Reverse())
			{
				events.Insert(insertIndex, item);
			}

			Changed();
		}

		//存在確認
		public bool Containts(string identifier)
		{
			return events.Any(o => o.Identifier == identifier);
		}

		//内部的なイベントの削除
		private void RemoveEventInternal(EventModel ev)
		{
			events.Remove(ev);
			AddEventMap(ev.Name, ev);
			ev.NameChanged -= Ev_NameChanged;
			Changed();
		}

		//内部的なイベントの追加
		private void AddEventInternal(EventModel ev)
		{
			events.Add(ev);
			AddEventMap(ev.Name, ev);
			ev.NameChanged += Ev_NameChanged;
		}

		private void Ev_NameChanged(object sender, EventNameChangeArgs e)
		{
			RemoveEventMap(e.OldName, (EventModel)sender);
			AddEventMap(e.NewName, (EventModel)sender);
		}

		//参照マップ追加
		private void AddEventMap(string name, EventModel ev)
		{
			List<EventModel> items;
			if(!internalEventMap.TryGetValue(name, out items))
			{
				items = new List<EventModel>();
				internalEventMap.Add(name, items);
			}
			items.Add(ev);
		}

		//参照マップ除去
		private void RemoveEventMap(string name, EventModel ev)
		{
			if(internalEventMap.TryGetValue(name, out var items))
			{
				items.Remove(ev);
				if (items.Any())
					internalEventMap.Remove(name);
			}
		}


		//辞書のシリアライズ
		public string Serialize()
		{
			bool isChanged = IsChanged;
			var serializedEvents = new List<string>();
			
			foreach(var ev in events)
			{
				//辞書ヘッダがカラの場合は無視
				if (ev.Type == EventType.Header && string.IsNullOrEmpty(ev.Body))
					continue;

				//イベント本体
				serializedEvents.Add(ev.Serialize());

				//区切りの空行追加
				for (int i = 0; i < MainViewModel.EditorSettings.GeneralSettings.ListedDictionaryInsertEmptyLineCount; i++)
					serializedEvents.Add(string.Empty);
			}

			//isChangedの状態を復元
			IsChanged = isChanged;

			return string.Join(Constants.NewLine, serializedEvents);
		}

		private static string TruncateDisableMark(string name, out bool isDisabled)
		{
			if (string.IsNullOrEmpty(name))
			{
				//名前がない。多分辞書ヘッダ
				isDisabled = false;
				return name;
			}

			var effectiveName = name;
			if (effectiveName.IndexOf(Constants.DisabledEventMark) == 0)
			{
				effectiveName = effectiveName.Substring(Constants.DisabledEventMark.Length);
				isDisabled = true;
				return effectiveName;
			}
			isDisabled = false;
			return effectiveName;
		}

		public void Deserialize(string text)
		{
			bool isChanged = IsChanged;
			internalEventMap.Clear();

			DeserializeInternal(text, (ev) => AddEvent(ev));

			//isChangedの状態を復元
			IsChanged = isChanged;
		}

		public void DeserializeInternal(string text, Action<EventModel> addEventAction)
		{
			var lines = text.Split(Constants.NewLineSeparator, StringSplitOptions.None);
			var eventLines = new List<string>();
			string eventName = null;
			string eventCondition = null;
			int eventLineIndex = 0;
			int eventLastEmptyLineCount = 0;			//末尾の空白行。保存設定で自動挿入する文、読み込み時は捨てるためのもの
			EventType eventType = EventType.Header;
			bool eventLineEscape = false;

			bool inlineEvent = false;
			for(var index = 0; index < lines.Length; index++)
			{
				var line = lines[index];

				//NOTE: エスケープ文字を解釈するかどうか悩ましいところ。１行に解釈まとめられないのでそのまま読んでしまう。
				EventType nextType;
				string nextName, nextCondition;
				if (DictionaryUtility.SplitEventHeader(line, out nextType, out nextName, out nextCondition) && !inlineEvent && !eventLineEscape)
				{
					//設定値だけ末尾の空白行をとりのぞく
					{
						int removeCount = Math.Min(eventLastEmptyLineCount, MainViewModel.EditorSettings.GeneralSettings.ListedDictionaryInsertEmptyLineCount);
						if (removeCount > 0)
							eventLines.RemoveRange(eventLines.Count - removeCount, removeCount);
					}

					//フラッシュ
					bool isDisabled;
					eventName = TruncateDisableMark(eventName, out isDisabled);
					var ev = new EventModel(eventType, eventName, eventCondition, string.Join(Constants.NewLine, eventLines), IsInlineEventAnalyze, eventLineIndex);
					ev.Disabled = isDisabled;
					addEventAction(ev);

					eventLastEmptyLineCount = 0;
					eventLineIndex = index + 1;
					eventType = nextType;
					eventName = nextName;
					eventCondition = nextCondition;
					eventLines.Clear();
				}
				else
				{
					eventLineEscape = false;
					inlineEvent = false;
					if (line == Constants.InlineEventSeparator) //インラインイベントを検出したら次の行はフラッシュせず同一の中に続ける
						inlineEvent = true;
					else
					{
						if (string.IsNullOrEmpty(line))
						{
							//末尾の空白行画の数を数える
							eventLastEmptyLineCount++;
						}
						else
						{
							//末尾ではなかったのでカウンタをリセット
							eventLastEmptyLineCount = 0;

							//行末エスケープか確認
							var match = Constants.LineEndEscapePattern.Match(line);
							if(match.Success)
							{
								//エスケープ文字が奇数個ならば行末エスケープされている
								if (match.Groups[1].Length % 2 == 1)
									eventLineEscape = true;
							}
						}

						eventLines.Add(line);
					}
				}
			}

			//設定値だけ末尾の空白行をとりのぞく
			{
				int removeCount = Math.Min(eventLastEmptyLineCount, MainViewModel.EditorSettings.GeneralSettings.ListedDictionaryInsertEmptyLineCount);
				if (removeCount > 0)
					eventLines.RemoveRange(eventLines.Count - removeCount, removeCount);
			}

			//最後のイベントをフラッシュ
			{
				bool isDisabled;
				eventName = TruncateDisableMark(eventName, out isDisabled);
				var lastEvent = new EventModel(eventType, eventName, eventCondition, string.Join(Constants.NewLine, eventLines), IsInlineEventAnalyze, eventLineIndex);
				lastEvent.Disabled = isDisabled;
				addEventAction(lastEvent);
			}
		}

		//検索等の目的での内部的なリスト化解除
		private void InstantDeserialize()
		{
			instantDeserializedEvents = new List<EventModel>();
			DeserializeInternal(Body, (ev) =>
			{
				//Dictionaryを設定するタイミングがないのでとりあえずここでやっちゃう
				ev.Dictionary = this;
				instantDeserializedEvents.Add(ev);
			});

			internalEventMap.Clear();
			foreach(var ev in instantDeserializedEvents)
			{
				AddEventMap(ev.Name, ev);
			}
		}

		//変更済みとしてマーク
		public void MarkChanged()
		{
			IsChanged = true;
		}

		public bool Save()
		{
			try
			{
				string saveText;
				if (IsSerialized)
				{
					//保存
					saveText = Body;
				}
				else
				{
					//シリアライズして保存
					saveText = Serialize();
				}
				File.WriteAllText(FullPath, saveText, Ghost.BootConf.DicEncoding);
				lastUpdateTime = File.GetLastWriteTimeUtc(FullPath);
				IsChanged = false;
				return true;
			}
			catch
			{
				return false;
			}
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
		private int analyzeLineIndex;

		private ObservableCollection<InlineEventModel> inlineEvents = new ObservableCollection<InlineEventModel>();
		public event EventHandler<EventNameChangeArgs> NameChanged;

		//項目名
		public string Name
		{
			get => name ?? string.Empty;
			set
			{
				var oldName = name;
				if (name != value)
					MarkChanged();
				name = value;
				NotifyChanged();
				NotifyChanged(nameof(Identifier));
				NameChanged?.Invoke(this, new EventNameChangeArgs(name, oldName));
			}
		}

		//実行条件
		public string Condition
		{
			get => condition ?? string.Empty;
			set
			{
				if (condition != value)
					MarkChanged();
				condition = value;
				NotifyChanged();
			}
		}

		//インラインかどうか
		public bool IsInlineEvent
		{
			get => isInlineEvent;
		}

		//解析時の行番号（編集中にずれるので、あくまでも解析時の位置）
		public int AnalyzeLineIndex
		{
			get => analyzeLineIndex;
		}
		//本文
		public string Body
		{
			get => body ?? string.Empty;
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

		public CustomizedReadOnlyObservableCollection<InlineEventModel> InlineEvents
		{
			get => new CustomizedReadOnlyObservableCollection<InlineEventModel>(inlineEvents);
		}

		public event Action<EventModel> OnRemove;

		public EventModel(EventType type, string name, string condition, string body, bool isInline = false, int analyzeLineIndex = 0)
		{
			this.analyzeLineIndex = analyzeLineIndex;
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
			var header = DictionaryUtility.SerializeEventHeader(type, name, condition, disabled);

			//インラインイベントを検出する
			var lines = DictionaryUtility.SplitLines(body);
			var serializeLines = new List<string>();

			foreach(var line in lines)
			{
				if (line.IndexOf(Constants.SentenceHead) == 0 || line.IndexOf(Constants.WordHead) == 0)
					serializeLines.Add(Constants.InlineEventSeparator);
				serializeLines.Add(line);
			}

			var serializedBody = DictionaryUtility.JoinLines(serializeLines);
			if (header == null)
				return serializedBody;
			else
				return header + Constants.NewLine + serializedBody;

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

		//イベントを削除
		public void Remove()
		{
			dictionary.RemoveEvent(this);
		}

		//イベントを別の辞書に移動
		public void MoveTo(DictionaryModel target)
		{
			dictionary.DetachEvent(this);
			target.AddEvent(this);
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

	/// <summary>
	/// 項目名の変更通知
	/// </summary>
	public class EventNameChangeArgs : EventArgs
	{
		public string NewName { get; set; }
		public string OldName { get; set; }

		public EventNameChangeArgs(string newName, string oldName)
		{
			NewName = newName;
			OldName = oldName;
		}
	}
	

	
}
