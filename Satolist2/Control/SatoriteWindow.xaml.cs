using ICSharpCode.AvalonEdit.Document;
using Satolist2.Module.TextEditor;
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
using System.Windows.Threading;

namespace Satolist2.Control
{
	/// <summary>
	/// Satorite.xaml の相互作用ロジック
	/// </summary>
	public partial class SatoriteWindow : UserControl
	{
		public SatoriteWindow()
		{
			InitializeComponent();
		}
	}

	internal class SatoriteViewModel : NotificationObject, IDockingWindowContent, IControlBindedReceiver
	{
		private SatoriteWindow control;
		public const string ContentId = "Satorite";
		public string DockingTitle => "さとりて";

		public string DockingContentId => ContentId;

		public ActionCommand SendToGhostCommand { get; }
		public ActionCommand SendToGhostSelectionRangeCommand { get; }

		public SatoriteViewModel(MainViewModel main)
		{
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					try
					{
						Satorite.SendSatori(main.Ghost, control.MainTextEditor.MainTextEditor.Text, EventType.Sentence);
						Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
					}
					catch(GhostNotFoundException ex)
					{
						ex.PrintErrorLog();
					}
				},
				o => main.Ghost != null
				);

			SendToGhostSelectionRangeCommand = new ActionCommand(
				o =>
				{
					try
					{
						if (!string.IsNullOrEmpty(control.MainTextEditor.MainTextEditor.SelectionString))
						{
							Satorite.SendSatori(main.Ghost, control.MainTextEditor.MainTextEditor.SelectionString, EventType.Sentence);
							Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
						}
					}
					catch (GhostNotFoundException ex)
					{
						ex.PrintErrorLog();
					}
				});
		}

		public void ControlBind(System.Windows.Controls.Control control)
		{
			if(control is SatoriteWindow ctrl)
			{
				this.control = ctrl;
			}
		}
	}
}
