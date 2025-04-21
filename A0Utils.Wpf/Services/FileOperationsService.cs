using Microsoft.Extensions.Logging;
using System;
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

        public bool IsFolderExist(string path)
        {
            return System.IO.Directory.Exists(path);
        }

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
                _logger.LogError(ex, "Ошибка при поиске файла лицензии: {message}", ex.Message);
                return string.Empty;
            }
        }
    }
}
