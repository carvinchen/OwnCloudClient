using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

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

		public static void DisplaySampleUseage()
		{
			Console.WriteLine("Note: Unless downloadonly or runonce flags are set, program will continue to monitor changes");
			Console.WriteLine();
			Console.WriteLine("--noconfirmdownload\t[flag] Don't ask about downloading files to local disk");
			Console.WriteLine("--noconfirmupload\t[flag] Don't ask about uploading files to OwnCloud");
			Console.WriteLine("--noconfirmdelete\t[flag] Don't ask about deleting remote files");
			Console.WriteLine("--runonce\t\t[flag] Run all checks once and then exit - otherwise sleep for [sleepseconds] and run again");
			Console.WriteLine("--downloadonly\t\t[flag] Download all changed/new files hosted in OwnCloud and exit");
			Console.WriteLine("--sleepseconds\t\tNumber of seconds to wait before checking for changes (default = 10)");
			Console.WriteLine("--watchdir\t\tThe directory (recursive) to watch for changes (default currentDir\\data\\)");
			Console.WriteLine("--owncloudurl\t\tThe URL to your OwnCloud instance");
			Console.WriteLine("--version\t\tDisplay version informatoon");
			Console.WriteLine("--help\t\t\tDisplay this screen");    
		}

		public static void DisplayCurrentSettings()
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

		//http://stackoverflow.com/questions/1600962/c-displaying-the-build-date
		private static DateTime RetrieveLinkerTimestamp()
		{
			string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;
			byte[] b = new byte[2048];
			System.IO.Stream s = null;

			try
			{
				s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				s.Read(b, 0, 2048);
			}
			finally
			{
				if (s != null)
					s.Close();
			}

			int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
			int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds(secondsSince1970);
			dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
			return dt;
		}

		private static void DisplayVersionInfo()
		{
			Console.WriteLine("OwnCloudClient");
			Console.WriteLine("Build Date: " + RetrieveLinkerTimestamp().ToString());
			var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Console.WriteLine(string.Format("Version: {0}.{1}.{2}.{3}", v.Major, v.Minor, v.Revision, v.Build));
		}

		public static bool SetSettings(string[] args)
		{
			bool success = true;

			cb.Options.Parser p = new cb.Options.Parser();
			try
			{
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "noconfirmdownload", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "noconfirmupload", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "noconfirmdelete", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "runonce", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "downloadonly", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "sleepseconds" });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "watchdir" });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "owncloudurl", IsRequired = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "help", IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = "version", IsFlag = true });

				p.Parse(args);

				if (p.IsOptionDefined("help"))
				{
					DisplaySampleUseage();
					return false;
				}

				if (p.IsOptionDefined("version"))
				{
					DisplayVersionInfo();
					return false;
				}

				if (p.IsOptionDefined("noconfirmdownload"))
					Settings.NoConfirmDownload = true;
				if (p.IsOptionDefined("noconfirmupload"))
					Settings.NoConfirmUpload = true;
				if (p.IsOptionDefined("noconfirmdelete"))
					Settings.NoConfirmDelete = true;
				if (p.IsOptionDefined("runonce"))
					Settings.RunOnce = true;
				if (p.IsOptionDefined("downloadonly"))
					Settings.DownloadOnly = true;
				if (p.IsOptionDefined("watchdir"))
					Settings.WatchDir = p.GetOptionStringValue("watchdir");
				if (p.IsOptionDefined("owncloudurl"))
					Settings.OwnCloudUrl = p.GetOptionStringValue("owncloudurl");
				if (p.IsOptionDefined("sleepseconds"))
					Settings.SleepSeconds = Convert.ToInt32(p.GetOptionStringValue("sleepseconds"));

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
			}
			catch (ArgumentException ex)
			{
				NLogger.Current.FatalException("Argument Exception", ex);

				DisplaySampleUseage();
				success = false;
			}
			catch (Exception ex)
			{
				NLogger.Current.FatalException("SetSettings Exception", ex);
				success = false;
			}
			return success;
		}

		static void Main(string[] args)
		{
			try
			{
				if (!SetSettings(args))
					return;

				DisplayCurrentSettings();

				if (!OwnCloudClient.Login(Settings.UserName, Settings.Password))
				{
					NLogger.Current.Warn("Invalid username or password");
					return;
				}

				if (Settings.DownloadOnly)
				{
					//OwnCloudClient.DownloadAll("vccdrom~");
					ConsoleColor currentColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("Warning: This may overwrite files in your local directory: Continue? [y/n]: ");
					Console.ForegroundColor = currentColor;

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
			catch (Exception ex)
			{
				NLogger.Current.FatalException("Main() Exception", ex);
			}
		}
	}
}
