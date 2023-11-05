using PlagScanAPI.Services.ProjectsStorage;

namespace PlagScanAPI.Services.PlagiarismChecker
{
    public class PlagiarismCheckerService : IPlagiarismCheckerService
    {
        private readonly IProjectsStorageService _projectsStorageService;

        public PlagiarismCheckerService(IProjectsStorageService projectsStorageService)
        {
            _projectsStorageService = projectsStorageService;
        }

        public void CheckPlagiarism(string projectName, string username)
        {
            string[] newProjectLines = _projectsStorageService.GetAllLines(Path.Combine(username, projectName));

            string[] pathsToAnotherUsersDirs = _projectsStorageService.GetPathsToUsersDirectories(username);

            var list = new List<(string pathToProject, double plagiarismPercentage)>();
            foreach (var userDir in pathsToAnotherUsersDirs)
            {
                string[] pathsToUserProjects = _projectsStorageService.GetAllUserProjects(userDir);

                foreach (var pathsToOldProject in pathsToUserProjects)
                {
                    string[] oldProjectLines = _projectsStorageService.GetAllLines(pathsToOldProject, false);

                    int identicalLinesAmount = newProjectLines.Intersect(oldProjectLines).Count();
                    double plagiarismPercentage = Math.Round((identicalLinesAmount / (double)oldProjectLines.Length) * 100, 2);

                    list.Add((GetLastTwoFolders(pathsToOldProject), plagiarismPercentage));
                }
            }

            foreach (var item in list)
            {
                Console.WriteLine($"project \"{item.pathToProject}\" has {item.plagiarismPercentage}% of plagiarism");
            }
        }

        private string GetLastTwoFolders(string fullPath)
        {
            var directories = fullPath.Split(Path.DirectorySeparatorChar);
            return string.Join(Path.DirectorySeparatorChar.ToString(), directories.Skip(Math.Max(0, directories.Length - 2)));
        }
    }
}
