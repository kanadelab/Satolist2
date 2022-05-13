using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Satolist2.Utility;

namespace Satolist2
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			//テーマ設定
			var isDarkMode = Model.EditorSettings.TemporaryLoadGeneralSettings()?.IsEnableDarkMode ?? false;

			Themes.ApplicationTheme.CurrentTheme = isDarkMode ? Themes.ApplicationTheme.Themes.Dark : Themes.ApplicationTheme.Themes.Default;
			Themes.ApplicationTheme.Setup(Resources);
			StartupUri = Themes.ApplicationTheme.GetBootWindowPath();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			MainViewModel.EditorSettings?.SaveTemporarySettings();
			base.OnExit(e);
		}
	}
}
