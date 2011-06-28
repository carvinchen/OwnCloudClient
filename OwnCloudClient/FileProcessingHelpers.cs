using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OwnCloudClient
{
	public static class FileProcessingHelpers
	{
		private enum ConfirmAnswer { Confirm, Skip, ConfirmAll, SkipAll };
		private enum ConfirmRequest { Download, Upload, Delete };

		private static ConfirmAnswer GetAnswer(string fileName)
		{
			Console.Write(" [c]onfirm, [s]kip, confirm [a]ll, s[k]ip all: ");
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

		private static List<FileInfoX> FillConfirmationList(List<FileInfoX> inList, ConfirmRequest req)
		{
			bool assumeConfirmed = false;
			List<FileInfoX> confirmedList = new List<FileInfoX>();

			foreach (FileInfoX x in inList)
			{
				Console.Write(req.ToString() + " " + x.FileName + " ");
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
		public static int ProcessOutDatedRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmUpload)
		{
			var remoteQuery = (from l in localFiles
							   join r in remoteFiles on l.CloudName equals r.CloudName
							   where (l.LastModified - r.LastModified).Seconds >= 2
							   select new { OldCloudNamePlusDate = r.CloudNamePlusDate, NewFileName = r.FileName }).ToList();

			bool assumeConfirmed = !confirmUpload;
			Dictionary<string, string> confirmed = new Dictionary<string, string>();

			if (remoteQuery.Count > 0)
			{
				NLogger.Current.Info("Found Uploads:");
				foreach (var xyz in remoteQuery)
					NLogger.Current.Info(xyz.NewFileName);
			}

			foreach (var xyz in remoteQuery)
			{
				Console.Write(ConfirmRequest.Delete.ToString() + " " + xyz.NewFileName + " ");
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
		public static int ProcessOutDatedLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDownload)
		{
			var localQuery = from l in localFiles
							 join r in remoteFiles on l.FileName equals r.FileName
							 where (r.LastModified - l.LastModified).Seconds >= 2
							 select r;

			var shouldDownloadLocalQuery = localQuery.Where(f => OwnCloudClient.ShouldDownload(f)).ToList();

			if (shouldDownloadLocalQuery.Count > 0)
			{
				NLogger.Current.Info("Found Downloads:");
				foreach (var x in localQuery)
					NLogger.Current.Info(x.FileName);
			}

			List<FileInfoX> confirmedToDownload = shouldDownloadLocalQuery;
			if (confirmDownload)
				confirmedToDownload = FillConfirmationList(shouldDownloadLocalQuery, ConfirmRequest.Download);

			foreach (var x in confirmedToDownload)
				OwnCloudClient.Download(x.CloudNamePlusDate);

			return confirmedToDownload.Count;
		}

		//files that exist on disk and not in the cloud -- assume new file and upload
		//files that exist on disk and the lastmodified date is greater than our last sweep -- assume locally modified and upload
		public static int ProcessNewLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool askUpload, DateTime lastSweep)
		{
			var toUpload = localFiles.Where(x => !remoteFiles.Select(y => y.CloudName).Contains(x.CloudName) || x.LastModified > lastSweep).ToList();

			if (toUpload.Count > 0)
			{
				NLogger.Current.Info("Found Uploads:");
				foreach (var u in toUpload)
					NLogger.Current.Info(u.FileName);
			}

			List<FileInfoX> confirmedToUpload = toUpload;
			if (askUpload)
				confirmedToUpload = FillConfirmationList(toUpload, ConfirmRequest.Upload);

			foreach (var x in confirmedToUpload)
			{
				if (x.LastModified > lastSweep)
				{
					var toDeleteX = remoteFiles.Where(y => y.CloudName == x.CloudName).FirstOrDefault();
					OwnCloudClient.DeleteFile(toDeleteX.CloudNamePlusDate);
				}
				string f = Settings.WatchDir + x.FileName;
				OwnCloudClient.UploadFile(f);			
			}
			return confirmedToUpload.Count;
		}

		//files that exist in the cloud but not on disk -- assume they were deleted
		public static int ProcessDeleteRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDelete)
		{
			var toDelete = remoteFiles.Where(x => !localFiles.Select(y => y.CloudName).Contains(x.CloudName)).ToList();
			if (toDelete.Count > 0)
			{
				NLogger.Current.Info("Found Deletes:");
				foreach (var d in toDelete)
					NLogger.Current.Info(d.FileName);
			}

			List<FileInfoX> confirmedToDelete = toDelete;
			if (confirmDelete)
				confirmedToDelete = FillConfirmationList(toDelete, ConfirmRequest.Delete);

			foreach (var x in confirmedToDelete)
			{
				if (string.IsNullOrEmpty(x.CloudName))
					continue;
				OwnCloudClient.DeleteFile(x.CloudNamePlusDate);
			}
			return confirmedToDelete.Count;
		}
	}
}
