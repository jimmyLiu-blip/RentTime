using DevExpress.XtraEditors;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // UI 事件：欄位連動 / 手動修改偵測
        // =========================================================

        // 偵測使用者手動修改聯絡資訊
        private void ContactFields_EditValueChanged(object sender, EventArgs e)
        {
            // 程式塞值不算「手動改」
            if (_isLoading) return;
            if (_jobNoAutoMode) return;
            _contactManuallyEdited = true;
        }

        // 午/晚餐
        private void chkHasLunch_CheckedChanged(object sender, EventArgs e)
        {
            ApplyLunchUI();
            UpdateEstimatedUI();
        }

        private void chkHasDinner_CheckedChanged(object sender, EventArgs e)
        {
            ApplyDinnerUI();
            UpdateEstimatedUI();
        }

        // 日期/時間/晚餐分鐘 -> 集中刷新
        private void startDateEdit_EditValueChanged(object sender, EventArgs e) => RefreshMealAndEstimateUI();
        private void endDateEdit_EditValueChanged(object sender, EventArgs e) => RefreshMealAndEstimateUI();
        private void startTimeEdit_EditValueChanged(object sender, EventArgs e) => RefreshMealAndEstimateUI();
        private void endTimeEdit_EditValueChanged(object sender, EventArgs e) => RefreshMealAndEstimateUI();
        private void cmbDinnerMinutes_EditValueChanged(object sender, EventArgs e) => RefreshMealAndEstimateUI();

        // Location -> Area
        private void cmbLocation_EditValueChanged(object sender, EventArgs e)
        {
            var location = cmbLocation.Text?.Trim() ?? "";
            var item = _locations.FirstOrDefault(x => x.Location == location);
            txtArea.Text = item?.Area ?? "";
        }

        // JobNo 改變 -> 啟動「查詢流程骨架」（會在這裡補：先查DB、再打API）
        private async void cmbJobNo_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            
            var jobNo = cmbJobNo.Text?.Trim() ?? "";
            _currentJobNo = string.IsNullOrWhiteSpace(jobNo) ? null : jobNo;

            // JobNo清空：回到手動模式
            if (string.IsNullOrWhiteSpace(jobNo))
            { 
                SetAutoFillMode(false);
                return;
            }

            // 產生這次查詢的流水號（用來丟棄舊回應）
            int seq = ++_jobLockupSeq;

            _isJobLockupLoading = true;
            SetLoading(true);

            SetAutoFillMode(true);

            try
            {
                // 先讓出一次控制權給 UI 執行緒，讓畫面有機會先「刷新」到 Loading 狀態，再繼續往下做查詢。
                await System.Threading.Tasks.Task.Yield();

                // 如果使用者又選了別的 JobNo，這次就丟掉
                if (seq != _jobLockupSeq) return;

                // 這一步先不做填值（下一步才開始：先查DB、再打API）
            }
            finally
            {
                // 只有「最新那一次」才可以解鎖
                if (seq == _jobLockupSeq)
                { 
                    _isJobLockupLoading = false;
                    SetLoading(false);

                    // Step 5-3 暫時先解鎖回手動模式
                    // Step 5-4/5-5 會改成：依「有沒有查到資料」決定要不要鎖
                    SetAutoFillMode(false);
                }
            }
        }

        // JobNo 離開欄位時才存入
        private void cmbJobNo_Validated(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var jobNo = cmbJobNo.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(jobNo)) return;

            try
            {
                // 確保 DB 裡有這筆 JobNo（沒有就插入）
                _jobNoService.GetOrCreateJobId(jobNo);

                // 讓下拉立刻也看得到（不用重開表單）
                if (!cmbJobNo.Properties.Items.Contains(jobNo))
                    cmbJobNo.Properties.Items.Add(jobNo);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "JobNo Save Error");
            }
        }

        // TestMode -> TestItem
        private void cmbTestMode_EditValueChanged(object sender, EventArgs e)
        {
            var mode = cmbTestMode.Text?.Trim() ?? "";
            UpdateTestItem(mode);
        }

        // Company -> Sales / ContactName / ContactPhone（手動改）
        private void cmbCompany_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            if (_jobNoAutoMode) return;

            var company = cmbCompany.Text?.Trim() ?? "";

            // 1) 是否換公司？
            bool companychanged = !string.Equals(company, _lastCompany, StringComparison.Ordinal);

            // 2) 同公司 + 已手動改 => 不要覆蓋
            if (!companychanged && _contactManuallyEdited)
                return;

            // 3) 換公司 => 解除鎖定（允許自動帶入）
            if (companychanged)
                _contactManuallyEdited = false;

            var c = _contactCompany.FirstOrDefault(x => x.Company == company);

            // 4) 自動帶入（用 _isLoading 壓住手動改事件）
            _isLoading = true;
            try
            {
                txtContactName.Text = c?.ContactName ?? "";
                txtContactPhone.Text = c?.ContactPhone ?? "";
                txtSales.Text = c?.Sales ?? "";
            }
            finally
            {
                _isLoading = false;
            }

            _lastCompany = company;
        }
    }
}
