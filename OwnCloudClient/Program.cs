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

		public static void PrintSampleUseage()
		{
			Console.WriteLine("Unless downloadonly or runonce flags are set, program will continue to monitor changes");
			Console.WriteLine("--noconfirmdownload\t[flag] Don't ask about downloading files to local disk");
			Console.WriteLine("--noconfirmupload\t[flag] Don't ask about uploading files to OwnCloud");
			Console.WriteLine("--noconfirmdelete\t[flag] Don't ask about deleting remote files");
			Console.WriteLine("--runonce\t\t[flag] Run all checks once and then exit - otherwise sleep for [sleepseconds] and run again");
			Console.WriteLine("--downloadonly\t\t[flag] Download all changed/new files hosted in OwnCloud and exit");
			Console.WriteLine("--sleepseconds\t\tNumber of seconds to wait before checking for changes (default = 10)");
			Console.WriteLine("--watchdir=\t\tThe directory (recursive) to watch for changes (default currentDir\\data\\)");
			Console.WriteLine("--owncloudurl\t\tThe URL to your OwnCloud instance");
		}

		public static void PrintCurrentSettings()
		{
			NLogger.Current.Debug("Options: ");
			NLogger.Current.Debug("confirmdownload: " + !Settings.NoConfirmDownload);
			NLogger.Current.Debug("confirmupload: " + !Settings.NoConfirmUpload);
			NLogger.Current.Debug("confirmdelete: " + !Settings.NoConfirmDelete);
			NLogger.Current.Debug("runonce: " + Settings.RunOnce);
			NLogger.Current.Debug("downloadonly: " + Settings.DownloadOnly);
			NLogger.Current.Debug("sleepSeconds: " + Settings.SleepSeconds);
			NLogger.Current.Debug("watchdir: " + Settings.WatchDir);
			NLogger.Current.Debug("owncloudurl: " + Settings.OwnCloudUrl);
		}

		public static bool SetSettings(string[] args)
		{
			bool success = true;

			GetOpt parser = new GetOpt(args);
			try
			{
				parser.SetOpts(new string[] {	
					"noconfirmdownload", 
					"noconfirmupload", 
					"noconfirmdelete", 
					"runonce", 
					"downloadonly", 
					"sleepseconds=",
					"watchdir=",
					"owncloudurl=",
					"help"
				});

				parser.Parse();

				if (parser.IsDefined("help"))
				{
					PrintSampleUseage();
					return false;
				}

				if (parser.IsDefined("noconfirmdownload"))
					Settings.NoConfirmDownload = true;
				if (parser.IsDefined("noconfirmupload"))
					Settings.NoConfirmUpload = true;
				if (parser.IsDefined("noconfirmdelete"))
					Settings.NoConfirmDelete = true;
				if (parser.IsDefined("runonce"))
					Settings.RunOnce = true;
				if (parser.IsDefined("downloadonly"))
					Settings.DownloadOnly = true;
				if (parser.IsDefined("watchdir"))
					Settings.WatchDir = parser.Opts["watchdir"].ToString();
				if (parser.IsDefined("owncloudurl"))
					Settings.OwnCloudUrl = parser.Opts["owncloudurl"].ToString();
				if (parser.IsDefined("sleepseconds"))
					Settings.SleepSeconds = Convert.ToInt32(parser.Opts["sleepseconds"].ToString());
			
			}
			catch (ArgumentException ex)
			{
				NLogger.Current.FatalException("Argument Exception", ex);

				PrintSampleUseage();
				success = false;
			}
			catch (Exception ex)
			{
				NLogger.Current.FatalException("GetOpt Exception", ex);
				success = false;
			}

			if (string.IsNullOrEmpty(Settings.UserName))
			{
				Console.Write("Enter UserName: ");
				Settings.UserName = Console.ReadLine();
			}
			if (string.IsNullOrEmpty(Settings.Password))
			{
				Console.Write("Enter Password: ");
				Settings.Password = ReadPassword();
				Console.WriteLine();
			}

			return success;
		}

		static void Main(string[] args)
		{
			if (!SetSettings(args))
				return;

			PrintCurrentSettings();

			if (!OwnCloudClient.Login(Settings.UserName, Settings.Password))
			{
				NLogger.Current.Warn("Invalid username or password");
				return;
			}

			if (Settings.DownloadOnly)
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
			int updatedRemoteFiles = FileProcessingHelpers.ProcessOutDatedRemoteFiles(localFiles, remoteFiles, !Settings.NoConfirmUpload);
			if (updatedRemoteFiles > 0)
			{
				NLogger.Current.Trace("Refreshing remotes");
				remoteFiles = OwnCloudClient.GetRemoteFileList(); //refresh
			}
			int updatedLocalFiles = FileProcessingHelpers.ProcessOutDatedLocalFiles(localFiles, remoteFiles, !Settings.NoConfirmDownload);

			DateTime lastSweep = DateTime.Now;
			while (true)
			{
				NLogger.Current.Trace("Refreshing locals");
				localFiles = OwnCloudClient.GetLocalFileList();

				int uploadCount = FileProcessingHelpers.ProcessNewLocalFiles(localFiles, remoteFiles, !Settings.NoConfirmUpload, lastSweep);
				int deleteCount = FileProcessingHelpers.ProcessDeleteRemoteFiles(localFiles, remoteFiles, !Settings.NoConfirmDelete);

				if (Settings.RunOnce)
					return; //bail out of program

				if (uploadCount > 0 || deleteCount > 0)
				{
					NLogger.Current.Trace("Refreshing remotes");
					remoteFiles = OwnCloudClient.GetRemoteFileList();
				}

				lastSweep = DateTime.Now;
				System.Threading.Thread.Sleep(1000 * Settings.SleepSeconds);
			}
		}
	}
}
