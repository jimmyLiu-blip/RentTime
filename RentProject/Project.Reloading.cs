using DevExpress.XtraEditors;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // 更新後重新讀 DB 並套用 UI 狀態
        private void ReloadRentTimeFromDb()
        {
            if (_editRentTimeId == null) return;

            var data = _rentTimeService.GetRentTimeById(_editRentTimeId.Value);

            _loadedRentTime = data;

            FillUIFromModel(data);
            _uiStatus = (UiRentStatus)data.Status;

            ApplyUiStatus();
            ApplyTabByStatus();
        }
    }
}
