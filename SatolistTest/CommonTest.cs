using Microsoft.VisualStudio.TestTools.UnitTesting;
using Satolist2.Core;
using Satolist2.Model;
using Satolist2.Utility;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace SatolistTest
{
	[TestClass]
	public class CommonTest
	{
		private static string GetSatolistWorkingDirectory()
		{
			return Path.GetFullPath(Environment.CurrentDirectory + "/../../../Satolist2/bin/Debug" );
		}

		private static string GetTestSampleGhostDirectory()
		{
			return Path.GetFullPath(Environment.CurrentDirectory + "/../../../TestSampleGhost");
		}

		[TestMethod]
		public void FMOTest()
		{
			SakuraFMOReader reader = new SakuraFMOReader();
			reader.Read();
			Console.WriteLine("Ok");
		}

		[TestMethod]
		public void SendSSTPTest()
		{
			SakuraFMOReader reader = new SakuraFMOReader();
			reader.Read();
			SakuraFMORecord instantTarget = reader.Records.First().Value;

			ProtocolBuilder sstp = new ProtocolBuilder();
			sstp.Command = "SEND SSTP/1.0";
			sstp.Parameters["Script"] = "\\0\\s[5]あーあーてすてす。";
			sstp.Parameters["Sender"] = "さとりすと そのに";
			sstp.Parameters["Option"] = "notranslate";
			sstp.Parameters["ID"] = instantTarget.ID;
			sstp.Parameters["IfGhost"] = "湯空さとり";

			Satorite.RaiseSSTP(sstp, instantTarget);
		}

		[TestMethod]
		public void NotifySSTPTest()
		{
			SakuraFMOReader reader = new SakuraFMOReader();
			reader.Read();
			SakuraFMORecord instantTarget = reader.Records.First().Value;

			ProtocolBuilder sstp = new ProtocolBuilder();
			sstp.Command = "NOTIFY SSTP/1.0";
			sstp.Parameters["Event"] = "OnBoot";
			sstp.Parameters["Sender"] = "さとりすと そのに";

			Satorite.RaiseSSTP(sstp, instantTarget);
		}

		[TestMethod]
		public void SatoriProxyTest()
		{
			SatoriProxy.Program.Main(new string[]{ "SendSatori", "OnBoot", GetTestSampleGhostDirectory() + "/ghost/master" });
		}

		[TestMethod]
		public void SatoriteTest()
		{
			Environment.CurrentDirectory = GetSatolistWorkingDirectory();

			GhostModel ghost = new GhostModel(GetTestSampleGhostDirectory());
			Satorite.SendSatori(ghost, "：（０）こんばんはー。", EventType.Sentence);
		}

		[TestMethod]
		public void SurfaceParseTest()
		{
			ShellAnalyzer reader = new ShellAnalyzer();
			reader.Load(@"D:\SSP_2\ghost\0x15\shell\master");
			Console.Write("loaded");
		}

		[TestMethod]
		public void SurfaceRangeTest()
		{
			string input = "10-20,10-20,!15";
			SurfaceRangeCollection range = SurfaceRangeCollection.MakeRange(input);
			Console.WriteLine( string.Format("\"{0}\" -> \"{1}\"", input, range));
		}

		[TestMethod]
		public void GenerateUpdateFileTest()
		{
			NarUtility.CreateUpdateFile(GetTestSampleGhostDirectory());
		}

		[TestMethod]
		public void GenerateUpdateNarTest()
		{
			NarUtility.CreateNar(GetTestSampleGhostDirectory(), Path.GetTempPath() + "/test.nar");
		}

		[TestMethod]
		public void FtpTest()
		{
			//パスワードいれたままあげないこと
			var account = new FtpAccount()
			{
			};

			var client = new FtpClient(account);
			var request = new FtpFileListRequest()
			{
				RemotePath = "www"
			};

			client.ExecuteCommand(request);
			Console.WriteLine("a");
		}

		[TestMethod]
		public void ZipDownload()
		{
			WebClient mywebClient = new WebClient();
			mywebClient.DownloadFile("https://github.com/kanadelab/Satolist2/releases/download/dev70/dev70.zip", @"downloaded.zip");
		}

	}
}
