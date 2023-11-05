using PlagScanAPI.Infrastructure.Exceptions;
using System.IO.Compression;
using System.Net;

namespace PlagScanAPI.Services.ProjectsStorage
{
    public class LocalProjectsStorage : IProjectsStorageService
    {
        private readonly string basePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public async Task<string> UnzipAndUploadProject(IFormFile projectFile, string userDirectoryName)
        {
            using (var memoryStream = new MemoryStream())
            {
                await projectFile.CopyToAsync(memoryStream);

                using (var archive = new ZipArchive(memoryStream))
                {
                    string newDirectoryPath = Path.Combine(basePath, userDirectoryName);

                    //TODO: add check does zip containt folder wrapper. if no -> add one with random name

                    string firstDirectoryInZip = archive.Entries.First().FullName.Split('/')[0];
                    string projectDirectoryPath = Path.Combine(newDirectoryPath, firstDirectoryInZip);
                    if (Directory.Exists(projectDirectoryPath))
                    {
                        Directory.Delete(projectDirectoryPath, true);
                    }

                    archive.ExtractToDirectory(newDirectoryPath);

                    return firstDirectoryInZip;
                }
            }
        }
        public MemoryStream DownloadProject(string pathToProject)
        {
            var directoryPath = Path.Combine(basePath, pathToProject);

            if (!Directory.Exists(directoryPath))
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Project with this path was not found!");
            }

            var outputStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = file.Substring(directoryPath.Length + 1);
                    var zipEntry = zipArchive.CreateEntry(relativePath);

                    using (var zipStream = zipEntry.Open())
                    using (var fileStream = File.OpenRead(file))
                    {
                        fileStream.CopyTo(zipStream);
                    }
                }
            }

            outputStream.Position = 0;

            return outputStream;
        }

        public string[] GetPathsToUsersDirectories(string ignoreDirectory)
        {
            return Directory.GetDirectories(basePath)
                .Where(dirName => dirName != Path.Combine(basePath, ignoreDirectory))
                .ToArray();
        }
        public string[] GetAllUserProjects(string pathToUserDirectory)
        {
            return Directory.GetDirectories(pathToUserDirectory).ToArray();
        }
        public string[] GetAllLines(string pathToProject, bool addBasePath = true)
        {
            string path = addBasePath ? Path.Combine(basePath, pathToProject) : pathToProject;

            var allLines = new List<string>();
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    allLines.AddRange(lines);
                }
            }

            return allLines.Distinct().ToArray();
        }
    }
}
