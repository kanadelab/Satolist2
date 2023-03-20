using Satolist2.Model;
using Satolist2.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace Satolist2.Utility
{
	internal static class JumpListClient
	{
		public static void ReflectHistoryToJumpList()
		{
			var items = MainViewModel.EditorSettings.TemporarySettings.GhostHistory;
			var historyItems = new List<OpenGhostHistory>();

			//お気に入りを先に
			historyItems.AddRange(items.Where(o => o.IsFavorite));
			historyItems.AddRange(items.Where(o => !o.IsFavorite));

			var list = new List<JumpTask>();

			foreach(var history in historyItems)
			{
				var task = new JumpTask();
				var openPath = DictionaryUtility.NormalizeWindowsPath(history.Path);
				var iconPath = DictionaryUtility.NormalizeWindowsPath(history.IconPath);

				//基本情報を設定
				task.Arguments = string.Format(@"""{0}""", history.Path);
				task.Description = openPath;
				task.Title = history.Name;
				task.CustomCategory = history.IsFavorite ? "お気に入りのゴースト" : "最近のゴースト";

				try
				{
					//アイコンを設定
					if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
					{
						task.IconResourcePath = iconPath;
					}
				}
				catch { }

				list.Add(task);
			}

			var jumpList = new JumpList(list, false, false);
			jumpList.Apply();
		}
	}
}
