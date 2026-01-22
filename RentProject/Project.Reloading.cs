using DevExpress.XtraEditors;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // 只從 Api 讀回來刷新 UI
        private async Task ReloadRentTimeFromApiAsync()
        {
            if (_editRentTimeId == null) return;

            SetLoading(true);
            try
            {
                var data = await _rentTimeApiClient.GetByIdAsync(_editRentTimeId.Value);
                if (data == null)
                {
                    XtraMessageBox.Show("找不到此 RentTime（可能已被刪除）", "提示");
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                _loadedRentTime = data;
                FillUIFromModel(data);
                // 讓編輯模式一打開就把旗標同步成正確狀態（不用 JobNo + Tab）
                SyncJobNoApiFlagsFromLoadedUI();

                _uiStatus = (UiRentStatus)data.Status;
                ApplyUiStatus();
                ApplyTabByStatus();
            }
            finally
            {
                SetLoading(false);
            }
        }

    }
}
