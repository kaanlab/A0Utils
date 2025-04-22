using A0Utils.Wpf.Helpers;
using A0Utils.Wpf.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace A0Utils.Wpf.Services
{
    public sealed class FileOperationsService
    {
        private readonly ILogger<FileOperationsService> _logger;

        public FileOperationsService(ILogger<FileOperationsService> logger)
        {
            _logger = logger;
        }

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
                _logger.LogError(ex, "Ошибка при поиске файла лицензии: {Error}", ex.Message);
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
                _logger.LogError(ex, "Ошибка при поиске файлов лицензии: {Error}", ex.Message);
                return [];
            }
        }

        public void CopyToAllFolders(string downloadLicensePath, IEnumerable<string> destinationDirectories)
        {
            foreach (var destinationDir in destinationDirectories)
            {
                string destinationFile = Path.Combine(destinationDir, Path.GetFileName(downloadLicensePath));

                try
                {
                    File.Copy(downloadLicensePath, destinationFile, true); // true allows overwriting
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при копировании файлов лицензии: {Error}", ex.Message);
                }
            }
        }
    }
}
