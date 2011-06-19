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
		enum ConfirmAnswer { Confirm, Skip, ConfirmAll, SkipAll };

		static List<FileInfoX> LoadLocals()
		{
			List<FileInfoX> files = new List<FileInfoX>();
			foreach (var item in System.IO.Directory.EnumerateFileSystemEntries(Settings.WatchDir, "*", System.IO.SearchOption.AllDirectories))
			{
				System.IO.FileInfo info = new System.IO.FileInfo(item);
				if (!info.Exists)
					continue;
				else if (System.Text.RegularExpressions.Regex.IsMatch(item, @"\.enc\.\d{12}$"))
					continue;

				FileInfoX x = new FileInfoX();
				DateTime lastWrite = new DateTime(info.LastWriteTime.Year, info.LastWriteTime.Month, info.LastWriteTime.Day, info.LastWriteTime.Hour, info.LastWriteTime.Minute, info.LastWriteTime.Second); //do it this way to avoid milisecond comparison problemsinfo.LastWriteTime;

				//!FileNameProcessing
				x.CloudName = item.Replace(Settings.WatchDir, "").Replace('\\', '~') + ".enc";
				x.LastModified = lastWrite;
				x.FileName = item.Replace(Settings.WatchDir, "");
				x.CloudNamePlusDate = x.CloudName + "." + OwnCloudClient.GetUnixTimeStamp(lastWrite);
				files.Add(x);
			}
			return files;
		}

		static ConfirmAnswer GetAnswer(string fileName)
		{
			Console.Write(" [c]onfirm/[s]kip/confirm [a]ll/s[k]ip all: ");
			ConsoleKeyInfo k = Console.ReadKey();
			Console.WriteLine();
			switch (Convert.ToChar(k.Key))
			{
				case 'C':
				case 'c':
					return ConfirmAnswer.Confirm;
				case 'S':
				case 's':
					return ConfirmAnswer.Skip;
				case 'A':
				case 'a':
					return ConfirmAnswer.ConfirmAll;
				case 'K':
				case 'k':
					return ConfirmAnswer.SkipAll;
				default:
					return GetAnswer(fileName);
			}
		}

		static List<FileInfoX> FillConfirmationList(List<FileInfoX> inList)
		{
			bool assumeConfirmed = false;
			List<FileInfoX> confirmedList = new List<FileInfoX>();

			foreach (FileInfoX x in inList)
			{
				Console.Write(x.FileName + " ");
				if (assumeConfirmed)
				{
					Console.WriteLine();
					confirmedList.Add(x);
					continue;
				}

				ConfirmAnswer a = GetAnswer(x.FileName);
				if (a == ConfirmAnswer.Confirm)
					confirmedList.Add(x);
				else if (a == ConfirmAnswer.Skip)
					continue;
				else if (a == ConfirmAnswer.SkipAll)
					break;
				else if (a == ConfirmAnswer.ConfirmAll)
				{
					assumeConfirmed = true;
					confirmedList.Add(x);
				}
			}
			return confirmedList;
		}

		//files that have older versions in the cloud -- upload the new ones and delete the old outdated cloud files
		static int ProcessOutDatedRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmUpload)
		{
			var remoteQuery = (from l in localFiles
							  join r in remoteFiles on l.CloudName equals r.CloudName
							  where (l.LastModified - r.LastModified).Seconds >= 2
							  select new { OldCloudNamePlusDate = r.CloudNamePlusDate, NewFileName = r.FileName }).ToList();

			bool assumeConfirmed = !confirmUpload;
			Dictionary<string, string> confirmed = new Dictionary<string, string>();

			if (remoteQuery.Count > 0)
			{
				Console.WriteLine("Found Uploads:");
				foreach (var xyz in remoteQuery)
					Console.WriteLine(xyz.NewFileName);
			}

			foreach (var xyz in remoteQuery)
			{
				Console.Write(xyz.NewFileName + " ");
				if (assumeConfirmed)
				{
					Console.WriteLine();
					confirmed.Add(xyz.OldCloudNamePlusDate, xyz.NewFileName);
					continue;
				}

				ConfirmAnswer a = GetAnswer(xyz.NewFileName);
				if (a == ConfirmAnswer.Confirm)
					confirmed.Add(xyz.OldCloudNamePlusDate, xyz.NewFileName);
				else if (a == ConfirmAnswer.Skip)
					continue;
				else if (a == ConfirmAnswer.SkipAll)
					break;
				else if (a == ConfirmAnswer.ConfirmAll)
				{
					assumeConfirmed = true;
					confirmed.Add(xyz.OldCloudNamePlusDate, xyz.NewFileName);
				}
			}

			foreach (var d in confirmed)
			{
				OwnCloudClient.DeleteFile(d.Key); //OldNameCloudName
				OwnCloudClient.UploadFile(Settings.WatchDir + d.Value); //NewFileName
			}
			return confirmed.Count;
		}

		//files that have a newer version in the cloud -- download them and replace local files
		static int ProcessOutDatedLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDownload)
		{
			var localQuery = (from l in localFiles
							  join r in remoteFiles on l.FileName equals r.FileName
							  where (r.LastModified - l.LastModified).Seconds >= 2
							  select r).ToList();

			if (localQuery.Count > 0)
			{
				Console.WriteLine("Found Downloads:");
				foreach (var x in localQuery)
					Console.WriteLine(x.FileName);
			}

			List<FileInfoX> confirmedToDownload = localQuery;
			if (confirmDownload)
				confirmedToDownload = FillConfirmationList(localQuery);

			foreach (var x in confirmedToDownload)
				OwnCloudClient.Download(x.CloudNamePlusDate);

			return confirmedToDownload.Count;
		}

		//files that exist on disk and not in the cloud -- assume new file and upload
		//files that exist on disk and the lastmodified date is greater than our last sweep -- assume locally modified and upload
		static int ProcessNewLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool askUpload, DateTime lastSweep)
		{
			var toUpload = localFiles.Where(x => !remoteFiles.Select(y => y.CloudName).Contains(x.CloudName) || x.LastModified > lastSweep).ToList();

			if (toUpload.Count > 0)
			{
				Console.WriteLine("Found Uploads:");
				foreach (var u in toUpload)
					Console.WriteLine(u.FileName);
			}

			List<FileInfoX> confirmedToUpload = toUpload;
			if (askUpload)
				confirmedToUpload = FillConfirmationList(toUpload);

			foreach (var x in confirmedToUpload)
			{
				if (x.LastModified > lastSweep)
				{
					var toDeleteX = remoteFiles.Where(y => y.CloudName == x.CloudName).FirstOrDefault();
					OwnCloudClient.DeleteFile(toDeleteX.CloudNamePlusDate);
				}
				string f = Settings.WatchDir + x.FileName;
				UploadFileStatus status = OwnCloudClient.UploadFile(f);
			}
			return confirmedToUpload.Count;
		}

		//files that exist in the cloud but not on disk -- assume they were deleted
		static int ProcessDeleteRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDelete)
		{
			var toDelete = remoteFiles.Where(x => !localFiles.Select(y => y.CloudName).Contains(x.CloudName)).ToList();
			if (toDelete.Count > 0)
			{
				Console.WriteLine("Found Deletes:");
				foreach (var d in toDelete)
					Console.WriteLine(d.FileName);
			}

			List<FileInfoX> confirmedToDelete = toDelete;
			if (confirmDelete)
				confirmedToDelete = FillConfirmationList(toDelete);

			foreach (var x in confirmedToDelete)
			{
				if (string.IsNullOrWhiteSpace(x.CloudName))
					continue;
				OwnCloudClient.DeleteFile(x.CloudNamePlusDate);
			}
			return confirmedToDelete.Count;
		}

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

			string userName = "";
			string password = "";

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
			catch (ArgumentException exception)
			{
				Console.WriteLine(exception.Message);

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
			catch (Exception exception2)
			{
				Console.WriteLine("Fatal Error:");
				Console.WriteLine(exception2.ToString());
			}

			Console.Write("Enter UserName: ");
			userName = Console.ReadLine();
			Console.Write("Enter Password: ");
			password = ReadPassword();
			Console.WriteLine();

			Console.WriteLine("Options: ");
			Console.WriteLine("confirmdownload: " + confirmDownload);
			Console.WriteLine("confirmupload: " + confirmUpload);
			Console.WriteLine("confirmdelete: " + confirmDelete);
			Console.WriteLine("runonce: " + runOnce);
			Console.WriteLine("massdownload: " + massDownload);
			Console.WriteLine("sleepSeconds: " + sleepSeconds);
			Console.WriteLine("watchdir: " + Settings.WatchDir);
			Console.WriteLine("baseurl: " + Settings.OwnCloudUrl);

			if (!OwnCloudClient.Login(userName, password))
			{
				Console.WriteLine("Invalid username or password");
				return;
			}

			if (massDownload)
			{
				//OwnCloudClient.DownloadAll("vccdrom~");
				Console.Write("Warning: This may overwrite files in your local directory: Continue? [y/n]: ");

				ConsoleKeyInfo k = Console.ReadKey();
				Console.WriteLine();

				if (k.KeyChar == 'y' || k.KeyChar == 'Y')
					OwnCloudClient.DownloadAll();

				return; //bail out of program
			}

			NLogger.Current.Trace("Loading locals");
			List<FileInfoX> localFiles = LoadLocals();

			NLogger.Current.Trace("Loading Remotes");
			List<FileInfoX> remoteFiles = OwnCloudClient.GetFileList();

			//outdated checks only need to run at the beginning since we are checking with lastSweep
			//NOTE: if used with multiple users on the same cloud account this becomes problematic
			int updatedRemoteFiles = ProcessOutDatedRemoteFiles(localFiles, remoteFiles, confirmUpload);
			if (updatedRemoteFiles > 0)
			{
				NLogger.Current.Trace("Refreshing remotes");
				remoteFiles = OwnCloudClient.GetFileList(); //refresh
			}
			int updatedLocalFiles = ProcessOutDatedLocalFiles(localFiles, remoteFiles, confirmDownload);

			DateTime lastSweep = DateTime.Now;
			while (true)
			{
				NLogger.Current.Trace("Refreshing locals");
				localFiles = LoadLocals();

				int uploadCount = ProcessNewLocalFiles(localFiles, remoteFiles, confirmUpload, lastSweep);

				int deleteCount = ProcessDeleteRemoteFiles(localFiles, remoteFiles, confirmDelete);

				if (runOnce)
					return; //bail out of program

				if (uploadCount > 0 || deleteCount > 0)
				{
					NLogger.Current.Trace("Refreshing remotes");
					remoteFiles = OwnCloudClient.GetFileList();
				}

				lastSweep = DateTime.Now;
				System.Threading.Thread.Sleep(1000 * sleepSeconds);

			}
		}
	}
}
