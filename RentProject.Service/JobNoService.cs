using RentProject.Domain;
using RentProject.Repository;

namespace RentProject.Service
{
    public class JobNoService
    {
        private readonly DapperJobNoRepository _jobNoRepository;
        private readonly IExternalJobNoClient _api;

        public JobNoService(DapperJobNoRepository jobNoRepository, IExternalJobNoClient api)
        {
            _jobNoRepository = jobNoRepository;
            _api = api;
        }

        public int GetOrCreateJobId(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
            {
                throw new ArgumentNullException(nameof(jobNo), "jobNo 不可為空");
            }

            return _jobNoRepository.GetOrCreateJobId(jobNo.Trim());
        }

        public Task<List<string>> GetActiveJobNosAsync(int top = 8, CancellationToken ct = default)
        {
            return _jobNoRepository.GetActiveJobNosAsync(top, ct);
        }

        public JobNoMaster? GetJobNoMasterByJobNo(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
            {
                throw new ArgumentException("jobNo 不可為空", nameof(jobNo));
            }

            return _jobNoRepository.GetJobNoMasterByJobNo(jobNo.Trim());
        }

        public async Task<JobNoMaster?> GetJobNoMasterFromApiAndSaveAsync(string jobNo, CancellationToken ct = default)
        { 
            if (string.IsNullOrWhiteSpace(jobNo)) return null;

            jobNo = jobNo.Trim();

            // 1. 先打外部 API
            var external =  await _api.GetJobNoMasterAsync(jobNo, ct);

            if (external == null) return null;

            // 2. 存回 DB（Upsert）
            _jobNoRepository.UpsertJobNoMasterOverwrite(external);

            // 3. 回傳 DB 版本（通常包含 JobId / CreatedAt / ModifiedAt 等）
            return _jobNoRepository.GetJobNoMasterByJobNo(jobNo);
        }
    }
}
