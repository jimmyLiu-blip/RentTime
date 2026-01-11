using DevExpress.XtraEditors;
using System;
using System.Linq;
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

        //  JobNo-> ProjectNo / ProjectName / PE
        private void cmbJobNo_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var jobNo = cmbJobNo.Text?.Trim() ?? "";
            var j = _projects.FirstOrDefault(x =>
                string.Equals(x.JobNo, jobNo, StringComparison.Ordinal));

            _isLoading = true;
            try
            {
                txtProjectName.Text = j?.ProjectName ?? "";
                txtProjectNo.Text = j?.ProjectNo ?? "";
                txtPE.Text = j?.PE ?? "";
            }
            finally
            {
                _isLoading = false;
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
