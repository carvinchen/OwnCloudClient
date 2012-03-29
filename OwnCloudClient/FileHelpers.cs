using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OwnCloudClient
{
	public static class FileHelpers
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
				Console.Write(req.ToString() + " " + x.LocalFileName + " ");
				if (assumeConfirmed)
				{
					Console.WriteLine();
					confirmedList.Add(x);
					continue;
				}

				ConfirmAnswer a = GetAnswer(x.LocalFileName);
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
		public static int ReplaceOutDatedRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmUpload)
		{
			var remoteQuery = (from l in localFiles
							   join r in remoteFiles on l.CloudFileName equals r.CloudFileName
							   where (l.LastModifiedUtc - r.LastModifiedUtc).TotalSeconds >= 2
							   select new { OldCloudNamePlusDate = r.CloudFileNameWithEmbeddedData, NewFileName = r.LocalFileName }).ToList();

			bool assumeConfirmed = !confirmUpload;
			Dictionary<string, string> confirmed = new Dictionary<string, string>();

			if (remoteQuery.Count > 0)
			{
				NLogger.Current.Info("Found Uploads:");
				foreach (var uploadFile in remoteQuery)
					NLogger.Current.Info(uploadFile.NewFileName);
			}

			foreach (var uploadFile in remoteQuery)
			{
				Console.Write(ConfirmRequest.Upload.ToString() + " " + uploadFile.NewFileName + " ");
				if (assumeConfirmed)
				{
					Console.WriteLine();
					confirmed.Add(uploadFile.OldCloudNamePlusDate, uploadFile.NewFileName);
					continue;
				}

				ConfirmAnswer a = GetAnswer(uploadFile.NewFileName);
				if (a == ConfirmAnswer.Confirm)
					confirmed.Add(uploadFile.OldCloudNamePlusDate, uploadFile.NewFileName);
				else if (a == ConfirmAnswer.Skip)
					continue;
				else if (a == ConfirmAnswer.SkipAll)
					break;
				else if (a == ConfirmAnswer.ConfirmAll)
				{
					assumeConfirmed = true;
					confirmed.Add(uploadFile.OldCloudNamePlusDate, uploadFile.NewFileName);
				}
			}

			foreach (var d in confirmed)
			{
				OwnCloudClient.DeleteFile(d.Key);						//OldCloudNamePlusDate
				OwnCloudClient.UploadFile(Settings.WatchDir + d.Value); //NewFileName
			}
			return confirmed.Count;
		}

		//files that have a newer version in the cloud -- download them and replace local files
		public static int ReplaceOutDatedLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDownload)
		{
			var localQuery = from l in localFiles
							 join r in remoteFiles on l.LocalFileName equals r.LocalFileName
							 where (r.LastModifiedUtc - l.LastModifiedUtc).TotalSeconds >= 2
							 select r;

			var shouldDownloadLocalQuery = localQuery.Where(f => OwnCloudClient.ShouldDownload(f)).ToList();

			if (shouldDownloadLocalQuery.Count > 0)
			{
				NLogger.Current.Info("Found Downloads:");
				foreach (var x in localQuery)
					NLogger.Current.Info(x.LocalFileName);
			}

			List<FileInfoX> confirmedToDownload = shouldDownloadLocalQuery;
			if (confirmDownload)
				confirmedToDownload = FillConfirmationList(shouldDownloadLocalQuery, ConfirmRequest.Download);

			foreach (var x in confirmedToDownload)
				OwnCloudClient.DownloadFile(x.CloudFileNameWithEmbeddedData);

			return confirmedToDownload.Count;
		}

		//files that exist on disk and not in the cloud -- assume new file and upload
		//files that exist on disk and the lastmodified date is greater than our last sweep -- assume locally modified and upload
		public static int UploadNewLocalFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool askUpload, DateTime lastSweepUtc)
		{
			var toUpload = localFiles.Where(x => !remoteFiles.Select(y => y.CloudFileName).Contains(x.CloudFileName) || x.LastModifiedUtc > lastSweepUtc).ToList();

			if (toUpload.Count > 0)
			{
				NLogger.Current.Info("Found Uploads:");
				foreach (var u in toUpload)
					NLogger.Current.Info(u.LocalFileName);
			}

			List<FileInfoX> confirmedToUpload = toUpload;
			if (askUpload)
				confirmedToUpload = FillConfirmationList(toUpload, ConfirmRequest.Upload);

			foreach (var x in confirmedToUpload)
			{
				if (x.LastModifiedUtc > lastSweepUtc)
				{
					var toDeleteX = remoteFiles.Where(y => y.CloudFileName == x.CloudFileName).FirstOrDefault();
					OwnCloudClient.DeleteFile(toDeleteX.CloudFileNameWithEmbeddedData);
				}
				string f = Settings.WatchDir + x.LocalFileName;
				OwnCloudClient.UploadFile(f);			
			}
			return confirmedToUpload.Count;
		}

		//files that exist in the cloud but not on disk -- assume they were deleted
		public static int DeleteRemoteFiles(List<FileInfoX> localFiles, List<FileInfoX> remoteFiles, bool confirmDelete)
		{
			var toDelete = remoteFiles.Where(x => !localFiles.Select(y => y.CloudFileName).Contains(x.CloudFileName)).ToList();
			if (toDelete.Count > 0)
			{
				NLogger.Current.Info("Found Deletes:");
				foreach (var d in toDelete)
					NLogger.Current.Info(d.LocalFileName);
			}

			List<FileInfoX> confirmedToDelete = toDelete;
			if (confirmDelete)
				confirmedToDelete = FillConfirmationList(toDelete, ConfirmRequest.Delete);

			foreach (var x in confirmedToDelete)
			{
				if (string.IsNullOrEmpty(x.CloudFileName))
					continue;
				OwnCloudClient.DeleteFile(x.CloudFileNameWithEmbeddedData);
			}
			return confirmedToDelete.Count;
		}
	}
}
