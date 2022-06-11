using Satolist2.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Satolist2.Core
{
	internal class LogMessage : NotificationObject
	{
		public static readonly LogMessage Instance = new LogMessage();
		private ObservableCollection<LogMessageItemViewModel> logItems;
		public ReadOnlyObservableCollection<LogMessageItemViewModel> LogItems
		{
			get => new ReadOnlyObservableCollection<LogMessageItemViewModel>(logItems);
		}
		public LogMessageItemViewModel NewestLogMessage
		{
			get
			{
				return logItems.LastOrDefault();
			}
		}

		private LogMessage()
		{
			logItems = new ObservableCollection<LogMessageItemViewModel>();
			SetVersionInfo();
		}

		public static void AddLog(LogMessageItemViewModel log)
		{
			Instance.logItems.Add(log);
			Instance.NotifyChanged(nameof(NewestLogMessage));
		}

		public static void AddLog(string message, LogMessageType type = LogMessageType.Notice)
		{
			AddLog(new LogMessageItemViewModel(message, type));
		}

		private void AddLogInternal(string message, LogMessageType type = LogMessageType.Notice)
		{
			logItems.Add(new LogMessageItemViewModel(message, type));
			NotifyChanged(nameof(NewestLogMessage));
		}

		public static void ClearLog()
		{
			Instance.logItems.Clear();
			Instance.SetVersionInfo();
		}

		private void SetVersionInfo()
		{
			var asmVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			var asmVerStr = string.Format("{0}.{1}.{2}", asmVer.Major, asmVer.Minor, asmVer.Build);
			AddLogInternal(string.Format(Version.VersionString, asmVerStr));
		}
	}

	internal enum LogMessageType
	{
		Notice,
		Error
	}

	internal class LogMessageItemViewModel : NotificationObject
	{
		private string message;
		private LogMessageType type;

		public string Message
		{
			get => message;
			set
			{
				message = value;
				NotifyChanged();
			}
		}

		public LogMessageType Type
		{
			get => type;
			set
			{
				type = value;
				NotifyChanged();
			}
		}

		public LogMessageItemViewModel(string message, LogMessageType type = LogMessageType.Notice)
		{
			Message = message;
			Type = type;
		}
	}
}
