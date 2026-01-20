using RentProject.Domain;
using RentProject.Repository;

namespace RentProject.Service
{
    public class JobNoService
    {
        private readonly DapperJobNoRepository _jobNoRepository;
        private readonly IJobNoApiClient _api;

        public JobNoService(DapperJobNoRepository jobNoRepository, IJobNoApiClient api)
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

        public List<string> GetActiveJobNos(int top = 8)
        {
            return _jobNoRepository.GetActiveJobNos(top);
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

            var apiData = await _api.GetJobNoMasterAsync(jobNo, ct);
            if (apiData == null) return null;

            // 確保 JobNo 格式一致
            apiData.JobNo = jobNo;

            // API 當真相：覆蓋式 Upsert
            _jobNoRepository.UpsertJobNoMasterOverwrite(apiData);

            return _jobNoRepository.GetJobNoMasterByJobNo(jobNo);
        }
    }
}
