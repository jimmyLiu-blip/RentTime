using RentProject.Domain;
using RentProject.Repository;

namespace RentProject.Service
{
    public class JobNoService
    {
        private readonly DapperJobNoRepository _jobNoRepository;

        public JobNoService(DapperJobNoRepository jobNoRepository)
        {
            _jobNoRepository = jobNoRepository;
        }

        public int GetOrCreateJobId(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
            {
                throw new ArgumentNullException("jobNo 不可為空", nameof(jobNo));
            }

            return _jobNoRepository.GetOrCreateJobId(jobNo.Trim());
        }

        public List<string> GetActiveJobNos()
        {
            return _jobNoRepository.GetActiveJobNos();
        }

        public JobNoMaster? GetJobNoMasterByJobNo(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
            {
                throw new ArgumentException("jobNo 不可為空", nameof(jobNo));
            }

            return _jobNoRepository.GetJobNoMasterByJobNo(jobNo.Trim());
        }
    }
}
