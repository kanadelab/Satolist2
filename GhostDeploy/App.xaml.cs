using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GhostDeploy
{
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
#if DEPLOY
			//アセンブリ位置をカレントに設定
			Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
#endif
			Satolist2.App.PrepareEnvironment();
			base.OnStartup(e);
		}
	}
}
