using RentProject.Domain;

namespace RentProject.Service
{
    public class FakeJobNoApiClient : IJobNoApiClient
    {
        public async Task<JobNoMaster?> GetJobNoMasterAsync(string jobNo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(jobNo)) return null;

            // 模擬網路延遲
            await Task.Delay(300, ct);

            // ToUpperInvariant()
            jobNo = jobNo.Trim();

            // 完整資料（測「完整 -> 鎖住」）
            if (jobNo == "JOB0001")
            {
                return new JobNoMaster
                {
                    JobNo = "JOB0001",
                    ProjectNo = "P-1001",
                    ProjectName = "WiFi7 認證專案",
                    PE = "Alex",
                    CustomerName = "ACME",
                    Sales = "Brian",
                    SampleNo = "S-01",
                    SampleModel = "Model-X",
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
            }

            // 不完整資料（測「不完整 -> 部分回填 + 回填欄位鎖住、其餘可填」）
            if (jobNo == "JOB0002")
            {
                return new JobNoMaster
                {
                    JobNo = "JOB0002",
                    ProjectNo = "P-2002",
                    ProjectName = "LTE RedCap",
                    PE = "Cindy",
                    CustomerName = "FOO INC",
                    // Sales 故意缺
                    SampleNo = "S-88",
                    // SampleModel 故意缺
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
            }
            // 查不到
            return null;
        }
    }
}
