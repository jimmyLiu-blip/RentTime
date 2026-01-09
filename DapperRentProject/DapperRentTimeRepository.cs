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
                    string bookingNo = $"TMP-{batchId:D7}-{seq}";

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

        // 複製單據規則需要再複習
        public CreateRentTimeResult CopyRentTime(int sourceRentTimeId, bool isHandOver, string createdBy, DateTime now)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var tx = connection.BeginTransaction();

            try
            {
                // 1) 讀來源單據（建議只允許複製未刪除的）
                var srcSql = @"
                SELECT TOP 1
                    RentTimeId, BookingNo, JobId, CreatedBy, Area, CustomerName, Sales, ProjectNo, ProjectName, PE, Location,
                    ContactName, Phone, TestInformation, EngineerName, SampleModel, SampleNo,
                    TestMode, TestItem, Notes,
                    StartDate, EndDate, StartTime, EndTime,
                    EstimatedMinutes, EstimatedHours,
                    HasLunch, LunchMinutes, HasDinner, DinnerMinutes,
                    Status, ActualStartAt, ActualEndAt, IsHandOver, IsDeleted
                FROM dbo.RentTimes
                WHERE RentTimeId = @Id AND IsDeleted = 0;";

                var src = connection.QueryFirstOrDefault<RentTime>(srcSql, new { Id = sourceRentTimeId }, tx);
                if (src == null) throw new Exception("找不到來源租時單");

                // 2) 來源的「結束時間」：優先 ActualEndAt，沒有就用預排 EndDate+EndTime
                DateTime? plannedStart = Combine(src.StartDate, src.StartTime);
                DateTime? plannedEnd = Combine(src.EndDate, src.EndTime);

                DateTime? srcStart = src.ActualStartAt ?? plannedStart;
                DateTime? srcEnd = src.ActualEndAt ?? plannedEnd;

                // 3) 計算一個「持續時間」(避免複製後 Start > End)
                //    如果抓不到或不合理，就先給 60 分鐘當預設
                var duration = TimeSpan.FromMinutes(60);
                if (srcStart.HasValue && srcEnd.HasValue && srcEnd.Value > srcStart.Value)
                    duration = srcEnd.Value - srcStart.Value;

                // 4) 新單開始時間規則
                var newStart = isHandOver ? (srcEnd ?? now) : now;
                var newEnd = newStart + duration;

                // 5) BookingNo 規則（方案B：先給 TMP，完成再轉 RF）
                string batchText;
                int seq;

                if (isHandOver)
                {
                    // 同 batch + 取下一個 seq
                    batchText = ExtractBatch(src.BookingNo);
                    seq = GetNextSeqForBatch(connection, tx, batchText);
                }
                else
                {
                    // 新 batch，seq=1
                    var batchIdSql = @"
                    INSERT INTO dbo.BookingBatch DEFAULT VALUES;
                    SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
                    long newBatchId = connection.ExecuteScalar<long>(batchIdSql, transaction: tx);

                    batchText = $"{newBatchId:D7}";
                    seq = 1;
                }

                var newBookingNo = $"TMP-{batchText}-{seq}";

                // 6) Insert 新 RentTime
                //    注意：TestMode/TestItem/Notes 不複製（清空）
                //    交接打勾：EngineerName 清空
                var insertSql = @"
                INSERT INTO dbo.RentTimes
                (
                    BookingNo, CreatedBy, Area, ProjectNo, ProjectName, PE, Location,
                    CustomerName, Sales, ContactName, Phone, TestInformation,
                    EngineerName, SampleModel, SampleNo,
                    TestMode, TestItem, Notes,
                    StartDate, EndDate, StartTime, EndTime,
                    EstimatedMinutes, EstimatedHours,
                    HasLunch, LunchMinutes, HasDinner, DinnerMinutes,
                    IsDeleted, JobId, Status,
                    ActualStartAt, ActualEndAt,
                    IsHandOver
                )
                OUTPUT INSERTED.RentTimeId
                VALUES
                (
                    @BookingNo, @CreatedBy, @Area, @ProjectNo, @ProjectName, @PE, @Location,
                    @CustomerName, @Sales, @ContactName, @Phone, @TestInformation,
                    @EngineerName, @SampleModel, @SampleNo,
                    NULL, NULL, NULL,
                    @StartDate, @EndDate, @StartTime, @EndTime,
                    @EstimatedMinutes, @EstimatedHours,
                    @HasLunch, @LunchMinutes, @HasDinner, @DinnerMinutes,
                    0, @JobId, 0,
                    NULL, NULL,
                    0
                );";

                var newEngineer = isHandOver ? "" : (src.EngineerName ?? "");

                int newId = connection.ExecuteScalar<int>(insertSql, new
                {
                    BookingNo = newBookingNo,
                    CreatedBy = createdBy,

                    src.Area,
                    src.ProjectNo,
                    src.ProjectName,
                    src.PE,
                    src.Location,

                    src.CustomerName,
                    src.Sales,
                    src.ContactName,
                    Phone = src.Phone,
                    src.TestInformation,

                    EngineerName = newEngineer,
                    src.SampleModel,
                    src.SampleNo,

                    StartDate = newStart.Date,
                    EndDate = newEnd.Date,
                    StartTime = newStart.TimeOfDay,
                    EndTime = newEnd.TimeOfDay,

                    src.EstimatedMinutes,
                    src.EstimatedHours,

                    src.HasLunch,
                    src.LunchMinutes,
                    src.HasDinner,
                    src.DinnerMinutes,

                    src.JobId,
                }, tx);

                tx.Commit();

                return new CreateRentTimeResult
                {
                    RentTimeId = newId,
                    BookingNo = newBookingNo
                };
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // 取得未刪除的所有案件 (ORDER BY要再練習)
        public List<RentTime> GetActiveRentTimesForProjectView()
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var sql = @"
                SELECT
                    RentTimeId, BookingNo, Area, Location, CustomerName, ContactName,
                    Phone, PE, Sales, StartDate, EndDate, StartTime, EndTime,
                    ProjectNo, ProjectName
                FROM dbo.RentTimes
                WHERE IsDeleted = 0
                ORDER BY
                    -- 主號：RF-0000009-4 / TMP-0000009-4  -> 取出 0000009
                    CASE WHEN BookingNo LIKE '%-%-%'
                         THEN TRY_CONVERT(BIGINT,
                              SUBSTRING(
                                  BookingNo,
                                  CHARINDEX('-', BookingNo) + 1,
                                  CHARINDEX('-', BookingNo, CHARINDEX('-', BookingNo) + 1) - CHARINDEX('-', BookingNo) - 1
                              )
                         )
                         ELSE 0 END DESC,

                    -- 流水號：RF-0000009-4 / TMP-0000009-4  -> 取出 4
                    CASE WHEN BookingNo LIKE '%-%-%'
                         THEN TRY_CONVERT(INT,
                              SUBSTRING(
                                  BookingNo,
                                  CHARINDEX('-', BookingNo, CHARINDEX('-', BookingNo) + 1) + 1,
                                  20
                              )
                         )
                         ELSE 0 END ASC,

                    CreatedDate DESC;
                ";


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
                        r.SampleNo, r.TestMode, r.TestItem, r.Notes, r.JobId, jm.JobNo, 
                        r.Status, r.ActualStartAt, r.ActualEndAt, r.IsHandOver, r.IsDeleted
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
                        ModifiedDate = @ModifiedDate,
                        IsHandOver = @IsHandOver,
                        ActualStartAt = @ActualStartAt,
                        ActualEndAt = @ActualEndAt
                        WHERE RentTimeId = @RentTimeId
                        AND Status <> 2;";  // Finished 不允許再被 Update

            return connection.Execute(sql, model);
        }

        // 租時開始：Draft(0) -> Started(1)
        // 只有在還沒 Finished(2) 時才允許更新
        public int StartRentTime(int rentTimeId, string modifiedBy, DateTime now)
        { 
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE dbo.RentTimes
                SET
                    Status = 1,    
                    EngineerName = COALESCE(NULLIF(LTRIM(RTRIM(EngineerName)), ''), @ModifiedBy),
                    ActualStartAt = COALESCE(ActualStartAt, @Now), --避免重複時覆蓋
                    ModifiedBy = @ModifiedBy,
                    ModifiedDate = @Now
                WHERE RentTimeId = @RentTimeId
                AND Status = 0;"; // 只允許 Draft -> Started

            return connection.Execute(sql, new
            {
                RentTimeId = rentTimeId,
                ModifiedBy = modifiedBy,
                Now = now
            });
        }

        // 租時完成：Started(1) -> Finished(2)
        // 只允許 Started 才能完成
        public int FinishRentTime(int rentTimeId, string modifiedBy, DateTime now)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE dbo.RentTimes
                SET
                    BookingNo = CASE
                        WHEN BookingNo LIKE 'RF-%' THEN BookingNo
                        WHEN BookingNo LIKE 'TMP-%' THEN STUFF(BookingNo, 1, 4, 'RF-')   -- TMP-xxxx => RF-xxxx
                        WHEN BookingNo LIKE 'TEMP-%' THEN STUFF(BookingNo, 1, 5, 'RF-')  -- TEMP-xxxx => RF-xxxx
                        ELSE BookingNo
                    END,
                    Status = 2,
                    ModifiedBy = @ModifiedBy,
                    ModifiedDate = @Now
                WHERE RentTimeId = @RentTimeId
                AND Status = 1;"; // 只允許 Started -> Finished

            return connection.Execute(sql, new
            {
                RentTimeId = rentTimeId,
                ModifiedBy = modifiedBy,
                Now = now
            });
        }

        // 回復狀態：Draft/Started(0/1) -> Draft(0)
        // Finished 不能回復，只處理 0/1
        public int RestoreToDraft(int rentTimeId, string modifiedBy, DateTime now)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE dbo.RentTimes
                SET 
                    Status = 0,
                    ActualStartAt = NULL,
                    ActualEndAt = NULL,
                    ModifiedBy = @ModifiedBy,
                    ModifiedDate = @Now
                WHERE RentTimeId = @RentTimeId
                AND Status In (0,1);";

            return connection.Execute(sql, new
            {
                RentTimeId = rentTimeId,
                ModifiedBy = modifiedBy,
                Now = now
            });
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


        // ====== 小工具 ======
        // 組合預排的 Date+Time
        private static DateTime? Combine(DateTime? date, TimeSpan? time)
        {
            if (date is null || time is null) return null;
            return date.Value.Date + time.Value;
        }

        private static string ExtractBatch(string? bookingNo)
        {
            if (string.IsNullOrWhiteSpace(bookingNo))
                throw new Exception("BookingNo 為空，無法複製流水號");

            var parts = bookingNo.Split('-');

            // 期望：TMP-0000123-1 或 RF-0000123-1
            if (parts.Length >= 3) return parts[1];

            // 退而求其次：RF-0000123（沒有 seq）
            if (parts.Length == 2) return parts[1];

            throw new Exception($"BookingNo 格式不正確：{bookingNo}");
        }

        private static int GetNextSeqForBatch(SqlConnection conn, SqlTransaction tx, string batchText)
        {
            var sql = @"
                SELECT ISNULL(MAX(
                    CASE 
                        WHEN CHARINDEX('-', BookingNo, CHARINDEX('-', BookingNo) + 1) > 0
                        THEN TRY_CONVERT(INT, SUBSTRING(
                            BookingNo,
                            CHARINDEX('-', BookingNo, CHARINDEX('-', BookingNo) + 1) + 1,
                            20
                        ))
                        ELSE NULL
                    END
                ), 0) + 1
                FROM dbo.RentTimes
                WHERE BookingNo LIKE '%-' + @Batch + '-%';";

            return conn.ExecuteScalar<int>(sql, new { Batch = batchText }, tx);
        }
    }
}
