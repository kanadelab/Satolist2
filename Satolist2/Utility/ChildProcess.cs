using Satolist2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Satolist2.Utility
{
	//自動終了子プロセス
	public class ChildProcess : IDisposable
	{
		public Process Process { get; }
		public IntPtr hJob;

		public ChildProcess(ProcessStartInfo startInfo)
		{
			//ジョブ作成、ジョブ開放でプロセスキルする仕組み
			hJob = Win32Import.CreateJobObject(IntPtr.Zero, null);
			Win32Import.JOBOBJECT_EXTENDED_LIMIT_INFORMATION exInfo = new Win32Import.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
			exInfo.BasicLimitInformation.LimitFlags = Win32Import.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
			Win32Import.SetInformationJobObject(hJob, Win32Import.JobObjectExtendedLimitInformation, ref exInfo, (uint)Marshal.SizeOf<Win32Import.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());

			//プロセス起動
			Process = Process.Start(startInfo);
			Win32Import.AssignProcessToJobObject(hJob, Process.Handle);
		}

		public void Dispose()
		{
			Process.Kill();
			Process.Dispose();
			Win32Import.CloseHandle(hJob);
		}
	}
}
