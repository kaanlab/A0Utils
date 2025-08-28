using A0Utils.Wpf.Helpers;
using A0Utils.Wpf.Models;
using CSharpFunctionalExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace A0Utils.Wpf.Services
{
    public sealed class FileOperationsService
    {
        public bool IsFolderExist(string path) => System.IO.Directory.Exists(path);

        public string FindLicFile(string path)
        {
            try
            {
                string searchPattern = "*.ISL";
                return Directory
                    .EnumerateFiles(path, searchPattern)
                    .Select(Path.GetFileName)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при поиске файла лицензии: {Error}", ex.Message);
                return string.Empty;
            }
        }

        public IEnumerable<LicenseModel> FindAllLicFiles(string path)
        {
            try
            {
                string searchPattern = "*.ISL";
                var fullPaths = Directory
                    .EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
                    .ToList();

                return fullPaths.MapToLicenseModel();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при поиске файлов лицензии: {Error}", ex.Message);
                return [];
            }
        }

        public Result CopyToAllFolders(string downloadLicensePath, IEnumerable<string> destinationDirectories)
        {
            foreach (var destinationDir in destinationDirectories)
            {
                if(File.Exists(downloadLicensePath))
                {
                    var attributes = File.GetAttributes(downloadLicensePath);
                    if (attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        attributes &= ~FileAttributes.ReadOnly; // Убираем атрибут ReadOnly
                        File.SetAttributes(downloadLicensePath, attributes);
                    }
                }

                string destinationFile = Path.Combine(destinationDir, Path.GetFileName(downloadLicensePath));

                try
                {
                    File.Copy(downloadLicensePath, destinationFile, true); // true allows overwriting
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при копировании лицензии: {Error}", ex.Message);
                    return Result.Failure($"Ошибка при копировании лицензии {destinationDir}");
                }
            }

            return Result.Success();
        }
    }
}
