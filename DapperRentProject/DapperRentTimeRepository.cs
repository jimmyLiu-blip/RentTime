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
        public CreateRentTimeResult CreateRentTime(RentTime model, long? bookingBatchId = null)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                // (1) 先拿一個新的 BatchId（同一次新增跨天，都共用這個 BatchId）
                var batchIdSql = @"
                    INSERT INTO dbo.BookingBatch DEFAULT VALUES;
                    SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                long batchId;

                if (bookingBatchId.HasValue)
                {
                    batchId = bookingBatchId.Value; // 用表單先拿到的 batchId
                }
                else
                {
                    batchId = connection.ExecuteScalar<long>(batchIdSql, transaction: tx);
                }   

                // (2) 算這次要產生幾天（沒填日期就當 1 筆）
                DateTime? start = model.StartDate?.Date;
                DateTime? end = model.EndDate?.Date;

                int days = 1;
                if (start.HasValue && end.HasValue)
                {
                    if (end.Value < start.Value)
                        throw new Exception("EndDate 不可小於 StartDate");

                    days = (end.Value - start.Value).Days + 1;
                }

                // (3) 每一天都 INSERT 一筆 RentTimes，BookingNo 直接寫入（不再 UPDATE）
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
                        @JobId, @BookingNo, @CreatedBy, @Area, @CustomerName, @Sales, @ProjectNo, @ProjectName, @PE, @Location,
                        @ContactName, @Phone, @TestInformation, @EngineerName, @SampleModel, @SampleNo,
                        @TestMode, @TestItem, @Notes, 
                        @StartDate, @EndDate, @StartTime, @EndTime, @EstimatedMinutes, @EstimatedHours,
                        @HasLunch, @LunchMinutes, @HasDinner, @DinnerMinutes
                    );";

                int firstRentTimeId = 0;
                string firstBookingNo = "";

                for (int seq = 1; seq <= days; seq++)
                {
                    // 如果有跨天：每筆 StartDate/EndDate 都固定為同一天
                    DateTime? day = start.HasValue ? start.Value.AddDays(seq - 1) : (DateTime?)null;

                    // BookingNo：RF -{ BatchId(補7碼)}-{ seq}
                    string bookingNo = $"RF-{batchId:D7}-{seq}";

                    int rentTimeId = connection.ExecuteScalar<int>(insertSql, new
                    {
                        model.JobId,
                        BookingNo = bookingNo,

                        model.CreatedBy,
                        model.Area,
                        model.CustomerName,
                        model.Sales,
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

                        StartDate = day,
                        EndDate = day,
                        model.StartTime,
                        model.EndTime,
                        model.EstimatedMinutes,
                        model.EstimatedHours,

                        model.HasLunch,
                        model.LunchMinutes,
                        model.HasDinner,
                        model.DinnerMinutes,

                    }, transaction: tx);

                    // 回傳先用第一筆（避免你其他 UI/Service 大改）
                    if (seq == 1)
                    {
                        firstRentTimeId = rentTimeId;
                        firstBookingNo = bookingNo;
                    }
                }

                tx.Commit();

                return new CreateRentTimeResult
                {
                    RentTimeId = firstRentTimeId,
                    BookingNo = firstBookingNo,
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
                        ORDER BY
                        -- 主號：RF-0000009-4  取出 0000009
                        CASE WHEN BookingNo LIKE 'RF-%-%' 
                             THEN TRY_CONVERT(BIGINT, SUBSTRING(BookingNo, 4, CHARINDEX('-', BookingNo, 4) - 4))
                             ELSE 0 END DESC,

                        -- 流水號：RF-0000009-4  取出 4
                        CASE WHEN BookingNo LIKE 'RF-%-%'
                             THEN TRY_CONVERT(INT, SUBSTRING(BookingNo, CHARINDEX('-', BookingNo, 4) + 1, 20))
                             ELSE 0 END ASC,

                        -- 同號同流水都一樣時，才用建立時間
                        CreatedDate DESC;";

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

        public long CreateBookingBatch()
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"
                INSERT INTO dbo.BookingBatch DEFAULT VALUES;
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            return connection.ExecuteScalar<long>(sql);
        }
    }
}
