using A0Utils.Wpf.Services;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace A0Utils.Wpf.Models
{
    public sealed class DownloadModel
    {
        public string LicensePath { get; set; }
        public string DescriptionPath { get; set; }
    }

    public static class DownloadModelExtentions
    {
        public static Result CopyToAllFolders(this DownloadModel model, FileOperationsService fileOperationsService, string a0InstallationPath)
        {
            var licenses = fileOperationsService.FindAllLicFiles(a0InstallationPath);
            var destinationDirs = licenses.Select(x => x.DirectoryPath).Distinct();
            if (!destinationDirs.Any())
            {
                destinationDirs = Directory.GetDirectories(a0InstallationPath, "bin", SearchOption.AllDirectories);
            }

            var copyLicenseResult = fileOperationsService.CopyToAllFolders(model.LicensePath, destinationDirs);
            if (copyLicenseResult.IsFailure)
            {
                return Result.Failure(copyLicenseResult.Error);
            }

            var copyDescriptionResult = fileOperationsService.CopyToAllFolders(model.DescriptionPath, destinationDirs);
            if (copyDescriptionResult.IsFailure)
            {
                return Result.Failure(copyDescriptionResult.Error);
            }

            return Result.Success();
        }
    }
}
