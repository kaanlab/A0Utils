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
            catch (DirectoryNotFoundException ex)
            {
                Log.Error(ex, "Директория не найдена");
                return [];
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, "Доступ запрещен");
                return [];
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при поиске файлов лицензии");
                return [];
            }
        }

        public Result CopyToAllFolders(string downloadLicensePath, IEnumerable<string> destinationDirectories)
        {
            var failures = new List<string>();
            foreach (var destinationDir in destinationDirectories)
            {
                try
                {
                    if (File.Exists(downloadLicensePath))
                    {
                        try
                        {
                            var attributes = File.GetAttributes(downloadLicensePath);
                            if (attributes.HasFlag(FileAttributes.ReadOnly))
                            {
                                attributes &= ~FileAttributes.ReadOnly; // Убираем атрибут ReadOnly
                                File.SetAttributes(downloadLicensePath, attributes);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Не удалось изменить атрибуты файла");
                            failures.Add($"Не удалось изменить атрибуты файла: {ex.Message}");
                        }
                    }

                    string destinationFile = Path.Combine(destinationDir, Path.GetFileName(downloadLicensePath));

                    File.Copy(downloadLicensePath, destinationFile, true); // true allows overwriting
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Error(ex, "Не хватает разрешений на запись файла");
                    failures.Add($"Не хватает разрешений на запись файла: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Log.Error(ex, "Фаил занят");
                    failures.Add($"Фаил занят: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при копировании лицензии");
                   failures.Add($"Ошибка при копировании лицензии {ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                var list = string.Join(", ", failures);
                return Result.Failure($"Ошибки при копировании лицензий: {list}");
            }

            return Result.Success();
        }
    }
}
