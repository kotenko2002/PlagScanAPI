using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlagScanAPI.Infrastructure.Extensions;
using PlagScanAPI.Services.PlagiarismChecker;
using PlagScanAPI.Services.PlagiarismChecker.Views;
using PlagScanAPI.Services.ProjectsStorage;

namespace PlagScanAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPlagiarismCheckerService _plagCheckerService;
        private readonly IProjectsStorageService _projectsStorageService; 

        public ProjectController(
            IHttpContextAccessor httpContextAccessor,
            IPlagiarismCheckerService plagCheckerService,
            IProjectsStorageService projectsStorageService)
        {
            _httpContextAccessor = httpContextAccessor;
            _plagCheckerService = plagCheckerService;
            _projectsStorageService = projectsStorageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadProject(IFormFile projectFile)
        {
            if (projectFile == null || projectFile.Length == 0)
                return Content("File not selected");

            if (!Path.GetExtension(projectFile.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Uploaded file is not a ZIP file");

            string username = _httpContextAccessor.HttpContext.User.GetUsername();
            string projectName = await _projectsStorageService.UnzipAndUploadProject(projectFile, username);

            List<CheckResultView> result = _plagCheckerService.CheckPlagiarism(projectName, username);

            return Ok(result);
        }

        [HttpGet("download/{pathToProject}")]
        public IActionResult DownloadDirectory(string pathToProject)
        {
            MemoryStream project = _projectsStorageService.DownloadProject(pathToProject);

            string zipName = pathToProject.Substring(pathToProject.IndexOf("\\") + 1);
            return File(project, "application/octet-stream", $"{zipName}.zip");
        }
    }
}
