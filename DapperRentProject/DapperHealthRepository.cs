using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentProject.Repository
{
    public sealed class DapperHealthRepository
    {
        private readonly string _connectionString;

        public DapperHealthRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public (bool OK, string Message) TestDbConnection()
        {
            try
            {
                using var connectionString = new SqlConnection(_connectionString);
                connectionString.Open();

                var result = connectionString.ExecuteScalar<int>("SELECT 1;");

                if(result == 1)
                    return (true, "OK：DB 連線成功，且可執行 SQL（SELECT 1 回傳 1）");

                return (false, $"DB 連線成功，但 SELECT 1 回傳非預期值：{result}");
            }
            catch (Exception ex)
            {
                return (false, $"DB 連線失敗：{ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
