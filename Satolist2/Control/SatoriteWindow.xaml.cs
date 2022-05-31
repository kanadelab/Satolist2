using ICSharpCode.AvalonEdit.Document;
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

		public void UpdateHilighting()
		{
			MainTextEditor.SyntaxHighlighting = null;
			MainTextEditor.Dispatcher.BeginInvoke(new Action(() =>
			{
				var hilighter = new SatoriSyntaxHilighter();
				MainTextEditor.SyntaxHighlighting = hilighter;
				MainTextEditor.Foreground = hilighter.MainForegroundColor;
			}
			), DispatcherPriority.Render);
		}

		public void UpdateFontSettings()
		{
			if (!string.IsNullOrEmpty(MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName) && MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize > 0)
			{
				MainTextEditor.TextArea.FontFamily = new FontFamily(MainViewModel.EditorSettings.GeneralSettings.TextEditorFontName);
				MainTextEditor.TextArea.FontSize = MainViewModel.EditorSettings.GeneralSettings.TextEditorFontSize;
			}
		}
	}

	internal class SatoriteViewModel : NotificationObject, IDockingWindowContent
	{
		public const string ContentId = "Satorite";
		public TextDocument Document { get; }
		public string DockingTitle => "さとりて";

		public string DockingContentId => ContentId;

		public ActionCommand SendToGhostCommand { get; }

		public SatoriteViewModel(MainViewModel main)
		{
			Document = new TextDocument();
			SendToGhostCommand = new ActionCommand(
				o =>
				{
					Satorite.SendSatori(main.Ghost, Document.Text, EventType.Sentence);
					Core.LogMessage.AddLog("ゴーストにトークを送信しました。");
				},
				o => main.Ghost != null
				);
		}
	}
}
