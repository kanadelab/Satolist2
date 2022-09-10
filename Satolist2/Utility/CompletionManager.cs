using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Satolist2.Utility
{
	//入力補助システム
	class CompletionManager
	{
		private static DrawingImage wordIcon;
		private static DrawingImage sentenceIcon;
		private static DrawingImage variableIcon;

		//メイン情報から抜き出す
		private MainViewModel Main { get; set; }

		static CompletionManager()
		{
			wordIcon = CreateIcon(Brushes.LightBlue, "M12,15C12.81,15 13.5,14.7 14.11,14.11C14.7,13.5 15,12.81 15,12C15,11.19 14.7,10.5 14.11,9.89C13.5,9.3 12.81,9 12,9C11.19,9 10.5,9.3 9.89,9.89C9.3,10.5 9,11.19 9,12C9,12.81 9.3,13.5 9.89,14.11C10.5,14.7 11.19,15 12,15M12,2C14.75,2 17.1,3 19.05,4.95C21,6.9 22,9.25 22,12V13.45C22,14.45 21.65,15.3 21,16C20.3,16.67 19.5,17 18.5,17C17.3,17 16.31,16.5 15.56,15.5C14.56,16.5 13.38,17 12,17C10.63,17 9.45,16.5 8.46,15.54C7.5,14.55 7,13.38 7,12C7,10.63 7.5,9.45 8.46,8.46C9.45,7.5 10.63,7 12,7C13.38,7 14.55,7.5 15.54,8.46C16.5,9.45 17,10.63 17,12V13.45C17,13.86 17.16,14.22 17.46,14.53C17.76,14.84 18.11,15 18.5,15C18.92,15 19.27,14.84 19.57,14.53C19.87,14.22 20,13.86 20,13.45V12C20,9.81 19.23,7.93 17.65,6.35C16.07,4.77 14.19,4 12,4C9.81,4 7.93,4.77 6.35,6.35C4.77,7.93 4,9.81 4,12C4,14.19 4.77,16.07 6.35,17.65C7.93,19.23 9.81,20 12,20H17V22H12C9.25,22 6.9,21 4.95,19.05C3,17.1 2,14.75 2,12C2,9.25 3,6.9 4.95,4.95C6.9,3 9.25,2 12,2Z");
			sentenceIcon = CreateIcon(Brushes.LightGreen, "M21 13H14.4L19.1 17.7L17.7 19.1L13 14.4V21H11V14.3L6.3 19L4.9 17.6L9.4 13H3V11H9.6L4.9 6.3L6.3 4.9L11 9.6V3H13V9.4L17.6 4.8L19 6.3L14.3 11H21V13Z");
			variableIcon = CreateIcon(Brushes.LightPink, "M7,15H9C9,16.08 10.37,17 12,17C13.63,17 15,16.08 15,15C15,13.9 13.96,13.5 11.76,12.97C9.64,12.44 7,11.78 7,9C7,7.21 8.47,5.69 10.5,5.18V3H13.5V5.18C15.53,5.69 17,7.21 17,9H15C15,7.92 13.63,7 12,7C10.37,7 9,7.92 9,9C9,10.1 10.04,10.5 12.24,11.03C14.36,11.56 17,12.22 17,15C17,16.79 15.53,18.31 13.5,18.82V21H10.5V18.82C8.47,18.31 7,16.79 7,15Z");
		}

		static DrawingImage CreateIcon(Brush brush, string geometry)
		{
			var geo = Geometry.Parse(geometry);
			var draw = new GeometryDrawing(brush, null, geo);
			var icon = new DrawingImage(draw);
			draw.Freeze();
			return icon;
		}

		public void RequestCompletion(TextEditor textEditor, MainViewModel main)
		{
			Main = main;
			CompletionWindow window = new CompletionWindow(textEditor.TextArea);
			window.Width = 300;
			foreach(var item in FilterCompletion())
				window.CompletionList.CompletionData.Add(item);
			window.Show();
		}

		private IEnumerable<ICompletionData> FilterCompletion()
		{
			Dictionary<string, EventCompletionData> items = new Dictionary<string, EventCompletionData>();

			//全部返してみるか
			foreach(var dic in Main.Ghost.Dictionaries)
			{
				if(dic.IsSatoriDictionary)
				{
					foreach(var ev in dic.InstantDeserializedEvents)
					{
						if (ev.Type == EventType.Header)
							continue;
						if (string.IsNullOrWhiteSpace(ev.Name))
							continue;

						if(!items.ContainsKey(ev.Name))
						{
							var data = new EventCompletionData(ev);
							items.Add(ev.Name, data);
							yield return data;
						}
						else
						{
							items[ev.Name].Count++;
						}
					}
				}
			}

			//変数
			foreach(var variable in Main.VariableListViewModel.Items)
			{
				yield return new VariableCompletionData(variable.Name, variable.Data);
			}
		}

		private class EventCompletionData : ICompletionData
		{
			private string serializedString;

			public EventModel Event { get; }
			public int Count { get; set; }
			
			public ImageSource Image
			{
				get
				{
					if (Event.Type == EventType.Sentence)
						return sentenceIcon;
					else if (Event.Type == EventType.Word)
						return wordIcon;
					return null;
				}
			}

			public string Text => Event.Name;

			public object Content => Event.Name;

			public object Description
			{
				get
				{
					if (serializedString == null)
					{
						serializedString = Event.Serialize();
						if (serializedString.Length > 128)
						{
							serializedString = serializedString.Substring(0, 128) + "......";
						}
						if (Count > 1)
						{
							serializedString = string.Concat("[", Count.ToString(), "個の項目]\r\n\r\n", serializedString);
						}
						serializedString = serializedString.TrimEnd(new char[] { '\r', '\n', ' ', '　' });
					}
					return serializedString;
				}
			}

			public double Priority => 0.0;

			public EventCompletionData(EventModel ev)
			{
				Event = ev;
				Count = 1;
			}

			public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
			{
				textArea.Document.Replace(completionSegment, Text);
			}
		}

		private class VariableCompletionData : ICompletionData
		{
			public string InitialValue { get; }
			public ImageSource Image => variableIcon;

			public string Text { get; }

			public object Content => Text;

			public object Description => string.Concat("初期値: ", InitialValue);

			public double Priority => 0.0;

			public VariableCompletionData(string name, string initialValue)
			{
				Text = name;
				InitialValue = initialValue;
			}

			public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
			{
				textArea.Document.Replace(completionSegment, Text);
			}
		}

	}
}
