using Dapper;
using Microsoft.Data.SqlClient;
using RentProject.Domain;

namespace RentProject.Repository
{
    public class DapperJobNoRepository
    {
        private readonly string _connectionString;

        public DapperJobNoRepository(string connectionString)
        { 
            _connectionString = connectionString;
        }

        public int GetOrCreateJobId(string jobNo)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.JobNoMaster WHERE JobNo = @JobNo)
                BEGIN
                    INSERT INTO dbo.JobNoMaster(JobNo) VALUES(@JobNo);
                END
                
                SELECT JobId FROM dbo.JobNoMaster WHERE JobNo = @JobNo;";

            return connection.ExecuteScalar<int>(sql, new { JobNo = jobNo });

        }

        public async Task<List<string>> GetActiveJobNosAsync(int top = 8, CancellationToken ct = default)
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync(ct);

            var sql = @"
                SELECT TOP (@Top) JobNo
                FROM dbo.JobNoMaster 
                WHERE IsActive = 1
                ORDER BY 
                    ISNULL(ModifiedAt, CreatedAt) DESC,
                    JobId DESC;";

            var cmd = new CommandDefinition(sql, new { Top = top }, cancellationToken: ct);

            var rows = await connection.QueryAsync<string>(cmd);

            return rows.ToList();
        }

        // 「同一次存檔」要把 JobNoMaster Upsert + RentTimes Insert 放在同一個 tx
        // 最後回傳 JobId
        public int UpsertJobNoMasterOverwrite(JobNoMaster m)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"
                IF EXISTS (SELECT 1 FROM dbo.JobNoMaster WHERE JobNo = @JobNo)
                BEGIN
                    Update dbo.JobNoMaster
                    SET
                        ProjectNo = @ProjectNo,
                        ProjectName = @ProjectName,
                        PE = @PE,
                        CustomerName = @CustomerName,
                        Sales = @Sales,
                        SampleNo = @SampleNo,
                        SampleModel = @SampleModel,
                        ModifiedAt = SYSDATETIME(),
                        IsActive = 1
                    WHERE JobNo = @JobNo;
                    
                    SELECT JobId FROM dbo.JobNoMaster WHERE JobNo = @JobNo;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.JobNoMaster
                    (
                        JobNo, ProjectNo, ProjectName, PE, CustomerName, Sales, SampleNo, SampleModel,
                        CreatedAt, ModifiedAt, IsActive
                    )
                    VALUES
                    (
                        @JobNo, @ProjectNo, @ProjectName, @PE, @CustomerName, @Sales, @SampleNo, @SampleModel,
                        SYSDATETIME(), SYSDATETIME(), 1
                    );
                
                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                END";

            return connection.ExecuteScalar<int>(sql, new
            {
                m.JobNo,
                m.ProjectNo,
                m.ProjectName,
                m.PE,
                m.CustomerName,
                m.Sales,
                m.SampleNo,
                m.SampleModel
            });
        }

        // 測試、單純查資料用
        public JobNoMaster? GetJobNoMasterByJobNo(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo)) return null;

            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            return GetJobNoMasterByJobNo(jobNo.Trim(), connection, null);
        }

        // 這個版本：給「外部交易」用（之後會在同一個 tx 裡一起做 Upsert + Insert RentTimes）
        public JobNoMaster? GetJobNoMasterByJobNo(string jobNo, SqlConnection connection, SqlTransaction? tx)
        { 
            var sql = @"
                SELECT TOP 1
                    JobId, JobNo,
                    ProjectNo, ProjectName, PE,
                    CustomerName, Sales,
                    SampleNo, SampleModel,
                    CreatedAt, ModifiedAt, IsActive
                FROM dbo.JobNoMaster
                WHERE JobNo = @JobNo;";

            return connection.QueryFirstOrDefault<JobNoMaster>(sql, new { JobNo = jobNo }, tx);
        }
    }
}
