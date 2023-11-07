namespace PlagScanAPI.Services.ProjectsStorage
{
    public interface IProjectsStorageService
    {
        Task<string> UnzipAndUploadProject(IFormFile projectFile, string userDirectoryName);
        MemoryStream DownloadProject(string pathToProject);

        string[] GetPathsToUsersDirectories(string ignoreDirectory);
        string[] GetAllUserProjects(string pathToUserDirectory);
        string[] GetAllLines(string pathToProject, bool addBasePath = true);
    }
}
