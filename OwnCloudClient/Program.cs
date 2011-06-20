using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Heyes;

namespace OwnCloudClient
{
	class Program
	{
		public static string ReadPassword()
		{
			Stack<string> pass = new Stack<string>();
			for (ConsoleKeyInfo consKeyInfo = Console.ReadKey(true); consKeyInfo.Key != ConsoleKey.Enter; consKeyInfo = Console.ReadKey(true))
			{
				if (consKeyInfo.Key == ConsoleKey.Backspace)
				{
					try
					{
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						Console.Write(" ");
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						pass.Pop();
					}
					catch (InvalidOperationException)
					{
						// Nothing to delete, go back to previous position 
						Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
					}
				}
				else
				{
					Console.Write("*");
					pass.Push(consKeyInfo.KeyChar.ToString());
				}
			}
			String[] password = pass.ToArray();
			Array.Reverse(password);
			return string.Join(string.Empty, password);
		}

		static void Main(string[] args)
		{
			bool confirmDownload = !Settings.NoConfirmDownload;
			bool confirmUpload = !Settings.NoConfirmUpload;
			bool confirmDelete = !Settings.NoConfirmDelete;
			bool runOnce = Settings.RunOnce;
			bool massDownload = Settings.MassDownload;
			int sleepSeconds = Settings.SleepSeconds;

			string userName = Settings.UserName;
			string password = Settings.Password;

			GetOpt parser = new GetOpt(args);
			try
			{
				parser.SetOpts(new string[] {	"noconfirmdownload", 
												"noconfirmupload", 
												"noconfirmdelete", 
												"runonce", 
												"massdownload", 
												"sleepseconds=",
												"watchdir=",
												"baseurl="

				});
				parser.Parse();

				if (parser.IsDefined("noconfirmdownload"))
					confirmDownload = false;
				if (parser.IsDefined("noconfirmupload"))
					confirmUpload = false;
				if (parser.IsDefined("noconfirmdelete"))
					confirmDelete = false;
				if (parser.IsDefined("runonce"))
					runOnce = true;
				if (parser.IsDefined("massdownload"))
					massDownload = true;
				if (parser.IsDefined("watchdir"))
					Settings.WatchDir = parser.Opts["watchdir"].ToString();
				if (parser.IsDefined("baseurl"))
					Settings.OwnCloudUrl = parser.Opts["baseurl"].ToString();

				if (parser.IsDefined("sleepseconds"))
					sleepSeconds = Convert.ToInt32(parser.Opts["sleepseconds"].ToString());
				if (sleepSeconds < 10)
					sleepSeconds = 10;
			}
			catch (ArgumentException ex)
			{
				NLogger.Current.FatalException("Argument Exception", ex);

				Console.WriteLine("noconfirmdownload");
				Console.WriteLine("noconfirmupload");
				Console.WriteLine("noconfirmdelete");
				Console.WriteLine("runonce");
				Console.WriteLine("massdownload");
				Console.WriteLine("sleepseconds");
				Console.WriteLine("watchdir");
				Console.WriteLine("baseurl");
				return;
			}
			catch (Exception ex)
			{
				NLogger.Current.FatalException("GetOpt Exception", ex);
			}

			if (string.IsNullOrEmpty(userName))
			{
				Console.Write("Enter UserName: ");
				userName = Console.ReadLine();
			}
			if (string.IsNullOrEmpty(password))
			{
				Console.Write("Enter Password: ");
				password = ReadPassword();
				Console.WriteLine();
			}

			NLogger.Current.Debug("Options: ");
			NLogger.Current.Debug("confirmdownload: " + confirmDownload);
			NLogger.Current.Debug("confirmupload: " + confirmUpload);
			NLogger.Current.Debug("confirmdelete: " + confirmDelete);
			NLogger.Current.Debug("runonce: " + runOnce);
			NLogger.Current.Debug("massdownload: " + massDownload);
			NLogger.Current.Debug("sleepSeconds: " + sleepSeconds);
			NLogger.Current.Debug("watchdir: " + Settings.WatchDir);
			NLogger.Current.Debug("baseurl: " + Settings.OwnCloudUrl);

			if (!OwnCloudClient.Login(userName, password))
			{
				NLogger.Current.Warn("Invalid username or password");
				return;
			}

			if (massDownload)
			{
				//OwnCloudClient.DownloadAll("vccdrom~");
				NLogger.Current.Warn("Warning: This may overwrite files in your local directory: Continue? [y/n]: ");

				ConsoleKeyInfo k = Console.ReadKey();
				Console.WriteLine();

				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					OwnCloudClient.DownloadAll();

				return; //bail out of program
			}

			NLogger.Current.Trace("Loading locals");
			List<FileInfoX> localFiles = OwnCloudClient.GetLocalFileList();

			NLogger.Current.Trace("Loading Remotes");
			List<FileInfoX> remoteFiles = OwnCloudClient.GetRemoteFileList();

			//outdated checks only need to run at the beginning since we are checking with lastSweep
			//NOTE: if used with multiple users on the same cloud account this becomes problematic
			int updatedRemoteFiles = FileProcessingHelpers.ProcessOutDatedRemoteFiles(localFiles, remoteFiles, confirmUpload);
			if (updatedRemoteFiles > 0)
			{
				NLogger.Current.Trace("Refreshing remotes");
				remoteFiles = OwnCloudClient.GetRemoteFileList(); //refresh
			}
			int updatedLocalFiles = FileProcessingHelpers.ProcessOutDatedLocalFiles(localFiles, remoteFiles, confirmDownload);

			DateTime lastSweep = DateTime.Now;
			while (true)
			{
				NLogger.Current.Trace("Refreshing locals");
				localFiles = OwnCloudClient.GetLocalFileList();

				int uploadCount = FileProcessingHelpers.ProcessNewLocalFiles(localFiles, remoteFiles, confirmUpload, lastSweep);
				int deleteCount = FileProcessingHelpers.ProcessDeleteRemoteFiles(localFiles, remoteFiles, confirmDelete);

				if (runOnce)
					return; //bail out of program

				if (uploadCount > 0 || deleteCount > 0)
				{
					NLogger.Current.Trace("Refreshing remotes");
					remoteFiles = OwnCloudClient.GetRemoteFileList();
				}

				lastSweep = DateTime.Now;
				System.Threading.Thread.Sleep(1000 * sleepSeconds);

			}
		}
	}
}
