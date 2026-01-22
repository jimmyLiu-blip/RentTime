using RentProject.Domain;

namespace RentProject.Service
{
    // CancellationToken 讓舊的查詢可以被取消，不要浪費資源，也不要回來亂改 UI。
    // = default 是什麼意思
    public interface IExternalJobNoClient
    {
        Task<JobNoMaster?> GetJobNoMasterAsync(string jobNo, CancellationToken ct = default); 
    }
}
