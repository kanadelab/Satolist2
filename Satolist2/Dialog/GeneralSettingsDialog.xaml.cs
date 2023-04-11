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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Satolist2.Dialog
{
	/// <summary>
	/// GeneralSettingsDialog.xaml の相互作用ロジック
	/// </summary>
	public partial class GeneralSettingsDialog : DialogContentBase
	{
		internal new GeneralSettingsDialogViewModel DataContext
		{
			get => (GeneralSettingsDialogViewModel)base.DataContext;
			set => base.DataContext = value;
		}

		internal GeneralSettingsDialog(MainViewModel main)
		{
			InitializeComponent();
			Owner = main.MainWindow.RootWindow;
			DataContext = new GeneralSettingsDialogViewModel(main, this);
		}

		
	}

	internal class GeneralSettingsDialogViewModel : NotificationObject
	{
		private string textEditorOffsetX;
		private string textEditorOffsetY;

		public GeneralSettings Model { get; }
		public GeneralSettingsDialog Dialog { get; }
		public ActionCommand OkCommand { get; }
		public ActionCommand CancelCommand { get; }
		public ActionCommand SelectTextEditorBackgroundImagePathCommand { get; }
		public List<GeneralSettingsColorSettingViewModel> Colors { get; }

		public bool IsChanged
		{
			get => !Model.IsEqlals(MainViewModel.EditorSettings.GeneralSettings);
		}

		public string TextEditorOffsetX
		{
			get => textEditorOffsetX;
			set
			{
				textEditorOffsetX = value;
				double data;
				if (double.TryParse(TextEditorOffsetX, out data))
					Model.TextEditorOffsetX = data;
				NotifyChanged();
			}
		}

		public string TextEditorOffsetY
		{
			get => textEditorOffsetY;
			set
			{
				textEditorOffsetY = value;
				double data;
				if (double.TryParse(TextEditorOffsetY, out data))
					Model.TextEditorOffsetY = data;
				NotifyChanged();
			}
		}

		public GeneralSettingsDialogViewModel(MainViewModel main, GeneralSettingsDialog dialog)
		{
			Dialog = dialog;
			Model = MainViewModel.EditorSettings.GeneralSettings.Clone();
			Colors = new List<GeneralSettingsColorSettingViewModel>();
			dialog.Closing += Dialog_Closing;

			OkCommand = new ActionCommand(
				o =>
				{
					if (IsChanged)
					{
						dialog.DialogResult = true;
					}
					dialog.Close();
				}
				);

			CancelCommand = new ActionCommand(
				o =>
				{
					dialog.Close();
				}
				);

			SelectTextEditorBackgroundImagePathCommand = new ActionCommand(
				o =>
				{
					if (o is System.Windows.Controls.TextBox t)
					{
						var d = new OpenFileDialog();
						d.Filter = "png画像ファイル(*.png)|*.png";
						if (d.ShowDialog() == DialogResult.OK)
						{
							t.Text = d.FileName;
						}
					}
				}

				);

			//数値
			textEditorOffsetX = Model.TextEditorOffsetX.ToString();
			textEditorOffsetY = Model.TextEditorOffsetY.ToString();

			//カラーデータ
			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "通常文字",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Default),
				Syntax = ScriptSyntax.Default
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "エスケープされた文字",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Escape),
				Syntax = ScriptSyntax.Escape
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "サーフェス変更",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.ChangeSurface),
				Syntax = ScriptSyntax.ChangeSurface
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "さくらスクリプト",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.ScriptTag),
				Syntax = ScriptSyntax.ScriptTag
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "選択肢",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Choice),
				Syntax = ScriptSyntax.Choice
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "ジャンプ",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Jump),
				Syntax = ScriptSyntax.Jump
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "文",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Sentence),
				Syntax = ScriptSyntax.Sentence
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "単語群",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Word),
				Syntax = ScriptSyntax.Word
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "変数",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Variable),
				Syntax = ScriptSyntax.Variable
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "関数",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Function),
				Syntax = ScriptSyntax.Function
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "情報取得変数",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Getter),
				Syntax = ScriptSyntax.Getter
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "コメント",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Comment),
				Syntax = ScriptSyntax.Comment
			});

			/*
			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "タブ",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Tab),
				Syntax = ScriptSyntax.Tab,
				IsBackgroundColor = true
			});
			*/

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "テキストエディタ背景",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Background),
				Syntax = ScriptSyntax.Background,
				IsBackgroundColor = true
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "半角スペース",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.Space),
				Syntax = ScriptSyntax.Space,
				IsBackgroundColor = true
			});

			Colors.Add(new GeneralSettingsColorSettingViewModel(this)
			{
				Label = "全角スペース",
				DefaultColor = Themes.ApplicationTheme.GetEditorHilight(ScriptSyntax.WideSpace),
				Syntax = ScriptSyntax.WideSpace,
				IsBackgroundColor = true
			});


			//現在の設定をロードする
			foreach(var item in Colors)
			{
				if( Model.TextEditorColors.ContainsKey(item.ColorKey))
				{
					var color = Model.TextEditorColors[item.ColorKey];

					item.IsUseDefault = false;
					item.Color = Themes.ApplicationTheme.UintToColorRGB(color);
				}
				else
				{
					item.IsUseDefault = true;
				}
			}
		}

		private void Dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Dialog.DialogResult.HasValue)
				return;

			if (IsChanged)
			{
				var result = System.Windows.MessageBox.Show("変更を保存せずに閉じてもよろしいですか？", "基本設定", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result != MessageBoxResult.Yes)
				{
					e.Cancel = true;
					return;
				}
			}
			Dialog.DialogResult = false;
		}
	}

	internal class GeneralSettingsColorSettingViewModel : NotificationObject
	{
		private GeneralSettingsDialogViewModel Parent { get; }
		private Color color;
		private bool isUseDefault;

		public Color Color
		{
			get => color;
			set
			{
				if (color != value)
				{
					color = value;
					NotifyChanged();
					NotifyChanged(nameof(Brush));

					//Modelに登録
					if (!IsUseDefault)
						Parent.Model.TextEditorColors[ColorKey] = Themes.ApplicationTheme.ColorToUintRGB(color);
				}
			}
		}
		public uint DefaultColor { get; set; }

		public bool IsUseDefault
		{
			get => isUseDefault;
			set
			{
				if (isUseDefault != value)
				{
					isUseDefault = value;
					NotifyChanged();

					//設定を消してデフォルトを示す
					if (isUseDefault)
					{
						Parent.Model.TextEditorColors.Remove(ColorKey);
						Color = Themes.ApplicationTheme.UintToColorRGB(DefaultColor);
					}
					else
						Parent.Model.TextEditorColors[ColorKey] = DefaultColor;
				}
			}
		}

		public Brush Brush
		{
			get => new SolidColorBrush(Color);
		}

		public bool IsBackgroundColor{get;set;}
		public string Label { get; set; }
		public ScriptSyntax Syntax { get; set; }
		public string ColorKey => Enum.GetName(typeof(ScriptSyntax), Syntax);
		public ActionCommand ColorEditCommand { get; }

		public GeneralSettingsColorSettingViewModel(GeneralSettingsDialogViewModel parent)
		{
			Parent = parent;
			ColorEditCommand = new ActionCommand(
				o =>
				{
					var dialog = new ColorDialog();
					dialog.AllowFullOpen = true;
					if(dialog.ShowDialog() == DialogResult.OK)
					{
						Color = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
					}
				}
				);
		}

	}
}
