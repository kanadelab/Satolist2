using Satolist2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Utility
{
	public static class GhostRuntimeRequest
	{
		//SHIORIリロードスクリプトでリロードを行う
		public static void ReloadShiori(GhostModel ghost, string message = "SHIORIリロード。", bool showErrorLog = true)
		{
			try
			{
				string script = @"\0\![reload,shiori]\![quicksession,true]" + message;
				Satorite.Default.SendSSTP(ghost, script, true, true);
				Core.LogMessage.AddLog("SHIORIをリロードします。");
			}
			catch(GhostNotFoundException)
			{
				if(showErrorLog)
					Core.LogMessage.AddLog("編集中のゴーストにアクセスできません。SSPでゴーストを起動していますか？", Core.LogMessageType.Error);
			}
		}

	}
}
