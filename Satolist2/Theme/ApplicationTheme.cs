using AvalonDock.Themes;
using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Satolist2.Themes
{
	//xamlで定義してないテーマ情報をここで扱ってる
	static class ApplicationTheme
	{
		public enum Themes
		{
			Default,
			Dark
		}

		public static Themes CurrentTheme { get; set; } = Themes.Dark;

		public static void Setup(ResourceDictionary resourceDictionary)
		{
			//リソースディクショナリ
			Uri uri = null;
			switch(CurrentTheme)
			{
				case Themes.Default:
					uri = new Uri("Theme/DefaultTheme.xaml", UriKind.Relative);
					break;
				case Themes.Dark:
					uri = new Uri("Theme/DarkTheme.xaml", UriKind.Relative);
					break;
			}
			
			var dic = new ResourceDictionary();
			dic.Source = uri;
			resourceDictionary.MergedDictionaries.Add(dic);
		}

		public static Uri GetBootWindowPath()
		{
			switch(CurrentTheme)
			{
				case Themes.Default:
					return new Uri("DefaultRootWindow.xaml", UriKind.Relative);
				case Themes.Dark:
					return new Uri("DarkRootWindow.xaml", UriKind.Relative);
			}
			throw new Exception();
		}

		public static AvalonDock.Themes.Theme GetDockingSystemTheme()
		{
			switch (CurrentTheme)
			{
				case Themes.Default:
					return new Vs2013LightTheme();
				case Themes.Dark:
					return new Vs2013DarkTheme();
			}
			throw new Exception();
		}

		public static uint GetEditorHilight(ScriptSyntax type)
		{
			switch(CurrentTheme)
			{
				case Themes.Default:
					return DefaultThemeEditorHilight[type];
				case Themes.Dark:
					return DarkThemeEditorHilight[type];
			}
			throw new Exception();
		}

		public static bool? ShowDialog(Dialog.DialogContentBase control)
		{
			switch(CurrentTheme)
			{
				case Themes.Default:
					return (new DefautDialogHostWindow(control)).ShowDialog();
				case Themes.Dark:
					return (new DarkDialogHostWindow(control)).ShowDialog();
			}
			throw new Exception();
		}

		public static Brush GetWartermarkBrush()
		{
			switch(CurrentTheme)
			{
				case Themes.Default:
					return new SolidColorBrush(Colors.Black);
				case Themes.Dark:
					return new SolidColorBrush(Colors.White);
			}
			throw new Exception();
		}

		private static Dictionary<ScriptSyntax, uint> DefaultThemeEditorHilight = new Dictionary<ScriptSyntax, uint>()
		{
			{ ScriptSyntax.Background, 0xFFFFFF },
			{ ScriptSyntax.Default, 0x000000 },
			{ ScriptSyntax.Escape, 0x888844 },
			{ ScriptSyntax.ScriptTag, 0x800000 },
			{ ScriptSyntax.ChangeSurface, 0x0080ff },
			{ ScriptSyntax.Jump, 0x108010 },
			{ ScriptSyntax.Choice, 0x108010 },
			{ ScriptSyntax.Anchor, 0x108010 },
			{ ScriptSyntax.Word, 0x0000ff },
			{ ScriptSyntax.Sentence, 0x0000ff },
			{ ScriptSyntax.Variable, 0x888844 },
			{ ScriptSyntax.Getter, 0x800000 },
			{ ScriptSyntax.Random, 0x108010 },
			{ ScriptSyntax.Argument, 0x108010 },
			{ ScriptSyntax.Comment, 0x60A060 },
			{ ScriptSyntax.Tab, 0xDDDDFF },
			{ ScriptSyntax.Function, 0x0080ff },
			{ ScriptSyntax.Saori, 0x0080ff },
			{ ScriptSyntax.Replace, 0x404070 },
			{ ScriptSyntax.ReplaceAfter, 0x404070 },
			{ ScriptSyntax.Space, 0xDDDDFF },
			{ ScriptSyntax.WideSpace, 0xDDFFDD }
		};

		private static Dictionary<ScriptSyntax, uint> DarkThemeEditorHilight = new Dictionary<ScriptSyntax, uint>()
		{
			{ ScriptSyntax.Background, 0x1E1E1E },
			{ ScriptSyntax.Default, 0xDCDCDC },
			{ ScriptSyntax.Escape, 0xAAAA66 },
			{ ScriptSyntax.ScriptTag, 0xAA6666 },
			{ ScriptSyntax.ChangeSurface, 0x44a0ff },
			{ ScriptSyntax.Jump, 0x88CC88 },
			{ ScriptSyntax.Choice, 0x88CC88 },
			{ ScriptSyntax.Anchor, 0x88CC88 },
			{ ScriptSyntax.Word, 0x8888FF },
			{ ScriptSyntax.Sentence, 0x8888FF },
			{ ScriptSyntax.Variable, 0xAAAA66 },
			{ ScriptSyntax.Getter, 0xAA6666 },
			{ ScriptSyntax.Random, 0x88CC88 },
			{ ScriptSyntax.Argument, 0x88CC88 },
			{ ScriptSyntax.Comment, 0x66AA66 },
			{ ScriptSyntax.Tab, 0x202088 },
			{ ScriptSyntax.Function, 0x0080ff },
			{ ScriptSyntax.Saori, 0x44AAff },
			{ ScriptSyntax.Replace, 0x8080A0 },
			{ ScriptSyntax.ReplaceAfter, 0x8080A0 },
			{ ScriptSyntax.Space, 0x202066 },
			{ ScriptSyntax.WideSpace, 0x306630 }
		};

		public static Color UintToColorRGB(uint color)
		{
			return Color.FromArgb(
						0xff,
						(byte)((color & 0x00ff0000) >> 16),
						(byte)((color & 0x0000ff00) >> 8),
						(byte)((color & 0x000000ff) >> 0)
						);
		}

		public static uint ColorToUintRGB(Color color)
		{
			return (uint)0xff << 24 | (uint)color.R << 16 | (uint)color.G << 8 | (uint)color.B;
		}
	}
}
