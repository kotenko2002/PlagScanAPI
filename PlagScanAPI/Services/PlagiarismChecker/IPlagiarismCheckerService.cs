namespace PlagScanAPI.Services.PlagiarismChecker
{
    public interface IPlagiarismCheckerService
    {
        void CheckPlagiarism(string projectName, string username);
    }
}
