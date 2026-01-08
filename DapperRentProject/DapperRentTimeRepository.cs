using Microsoft.Data.SqlClient;
using RentProject.Domain;
using Dapper;


namespace RentProject.Repository
{
    public class DapperRentTimeRepository
    {
        private readonly string _connectionString;

        public DapperRentTimeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 連線測試
        public string TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                connection.Open();

                int result = connection.ExecuteScalar<int>("SELECT 1;");

                return result == 1
                    ? "OK：連線成功，且可執行 SQL (SELECT 1 回傳1)"
                    : $"連線成功，但 SELECT 1 回傳非預期值：{result}";
            }
            catch (Exception ex)
            {
                return $"連線失敗：{ex.GetType().Name} - {ex.Message}";
            }
        }

        // 新增租時單
        public CreateRentTimeResult CreateRentTime(RentTime model)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                var insertSql = @"
                INSERT INTO dbo.RentTimes
                (
                    JobId, BookingNo, CreatedBy, Area, CustomerName, Sales, ProjectNo, ProjectName, PE, Location,
                    ContactName, Phone, TestInformation, EngineerName, SampleModel, SampleNo,
                    TestMode, TestItem, Notes, 
                    StartDate, EndDate, StartTime, EndTime, EstimatedMinutes, EstimatedHours,
                    HasLunch, LunchMinutes, HasDinner, DinnerMinutes
                )
                OUTPUT INSERTED.RentTimeId
                VALUES
                (
                    @JobId, NULL, @CreatedBy, @Area, @CustomerName, @Sales, @ProjectNo, @ProjectName, @PE, @Location,
                    @ContactName, @Phone, @TestInformation, @EngineerName, @SampleModel, @SampleNo,
                    @TestMode, @TestItem, @Notes, 
                    @StartDate, @EndDate, @StartTime, @EndTime, @EstimatedMinutes, @EstimatedHours,
                    @HasLunch, @LunchMinutes, @HasDinner, @DinnerMinutes
                );";

                int rentTimeId = connection.ExecuteScalar<int>(insertSql, new
                {
                    model.CreatedBy,
                    model.Area,
                    model.CustomerName,
                    model.Sales,
                    model.JobId,
                    model.ProjectNo,
                    model.ProjectName,
                    model.PE,
                    model.Location,

                    model.ContactName,
                    model.Phone,
                    model.TestInformation,
                    model.EngineerName,
                    model.SampleModel,
                    model.SampleNo,
                    model.TestMode,
                    model.TestItem,
                    model.Notes,

                    model.StartDate,
                    model.EndDate,
                    model.StartTime,
                    model.EndTime,
                    model.EstimatedMinutes,
                    model.EstimatedHours,

                    model.HasLunch,
                    model.LunchMinutes,
                    model.HasDinner,
                    model.DinnerMinutes,

                }, transaction: tx);

                string bookingNo = $"RF-{rentTimeId:D8}";

                var updateSql = @"UPDATE dbo.RentTimes 
                                SET BookingNo = @BookingNo
                                WHERE RentTimeId = @RentTimeId;";

                connection.Execute(updateSql, new
                {
                    BookingNo = bookingNo,
                    RentTimeId = rentTimeId
                }, transaction: tx);

                tx.Commit();

                return new CreateRentTimeResult
                {
                    RentTimeId = rentTimeId,
                    BookingNo = bookingNo,
                };
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // 取得未刪除的所有案件
        public List<RentTime> GetActiveRentTimesForProjectView()
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"SELECT
                        RentTimeId, BookingNo, Area, Location, CustomerName, ContactName, 
                        Phone, PE, Sales, StartDate, EndDate, StartTime, EndTime, 
                        ProjectNo, ProjectName
                        FROM dbo.RentTimes
                        WHERE IsDeleted = 0
                        ORDER BY CreatedDate DESC;";

            return connection.Query<RentTime>(sql).ToList();
        }

        // 透過案件編號查詢租時單
        public RentTime? GetRentTimeById(int rentTimeId)
        { 
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"SELECT 
                        r.RentTimeId, r.BookingNo, r.Area, r.CustomerName, r.Sales, r.CreatedBy,
                        r.ContactName, r.Phone, r.TestInformation, r.ProjectNo, r.ProjectName,
                        r.PE, r.Location, r.StartDate, r.EndDate, r.StartTime, r.EndTime, r.HasLunch,
                        r.LunchMinutes, r.HasDinner, r.DinnerMinutes, r.EngineerName, r.SampleModel,
                        r.SampleNo, r.TestMode, r.TestItem, r.Notes, r.JobId, jm.JobNo
                        FROM dbo.RentTimes r
                        LEFT JOIN dbo.JobNoMaster jm ON jm.JobId = r.JobId
                        WHERE r.RentTimeId = @RentTimeId;";

            return connection.QueryFirstOrDefault<RentTime>(sql, new { RentTimeId = rentTimeId });
        }

        // 更新租時單
        public int UpdateRentTime(RentTime model)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"UPDATE dbo.RentTimes
                        SET 
                        Area = @Area,
                        CustomerName = @CustomerName,
                        Sales = @Sales,
                        CreatedBy = @CreatedBy,
                        ContactName = @ContactName,
                        Phone = @Phone,
                        TestInformation = @TestInformation,
                        JobId = @JobId,
                        ProjectNo = @ProjectNo,
                        ProjectName = @ProjectName,
                        PE = @PE,
                        Location = @Location,
                        StartDate = @StartDate,
                        EndDate = @EndDate,
                        StartTime = @StartTime,
                        EndTime = @EndTime,
                        HasLunch = @HasLunch,
                        LunchMinutes = @LunchMinutes,
                        HasDinner = @HasDinner,
                        DinnerMinutes = @DinnerMinutes,
                        EngineerName = @EngineerName,
                        SampleModel = @SampleModel,
                        SampleNo = @SampleNo,
                        TestMode = @TestMode,
                        TestItem = @TestItem,
                        Notes = @Notes,
                        EstimatedMinutes = @EstimatedMinutes,
                        EstimatedHours = @EstimatedHours,
                        ModifiedBy = @ModifiedBy,
                        ModifiedDate = @ModifiedDate
                        WHERE RentTimeId = @RentTimeId;";

            return connection.Execute(sql, model);
        }

        public int DeletedRentTime(int rentTimeId, string createdBy, DateTime modifiedDate)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"UPDATE dbo.RentTimes
                        SET IsDeleted = 1,
                            ModifiedBy = @ModifiedBy,
                            ModifiedDate = @ModifiedDate
                        WHERE RentTimeId = @RentTimeId;";

            return connection.Execute(sql, new { 
                RentTimeId = rentTimeId ,
                ModifiedBy = createdBy,
                ModifiedDate = modifiedDate
            });
        }
    }
}
