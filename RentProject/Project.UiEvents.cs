using DevExpress.XtraEditors;
using RentProject.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // UI 事件：欄位連動 / 手動修改偵測
        // =========================================================

        // 午/晚餐
        private void chkHasLunch_CheckedChanged(object sender, EventArgs e)
        {
            SafeRun(() =>
            {
                ApplyLunchUI();
                UpdateEstimatedUI();
            }, caption: "更新午餐失敗");
        }

        private void chkHasDinner_CheckedChanged(object sender, EventArgs e)
        {
            SafeRun(() =>
            {
                ApplyDinnerUI();
                UpdateEstimatedUI();
            }, caption: "更新晚餐失敗");
        }

        // 日期/時間/晚餐分鐘 -> 集中刷新
        private void startDateEdit_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() => RefreshMealAndEstimateUI(), caption: "刷新預估時間失敗");
        } 
        
        private void endDateEdit_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() => RefreshMealAndEstimateUI(), caption: "刷新預估時間失敗");
        }

        private void startTimeEdit_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() => RefreshMealAndEstimateUI(), caption: "刷新預估時間失敗");
        }

        private void endTimeEdit_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() => RefreshMealAndEstimateUI(), caption: "刷新預估時間失敗");
        }

        private void cmbDinnerMinutes_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() => RefreshMealAndEstimateUI(), caption: "刷新預估時間失敗");
        }

        // Location -> Area
        private void cmbLocation_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SafeRun(() =>
            {
                var location = cmbLocation.Text?.Trim() ?? "";
                var item = _locations.FirstOrDefault(x => x.Location == location);
                txtArea.Text = item?.Area ?? "";
            }, caption: "更新場地/區域失敗");
        }

        // JobNo 改變 -> 啟動「查詢流程骨架」（會在這裡補：先查DB、再打API）
        private void cmbJobNo_EditValueChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            SafeRun(() =>
            {
                // 只要 JobNo 有變動，就取消上一個查詢，避免舊回來亂改 UI
                _jobNoCts?.Cancel();

                var jobNo = cmbJobNo.Text?.Trim() ?? "";
                _currentJobNo = string.IsNullOrWhiteSpace(jobNo) ? null : jobNo;

                // 規則1：使用者正在輸入中，不查 DB、不清欄位
                // 先回手動模式，避免規則打架
                SetAutoFillMode(false);

                // 只要 JobNo 被改，就先視為「API 尚未確認 / 或尚未查到」
                _jobNoApiHasCustomer = false;
                _jobNoApiHasSales = false;
            }, caption: "JobNo 變更處理失敗");
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
            var prev = _isLoading;     // 記住原本狀態
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
            finally { _isLoading = prev; } // 還原，不要硬改成 false
        }

        // 事件處理器（Event Handler） 只能長這樣：void XXX(object sender, EventArgs e)
        // 所以如果要在事件裡用 await，只能寫成 async void（因為簽名固定）
        // JobNo 離開欄位時才存入
        private async void cmbJobNo_Validated(object sender, EventArgs e)
        {
            if (_isLoading) return;

            await SafeRunAsync(async () =>
            {
                var jobNo = cmbJobNo.Text?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(jobNo)) return;

                // 先判斷「這次輸入」是不是本來就存在下拉裡
                bool existedInList = cmbJobNo.Properties.Items.Contains(jobNo);

                // 新的：加進下拉（但不要 return）
                if (!existedInList && !cmbJobNo.Properties.Items.Contains(jobNo))
                {
                   AddJobNoToRecentList(jobNo, max:8);
                }

                // 不管新舊：都打 API
                await LookupJobNoFromAPIAsync(jobNo);
            }, caption: "JobNo 查詢失敗");
        }

        // 最核心：取消、丟舊、呼叫 API、回填 UI、鎖欄位。
        private async Task LookupJobNoFromAPIAsync(string jobNo)
        {
            // 取消並放上一個 CTS
            _jobNoCts?.Cancel();
            _jobNoCts?.Dispose();

            // 這次查詢新的 CTS
            _jobNoCts = new CancellationTokenSource();
            var ct = _jobNoCts.Token;

            // 產生這次查詢的流水號（用來丟棄舊回應）
            int seq = ++_jobLockupSeq;

            _isJobLockupLoading = true;
            SetLoading(true);

            try
            {
                // 讓 UI 有機會先刷新到 Loading 狀態
                await Task.Yield();

                // 先確保 DB 有 jobId（可取消）
                _currentJobNo = jobNo;
                _currentJobId = await _jobNoApiClient.GetOrCreateJobIdAsync(jobNo, ct);

                // 若取消，直接停止
                ct.ThrowIfCancellationRequested();

                // 使用者又選了別的 JobNo，就丟掉
                if (seq != _jobLockupSeq) return;

                // 把 ct 往下傳，API/Delay/HTTP 才能真的被取消
                var m = await _jobNoApiClient.GetJobNoMasterFromApiAndSaveAsync(jobNo, ct);

                _jobNoApiHasCustomer = !string.IsNullOrWhiteSpace(m?.CustomerName);
                _jobNoApiHasSales = !string.IsNullOrWhiteSpace(m?.Sales);

                ct.ThrowIfCancellationRequested();

                // 使用者又切 JobNo，就丟掉
                if (seq != _jobLockupSeq) return;

                // 2. 查不到：回手動模式
                if (m == null)
                {
                    _jobNoApiHasCustomer = false;
                    _jobNoApiHasSales = false;

                    ApplyJobNoMasterToUI(null);
                    SetAutoFillMode(false);
                    return;
                }

                // 3. 查到：先「部分回填 + 缺的清空」
                ApplyJobNoMasterToUI(m);

                // 4. 完整才進 AutoMode；不完整維持手動模式
                bool complete = IsJobNoMasterComplete(m);
                SetAutoFillMode(complete);

                // 不管完整不完整，只要有回填到的欄位就鎖住
                ApplyJobNoFilledLocks(true);
            }
            catch (OperationCanceledException)
            {
                // 使用者改了 JobNo，這次查詢被取消是正常狀況，直接結束
            }
            catch (Exception ex)
            { 
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}","JobNo 查詢失敗",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

                ApplyJobNoMasterToUI(null);
                SetAutoFillMode(false);
                _jobNoApiHasCustomer = false;
                _jobNoApiHasSales = false;
            }
            finally //不管 try 中途 return、或發生例外，finally 一定會跑
            {
                // 只允許「最新那次」把 loading 收掉；舊的查詢不要亂關
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
            if (_isLoading) return;
            SafeRun(() =>
            {
                var mode = cmbTestMode.Text?.Trim() ?? "";
                UpdateTestItem(mode);
            }, caption: "更新測試項目失敗");
        }

        // 限制jobNo下拉呈現數量
        private void AddJobNoToRecentList(string jobNo, int max = 8)
        { 
            jobNo = (jobNo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(jobNo)) return;

            var items = cmbJobNo.Properties.Items;

            // 先移除舊的同值（避免重複）
            int idx = items.IndexOf(jobNo);
            if (idx >= 0) items.RemoveAt(idx);

            // 插到最前面
            items.Insert(0, jobNo);

            // 超過上限就砍
            while (items.Count > max)
                items.RemoveAt(items.Count-1);
        }
    }
}
