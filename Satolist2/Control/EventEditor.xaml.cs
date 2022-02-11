using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Satolist2.Control
{
	/// <summary>
	/// EventEditor.xaml の相互作用ロジック
	/// </summary>
	public partial class EventEditor : UserControl
	{
		public EventEditor()
		{
			InitializeComponent();
		}
	}

	internal struct EventTypeComboBoxItem
	{
		public string Name
		{
			get;set;
		}

		public EventType Type
		{
			get;set;
		}
	}

	internal class EventEditorViewModel : NotificationObject, IDockingWindowContent
	{
		public EventModel Event { get; }
		public string randomizedContentId;
		public static readonly EventTypeComboBoxItem[] eventTypeList =
		{
			new EventTypeComboBoxItem(){Name="文(" + Constants.SentenceHead +")", Type=EventType.Sentence  },
			new EventTypeComboBoxItem(){Name="単語群(" + Constants.WordHead+")", Type=EventType.Word }
		};

		public EventTypeComboBoxItem[] EventTypeList
		{
			get => eventTypeList;
		}

		public string DockingTitle => Event.Name;

		public string DockingContentId => randomizedContentId;

		public EventEditorViewModel(EventModel ev)
		{
			randomizedContentId = Guid.NewGuid().ToString();	//複数出現するのでユニークなIDを振る
			Event = ev;
			Event.PropertyChanged += Event_PropertyChanged;
		}

		private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if(e.PropertyName == nameof(EventModel.Name))
				NotifyChanged(nameof(DockingTitle));
		}
	}
}
