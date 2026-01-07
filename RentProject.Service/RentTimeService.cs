using RentProject.Domain;
using RentProject.Repository;

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
        public CreateRentTimeResult CreateRentTime(RentTime model)
        {
            ValidateRequired(model);
            CalculateEstimated(model);

            return _repo.CreateRentTime(model);
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

            // Update 前也要做：必填驗證 + 預估重算
            ValidateRequired(model);
            CalculateEstimated(model);

            model.ModifiedBy = model.CreatedBy;
            model.ModifiedDate = DateTime.Now;

            var rows = _repo.UpdateRentTime(model);
            if (rows != 1) throw new Exception($"更新失敗，受影響筆數={rows}");
        }

        // 刪除租時單
        public void DeletedRentTime(int rentTimeId, string createdBy, DateTime modifiedDate)
        {
            if (rentTimeId <= 0) throw new Exception("RentTimeId 不正確");

            var rows = _repo.DeletedRentTime(rentTimeId, createdBy,DateTime.Now);

            if (rows != 1) throw new Exception($"刪除失敗，受影響筆數={rows}");
        }

        private static void ValidateRequired(RentTime model)
        {
            //if (string.IsNullOrWhiteSpace(model.ProjectNo)) throw new Exception("ProjectNo 必填");
            if (string.IsNullOrWhiteSpace(model.Location)) throw new Exception("場地必填");
            if (string.IsNullOrWhiteSpace(model.CustomerName)) throw new Exception("客戶名稱必填");

            //if (string.IsNullOrWhiteSpace(model.ProjectName)) throw new Exception("ProjectName 必填");
            //if (string.IsNullOrWhiteSpace(model.PE)) throw new Exception("PE 必填");
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
    }
}
