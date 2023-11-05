using System.IO.Compression;

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
