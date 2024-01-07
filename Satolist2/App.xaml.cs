using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
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
		public static string UpdatorPath { get; private set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			//アセンブリ位置をカレントに設定
			Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			PrepareEnvironment();

			base.OnStartup(e);

			//テーマ設定
			var isDarkMode = Model.EditorSettings.TemporaryLoadGeneralSettings()?.IsEnableDarkMode ?? false;

			Themes.ApplicationTheme.CurrentTheme = isDarkMode ? Themes.ApplicationTheme.Themes.Dark : Themes.ApplicationTheme.Themes.Default;
			Themes.ApplicationTheme.Setup(Resources);
			StartupUri = Themes.ApplicationTheme.GetBootWindowPath();
		}

		public static void PrepareEnvironment()
		{
			//アセンブリを奥においやる為の
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		//アセンブリを奥におしやる
		private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var name = new AssemblyName(args.Name);
			var path = string.Format(@"data\assembly\{0}", name.Name);
			if (!string.IsNullOrEmpty(name.CultureName))
				path = string.Format(@"data\assembly\{1}\{0}", name.Name, name.CultureName);

			if (System.IO.File.Exists(path + ".dll"))
				return Assembly.Load(System.IO.File.ReadAllBytes(path + ".dll"));
			else if (System.IO.File.Exists(path + ".exe"))
			{
				//アップデータの名前を記憶
				if (name.Name == "SatolistUpdator")
					UpdatorPath = path + ".exe";
				return Assembly.Load(System.IO.File.ReadAllBytes(path + ".exe"));
			}
			else
				return null;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			MainViewModel.EditorSettings?.SaveTemporarySettings();
			MainViewModel.EditorSettings?.SaveGeneralSettings();
			JumpListClient.ReflectHistoryToJumpList();
			base.OnExit(e);
		}
	}
}
