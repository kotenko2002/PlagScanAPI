using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlagScanAPI.Infrastructure.Extensions;
using PlagScanAPI.Services.PlagiarismChecker;
using PlagScanAPI.Services.ProjectsStorage;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

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

            _plagCheckerService.CheckPlagiarism(projectName, username);

            return Ok();
        }

        [HttpGet("downloadDirectory/{directoryName}")]
        public IActionResult DownloadDirectory(string directoryName)
        {
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", directoryName);

            if (!Directory.Exists(directoryPath))
            {
                return NotFound("Directory not found");
            }

            var outputStream = new MemoryStream();

            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = file.Substring(directoryPath.Length + 1);
                    var zipEntry = zipArchive.CreateEntry(relativePath);

                    using (var zipStream = zipEntry.Open())
                    using (var fileStream = System.IO.File.OpenRead(file))
                    {
                        fileStream.CopyTo(zipStream);
                    }
                }
            }

            outputStream.Position = 0;

            string zipName = directoryName.Substring(directoryName.IndexOf("\\") + 1);
            return File(outputStream, "application/octet-stream", $"{zipName}.zip");
        }
    }
}
