using RentProject.Domain;
using RentProject.Repository;
using System.Reflection;

namespace RentProject.Service
{
    public class RentTimeService
    {
        private readonly DapperRentTimeRepository _repo;
        private static readonly int[] AllowedDinnerMinutes = { 30, 60, 90, 120, 150, 180, 210, 240 };

        public RentTimeService(DapperRentTimeRepository repo)
        { 
            _repo = repo;
        }

        // 新增租時單
        // long? bookingBatchId = null，代表：這個參數是「可為空的 long」，而且預設值是 null，所以呼叫時可以不傳。
        public CreateRentTimeResult CreateRentTime(RentTime model, long? bookingBatchId = null)
        {
            ValidateRequired(model);
            CalculateEstimated(model);

            return _repo.CreateRentTime(model, bookingBatchId);
        }

        // 複製單據
        public CreateRentTimeResult CopyRentTime(int sourceRentTimeId, bool isHandOver, string createdBy)
        {
            return _repo.CopyRentTime(sourceRentTimeId, isHandOver, createdBy, DateTime.Now);
        }


        // 取得案件清單
        public List<RentTime> GetProjectViewList()
        {
            return _repo.GetActiveRentTimesForProjectView();
        }

        // 透過編號取得租時單
        public RentTime GetRentTimeById(int rentTimeId)
        {
            var data = _repo.GetRentTimeById(rentTimeId);

            if (data == null)
            {
                throw new Exception($"找不到 RentTimeId={rentTimeId}");
            }

            return data;
        }

        // 透過編號更新租時單
        public void UpdateRentTimeById(RentTime model)
        {
            if (model.RentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            // 先讀 DB，知道目前狀態（Draft/Started/Finished）
            var db = _repo.GetRentTimeById(model.RentTimeId);
            if (db == null) throw new Exception("找不到 RentTime");

            // 依 DB 狀態決定檢查哪一組時間
            if (db.Status == 0)
            {
                var plannedStart = Combine(model.StartDate, model.StartTime);
                var plannedEnd = Combine(model.EndDate, model.EndTime);
                EnsureEndNotBeforeStart(plannedStart, plannedEnd, "預排時間");
            }
            else // Started/Finished：檢查實際
            {
                EnsureEndNotBeforeStart(model.ActualStartAt, model.ActualEndAt, "實際時間");
            }

            // Update 前也要做：必填驗證 + 預估重算
            ValidateRequired(model);
            CalculateEstimated(model);

            model.ModifiedBy = model.CreatedBy;
            model.ModifiedDate = DateTime.Now;

            var rows = _repo.UpdateRentTime(model);
            if (rows != 1) throw new Exception($"更新失敗，受影響筆數={rows}");
        }

        public void StartRentTimeById(int rentTimeId, string modifiedBy)
        {
            if (rentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            var rows = _repo.StartRentTime(rentTimeId, modifiedBy, DateTime.Now);
            if (rows != 1) throw new Exception($"租時開始失敗（可能不是 Draft 狀態），受影響筆數={rows}");
        }

        public void FinishRentTimeById(int rentTimeId, string modifiedBy)
        {
            if (rentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            var rows = _repo.FinishRentTime(rentTimeId, modifiedBy, DateTime.Now);
            if (rows != 1) throw new Exception($"租時完成失敗（可能不是 Started 狀態），受影響筆數={rows}");
        }

        public void RestoreToDraftById(int rentTimeId, string modifiedBy)
        {
            if (rentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            var rows = _repo.RestoreToDraft(rentTimeId, modifiedBy, DateTime.Now);
            if (rows != 1) throw new Exception($"回復狀態失敗（可能已是 Finished 或找不到資料），受影響筆數={rows}");
        }


        // 刪除租時單
        public void DeletedRentTime(int rentTimeId, string createdBy, DateTime modifiedDate)
        {
            if (rentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            var rows = _repo.DeletedRentTime(rentTimeId, createdBy,DateTime.Now);

            if (rows != 1) throw new Exception($"刪除失敗，受影響筆數={rows}");
        }

        public long CreateBookingBatch()
        { 
            return _repo.CreateBookingBatch();
        }

        // 小工具
        private static void ValidateRequired(RentTime model)
        {
            if (string.IsNullOrWhiteSpace(model.Location)) throw new Exception("場地必填");
            if (string.IsNullOrWhiteSpace(model.CustomerName)) throw new Exception("客戶名稱必填");

            if (string.IsNullOrWhiteSpace(model.Area)) throw new Exception("區域必填");
            if (string.IsNullOrWhiteSpace(model.Sales)) throw new Exception("Sales 必填");

            if (model.StartDate is null || model.StartTime is null || model.EndDate is null || model.EndTime is null)
            {
                throw new Exception("開始/結束日期時間必填");
            }

            if (model.HasLunch && model.LunchMinutes <= 0) throw new Exception("已勾午餐但 LunchMinutes 不正確");
            if (!model.HasLunch) model.LunchMinutes = 0;

            if (model.HasDinner)
            {
                if (model.DinnerMinutes <= 0) throw new Exception("已勾晚餐但 DinnerMinutes 未選");
                if (!AllowedDinnerMinutes.Contains(model.DinnerMinutes)) throw new Exception("DinnerMinutes 不在允許範圍");
            }
            else
            { 
                model.DinnerMinutes = 0;
            }
        }

        private static void CalculateEstimated(RentTime model)
        {
            var start = model.StartDate.Value.Date + model.StartTime.Value;
            var end = model.EndDate.Value.Date + model.EndTime.Value;

            if (end < start) throw new Exception("結束時間不可早於開始時間");

            var minutes = (int)(end - start).TotalMinutes; // 轉換成總分鐘

            if (model.HasLunch) minutes -= model.LunchMinutes;
            if (model.HasDinner) minutes -= model.DinnerMinutes;

            if (minutes < 0) throw new Exception("扣除午餐/晚餐後，預估時間變成負數，請檢查時間與晚餐分配");

            model.EstimatedMinutes = minutes;
            model.EstimatedHours = Math.Round(minutes/60m, 2);
        }

        private static DateTime? Combine(DateTime? date, TimeSpan? time)
        {
            if ( date is null || time is null)
            {
                return null;
            }
            return date.Value.Date + time.Value;
        }

        private static void EnsureEndNotBeforeStart(DateTime? start, DateTime? end, string label)
        {
            if (start is null || end is null) return; 
            if (end.Value < start.Value)
                throw new Exception($"{label}：結束時間不可早於開始時間");
        }
    }
}
