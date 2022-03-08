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

namespace Satolist2.Dialog
{
	/// <summary>
	/// TextEditorInsertPaletteSettingsDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class TextEditorInsertPaletteSettingsDialog : UserControl
	{
		public TextEditorInsertPaletteSettingsDialog()
		{
			InitializeComponent();
		}
	}

	internal class TextEditorInsertPaletteSettingsDialogViewModel : NotificationObject
	{

	}

	internal class TextEditorInsertPaletteSettingsDialogItemViewModel : NotificationObject
	{
		public enum NodeType
		{
			Item,
			Group
		}

		private string label;
		private string body;
		private int shortcutKeyNumber;
		private bool shortcutKeyCtrl;
		private bool shortcutKeyAlt;
		private bool shortcutKeyShift;

		public NodeType Type { get; private set; }
		public string Label
		{
			get => label;
			set
			{
				label = value;
				NotifyChanged();
			}
		}

		public string Body
		{
			get => body;
			set
			{
				body = value;
				NotifyChanged();
			}
		}

		public int ShortcutKeyNumber
		{
			get => shortcutKeyNumber;
			set
			{
				shortcutKeyNumber = value;
				NotifyChanged();
			}
		}

		public bool ShortcutKeyCtrl
		{
			get => shortcutKeyCtrl;
			set
			{
				shortcutKeyCtrl = value;
				NotifyChanged();
			}
		}

		public bool ShortcutKeyAlt
		{
			get => ShortcutKeyAlt;
			set
			{
				shortcutKeyAlt = value;
				NotifyChanged();
			}
		}

		public bool ShortcutKeyShift
		{
			get => shortcutKeyShift;
			set
			{
				shortcutKeyShift = value;
				NotifyChanged();
			}
		}

	}
}
