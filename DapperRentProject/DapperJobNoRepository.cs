using Dapper;
using Microsoft.Data.SqlClient;

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

        public List<string> GetActiveJobNos()
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"
                SELECT JobNo
                FROM dbo.JobNoMaster 
                WHERE IsActive = 1
                ORDER BY JobNo;";

            return connection.Query<string>(sql).ToList();
        }
    }
}
