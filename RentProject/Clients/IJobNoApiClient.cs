using RentProject.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RentProject.Clients
{
    public interface IJobNoApiClient
    {
        //抓 JobNoMaster（由 WebApi 做「抓外部+存DB」）
        Task<JobNoMaster?> GetJobNoMasterFromApiAndSaveAsync(string jobNo, CancellationToken ct = default);

        Task<List<string>> GetActiveJobNoAsync(int top, CancellationToken ct = default);

        // 拿 jobId（不存在就建立）
        Task<int> GetOrCreateJobIdAsync(string jobNo, CancellationToken ct = default);
    }
}
