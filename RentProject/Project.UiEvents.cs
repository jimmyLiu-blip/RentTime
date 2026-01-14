using DevExpress.XtraEditors;
using RentProject.Domain;
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
        private void cmbJobNo_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            
            var jobNo = cmbJobNo.Text?.Trim() ?? "";
            _currentJobNo = string.IsNullOrWhiteSpace(jobNo) ? null : jobNo;

            // 規則1：使用者正在輸入中，不查 DB、不清欄位
            // 先回手動模式，避免規則打架
            SetAutoFillMode(false);
        }

        // 全部都不是空白才回傳 true，只要有任何一個是空白就回傳 false
        // string.IsNullOrWhiteSpace(x) 回傳 true：代表 x 是空的/沒內容
        private bool IsJobNoMasterComplete(JobNoMaster m)
        {
            //  這裡的「必填」就是 JobNo 查詢要用來判斷「算查到嗎」的欄位
            return
                !string.IsNullOrWhiteSpace(m.CustomerName) &&
                !string.IsNullOrWhiteSpace(m.Sales) &&
                !string.IsNullOrWhiteSpace(m.ProjectNo) &&
                !string.IsNullOrWhiteSpace(m.ProjectName) &&
                !string.IsNullOrWhiteSpace(m.PE) &&
                !string.IsNullOrWhiteSpace(m.SampleModel) &&
                !string.IsNullOrWhiteSpace(m.SampleNo);
        }

        private void ApplyJobNoMasterToUI(JobNoMaster? m)
        {
            _isLoading = true;
            try
            {
                txtProjectNo.Text = m?.ProjectNo ?? "";
                txtProjectName.Text = m?.ProjectName ?? "";
                txtPE.Text = m?.PE ?? "";

                cmbCompany.Text = m?.CustomerName ?? "";
                txtSales.Text = m?.Sales ?? "";

                txtSampleModel.Text = m?.SampleModel ?? "";
                txtSampleNo.Text = m?.SampleNo ?? "";
            }
            finally { _isLoading = false; }
        }

        // 事件處理器（Event Handler） 只能長這樣：void XXX(object sender, EventArgs e)
        // 所以如果要在事件裡用 await，只能寫成 async void（因為簽名固定）
        // JobNo 離開欄位時才存入
        private async void cmbJobNo_Validated(object sender, EventArgs e)
        {
            if (_isLoading) return;

            var jobNo = cmbJobNo.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(jobNo)) return;

            // 先判斷「這次輸入」是不是本來就存在下拉裡
            bool existedInList = cmbJobNo.Properties.Items.Contains(jobNo);

            if (!existedInList)
            {
                // 新 JobNo:只存 JobNo，其他欄位讓使用者手填
                _jobNoService.GetOrCreateJobId(jobNo);

                // 讓下拉立刻也可以看到 (不用重開表單)
                if (!cmbJobNo.Properties.Items.Contains(jobNo))
                { 
                    cmbJobNo.Properties.Items.Add(jobNo);
                }

                SetAutoFillMode(false);
                return;
            }

            // 舊 JobNo:才做 DB Lockup (完整才 AutoFill；不完整不覆蓋 UI）
            await LookupJobNoFromDbAsync(jobNo);
        }

        // 等待 DB 查詢流程做完，再回到事件後續流程。
        private async Task LookupJobNoFromDbAsync(string jobNo)
        {
            // 產生這次查詢的流水號（用來丟棄舊回應）
            int seq = ++_jobLockupSeq;

            _isJobLockupLoading = true;
            SetLoading(true);

            try
            {
                // 讓 UI 有機會先刷新到 Loading 狀態
                await Task.Yield();

                // 使用者又選了別的 JobNo，就丟掉
                if (seq != _jobLockupSeq) return;

                // 1. 查本機 DB
                var m = _jobNoService.GetJobNoMasterByJobNo(jobNo);

                // 使用者又切 JobNo，就丟掉
                if (seq != _jobLockupSeq) return;

                // 2. DB 查不到：回手動模式
                if (m == null)
                {
                    ApplyJobNoMasterToUI(null);
                    SetAutoFillMode(false);
                    return;
                }

                // 3) DB 查到：先「部分回填 + 缺的清空」
                ApplyJobNoMasterToUI(m);

                // 4) 完整才鎖；不完整維持手動
                SetAutoFillMode(IsJobNoMasterComplete(m));
            }
            finally //不管 try 中途 return、或發生例外，finally 一定會跑
            {
                if (seq == _jobLockupSeq)
                { 
                    _isJobLockupLoading = false;
                    SetLoading(false);
                }
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
