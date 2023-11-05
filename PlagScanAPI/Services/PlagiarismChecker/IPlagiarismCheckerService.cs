using PlagScanAPI.Services.PlagiarismChecker.Views;

namespace PlagScanAPI.Services.PlagiarismChecker
{
    public interface IPlagiarismCheckerService
    {
        List<CheckResultView> CheckPlagiarism(string projectName, string username);
    }
}
