using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace OwnCloudClient
{
	class Program
	{
		//public static bool Validator(object sender, 
		//    System.Security.Cryptography.X509Certificates.X509Certificate certificate, 
		//    System.Security.Cryptography.X509Certificates.X509Chain chain, 
		//    System.Net.Security.SslPolicyErrors sslPolicyErrors)
		//{
		//    //TODO: add application trust logic: http://www.mono-project.com/UsingTrustedRootsRespectfully
			
		//    //Console.WriteLine(sslPolicyErrors.ToString());

		//    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
		//        return true;
		//    // only ask for trust failure (you may want to handle more cases)
		//    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
		//        return false;

		//    Console.Write("A trust error occured while attempting to " +
		//        "access the web site. Do you wish to continue this " +
		//        "session even if we couldn't assess its security? ");
		//    return (Console.ReadLine().ToLower() == "yes");
		//}

		private static string ReadPassword()
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

		private static void DisplaySampleUseage()
		{
			Console.WriteLine("Note: Unless " + Settings.Constants.DownloadAll + " or " 
				+ Settings.Constants.RunOnce + " flags are set, program will continue to monitor changes");

			Console.WriteLine();
			Console.WriteLine("--" + Settings.Constants.NoConfirmDownload + "\t[flag] Don't ask about downloading files to local disk");
			Console.WriteLine("--" + Settings.Constants.NoConfirmUpload + "\t[flag] Don't ask about uploading files to OwnCloud");
			Console.WriteLine("--" + Settings.Constants.NoConfirmDelete + "\t[flag] Don't ask about deleting remote files");
			Console.WriteLine("--" + Settings.Constants.RunOnce + "\t\t[flag] Run all checks once and then exit - otherwise sleep for [sleepseconds] and run again");
			Console.WriteLine("--" + Settings.Constants.DownloadAll + "\t\t[flag] Download all changed/new files hosted in OwnCloud and exit");
			Console.WriteLine("--" + Settings.Constants.SleepSeconds + "\t\tNumber of seconds to wait before checking for changes (default = 10)");
			Console.WriteLine("--" + Settings.Constants.WatchDir + "\t\tThe directory (recursive) to watch for changes (default currentDir\\data\\)");
			Console.WriteLine("--" + Settings.Constants.OwnCloudUrl + "\t\tThe URL to your OwnCloud instance");
			Console.WriteLine("--" + Settings.Constants.DownloadAllPrefix + "\tDownload everything that starts with this");
			Console.WriteLine("--" + Settings.Constants.Version + "\t\tDisplay version information");
			Console.WriteLine("--" + Settings.Constants.Help + "\t\t\tDisplay this screen");    
		}

		private static void DisplayCurrentSettings()
		{
			NLogger.Current.Debug("Options: ");
			NLogger.Current.Debug(Settings.Constants.NoConfirmDownload + ": " + !Settings.NoConfirmDownload);
			NLogger.Current.Debug(Settings.Constants.NoConfirmUpload + ": " + !Settings.NoConfirmUpload);
			NLogger.Current.Debug(Settings.Constants.NoConfirmDelete + ": " + !Settings.NoConfirmDelete);
			NLogger.Current.Debug(Settings.Constants.RunOnce + ": " + Settings.RunOnce);
			NLogger.Current.Debug(Settings.Constants.DownloadAll + ": " + Settings.DownloadAll);
			NLogger.Current.Debug(Settings.Constants.SleepSeconds + ": " + Settings.SleepSeconds);
			NLogger.Current.Debug(Settings.Constants.WatchDir + ": " + Settings.WatchDir);
			NLogger.Current.Debug(Settings.Constants.ListRemoteFiles + ": " + Settings.ListRemoteFiles);
			NLogger.Current.Debug(Settings.Constants.DownloadAllPrefix + ": " + Settings.DownloadAllPrefix);
			NLogger.Current.Debug(Settings.Constants.OwnCloudUrl + ": " + Settings.OwnCloudUrl);
		}
		
		private static DateTime RetrieveLinkerTimestamp()
		{
			//http://stackoverflow.com/questions/1600962/c-displaying-the-build-date
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

		private static bool SetSettings(string[] args)
		{
			bool success = true;

			cb.Options.Parser p = new cb.Options.Parser();
			try
			{
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.NoConfirmDownload, IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.NoConfirmUpload, IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.NoConfirmDelete, IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.RunOnce, IsFlag = true, ShortName = '1' });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.DownloadAll, IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.UploadOnly, IsFlag = true });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.SleepSeconds, ShortName = 's'});
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.WatchDir });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.OwnCloudUrl });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.Help, IsFlag = true, ShortName = 'h' });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.Version, IsFlag = true, ShortName = 'v' });
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.DownloadAllPrefix});
				p.AddDefinition(new cb.Options.OptionDefinition() { LongName = Settings.Constants.ListRemoteFiles, IsFlag = true, ShortName = 'l'});

				p.Parse(args);

				if (p.IsOptionDefined(Settings.Constants.Help))
				{
					DisplaySampleUseage();
					return false;
				}

				if (p.IsOptionDefined(Settings.Constants.Version))
				{
					DisplayVersionInfo();
					return false;
				}

				Settings.NoConfirmDownload = p.IsOptionDefined(Settings.Constants.NoConfirmDownload);
				Settings.NoConfirmUpload = p.IsOptionDefined(Settings.Constants.NoConfirmUpload);
				Settings.NoConfirmDelete = p.IsOptionDefined(Settings.Constants.NoConfirmDelete);
				Settings.RunOnce = p.IsOptionDefined(Settings.Constants.RunOnce);
				Settings.DownloadAll = p.IsOptionDefined(Settings.Constants.DownloadAll);
				Settings.UploadOnly = p.IsOptionDefined(Settings.Constants.UploadOnly);
				Settings.ListRemoteFiles = p.IsOptionDefined(Settings.Constants.ListRemoteFiles);

				if (p.IsOptionDefined(Settings.Constants.WatchDir))
					Settings.WatchDir = p.GetOptionStringValue(Settings.Constants.WatchDir);

				if (p.IsOptionDefined(Settings.Constants.OwnCloudUrl))
					Settings.OwnCloudUrl = p.GetOptionStringValue(Settings.Constants.OwnCloudUrl);
				if (string.IsNullOrEmpty(Settings.OwnCloudUrl) || !Settings.OwnCloudUrl.StartsWith("http"))
					throw new Exception("Invalid OwnCloudUrl");

				if (p.IsOptionDefined(Settings.Constants.SleepSeconds))
					Settings.SleepSeconds = Convert.ToInt32(p.GetOptionStringValue(Settings.Constants.SleepSeconds));
				if (p.IsOptionDefined(Settings.Constants.DownloadAllPrefix))
					Settings.DownloadAllPrefix = p.GetOptionStringValue(Settings.Constants.DownloadAllPrefix);

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
			//System.Net.ServicePointManager.ServerCertificateValidationCallback = Validator;
			try
			{
				NLogger.Current.Trace("Getting Settings");
				if (!SetSettings(args))
					return;

				DisplayCurrentSettings();

				NLogger.Current.Info("Logging In");
				if (!OwnCloudClient.Login(Settings.UserName, Settings.Password))
				{
					NLogger.Current.Warn("Invalid username or password");
					return;
				}


				if (Settings.ListRemoteFiles)
				{
					OwnCloudClient.PrintRemoteFileList();
					return;
				}

				if (Settings.DownloadAll || !string.IsNullOrEmpty(Settings.DownloadAllPrefix))
				{
					//OwnCloudClient.DownloadAll("vccdrom~");
					ConsoleColor currentColor = Console.ForegroundColor;
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("Warning: This may overwrite files in your local directory: Continue? [y/n]: ");
					Console.ForegroundColor = currentColor;

					ConsoleKeyInfo k = Console.ReadKey();
					Console.WriteLine();

					if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					{
						if (!string.IsNullOrEmpty(Settings.DownloadAllPrefix))
							OwnCloudClient.DownloadAll(Settings.DownloadAllPrefix);
						else
							OwnCloudClient.DownloadAll();
					}

					return; //bail out of program
				}

				NLogger.Current.Trace("Loading locals");
				List<FileInfoX> localFiles = OwnCloudClient.GetLocalFileList();

				NLogger.Current.Trace("Loading Remotes");
				List<FileInfoX> remoteFiles = OwnCloudClient.GetRemoteFileList();

				//outdated checks only need to run at the beginning since we are checking with lastSweep
				//NOTE: if used with multiple users on the same cloud account this becomes problematic
				int updatedRemoteFiles = FileHelpers.ReplaceOutDatedRemoteFiles(localFiles, remoteFiles, !Settings.NoConfirmUpload);
				if (updatedRemoteFiles > 0)
				{
					NLogger.Current.Trace("Refreshing remotes");
					remoteFiles = OwnCloudClient.GetRemoteFileList(); //refresh
				}

				if (!Settings.UploadOnly)
					FileHelpers.ReplaceOutDatedLocalFiles(localFiles, remoteFiles, !Settings.NoConfirmDownload);

				DateTime lastSweep = DateTime.Now;
				while (true)
				{
					NLogger.Current.Trace("Refreshing locals");
					localFiles = OwnCloudClient.GetLocalFileList();

					int uploadCount = FileHelpers.UploadNewLocalFiles(localFiles, remoteFiles, !Settings.NoConfirmUpload, lastSweep);

					int deleteCount = 0;
					if (!Settings.UploadOnly)
						 deleteCount = FileHelpers.DeleteRemoteFiles(localFiles, remoteFiles, !Settings.NoConfirmDelete);

					if (Settings.RunOnce)
						return; //bail out of program

					if (uploadCount > 0 || deleteCount > 0)
					{
						NLogger.Current.Trace("Refreshing remotes");
						remoteFiles = OwnCloudClient.GetRemoteFileList();
					}

					lastSweep = DateTime.Now;
					NLogger.Current.Trace(string.Format("Sleeping for {0} seconds", Settings.SleepSeconds));
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
