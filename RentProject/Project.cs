using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Mask;
using RentProject.Domain;
using RentProject.UIModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using RentProject.Clients;
using System.Threading.Tasks;
using RentProject.UI;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // A) 服務 / 狀態欄位 (State)
        // =========================================================
        private readonly IRentTimeApiClient _rentTimeApiClient;
        private readonly IJobNoApiClient _jobNoApiClient;

        private List<ProjectItem> _projects = new();

        // 午餐/晚餐規則門檻
        private static readonly TimeSpan LunchEnableAt = new(13, 0, 0);
        private static readonly TimeSpan DinnerEnableAt = new(18, 0, 0);

        // 編輯租時單模式
        private readonly int? _editRentTimeId = null;
        private static string _lastCreatedBy = "Jimmy";

        // 控制「程式在塞值」時，不要被事件誤判為手動修改
        private bool _isLoading = false;

        // JobNo 查詢的流水號 (用來丟棄舊的API回應)
        private int _jobLockupSeq = 0;

        // JobNo 是否正在查詢中 (查詢中要鎖 Save)
        private bool _isJobLockupLoading = false;

        // 舊JobNo停止查詢，避免浪費資源
        private CancellationTokenSource? _jobNoCts;

        // 這次 JobNo 查詢（WebApi）是否有回值：客戶名稱 / Sales
        private bool _jobNoApiHasCustomer = false;
        private bool _jobNoApiHasSales = false;

        // 紀錄目前 UI 選到的 JobNo (方便除錯)
        private string? _currentJobNo = null;

        private int? _currentJobId = null;

        // 紀錄BookingBatchId
        private long? _bookingBatchId;

        private readonly string _currentUser = "Bob"; // 暫時寫死，之後接登入

        // 目前這張單在 UI 上該用哪個狀態顯示
        private enum UiRentStatus { Draft = 0, Started = 1, Finished = 2, SubmittedToAssistant = 3 };
        private UiRentStatus _uiStatus = UiRentStatus.Draft;

        // 快照保存從DB獨到的原始資料
        private RentTime? _loadedRentTime;

        // 通知外面(ProjectView/Form1更新)
        public event Action? RentTimeChanged;

        // Invoke() 就是「把事件（其實是委派 delegate）叫起來執行」
        private void NotifyRentTimeChanged()
        {
            RentTimeChanged?.Invoke();
        }

        // 資料讀取時鎖定新增租時單和JobNo控制
        private void SetLoading(bool loading)
        {
            _isLoading = loading;

            // 最小規則：Loading時不讓存檔，避免API還沒回來就存
            btnCreatedRentTime.Enabled = !loading;

            // 視覺回饋
            this.Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
        }

        // ===== 共用 try/catch 包裝（比照 Form1）=====
        private void SetAutoFillMode(bool enabled)
        {
            // 只要欄位「有值」就鎖；沒有值就開放
            bool HasText(string? s) => !string.IsNullOrWhiteSpace(s);

            // 這些欄位是「JobNo 查到才自動帶入」的，查到就鎖住避免規則打架
            txtProjectNo.Properties.ReadOnly = !enabled;
            txtProjectName.Properties.ReadOnly = !enabled;
            txtPE.Properties.ReadOnly = !enabled;

            cmbCompany.Properties.ReadOnly = enabled && HasText(cmbCompany.Text);
            txtSales.Properties.ReadOnly = enabled && HasText(txtSales.Text);

            txtSampleNo.Properties.ReadOnly = enabled && HasText(txtSampleNo.Text);
            txtSampleModel.Properties.ReadOnly = enabled && HasText(txtSampleModel.Text);
        }

        private void ApplyJobNoFilledLocks(bool lockFilled)
        {
            bool HasText(string? s) => !string.IsNullOrWhiteSpace(s);

            txtProjectNo.Properties.ReadOnly = lockFilled && HasText(txtProjectNo.Text);
            txtProjectName.Properties.ReadOnly = lockFilled && HasText(txtProjectName.Text);
            txtPE.Properties.ReadOnly = lockFilled && HasText(txtPE.Text);

            cmbCompany.Properties.ReadOnly = lockFilled && HasText(cmbCompany.Text);
            txtSales.Properties.ReadOnly = lockFilled && HasText(txtSales.Text);

            txtSampleNo.Properties.ReadOnly = lockFilled && HasText(txtSampleNo.Text);
            txtSampleModel.Properties.ReadOnly = lockFilled && HasText(txtSampleModel.Text);
        }

        // =========================================================
        // B) 建構子
        // =========================================================
        public Project(IRentTimeApiClient rentTimeApiClient, IJobNoApiClient jobNoApiClient)
        {
            InitializeComponent();

            _rentTimeApiClient = rentTimeApiClient;
            _jobNoApiClient = jobNoApiClient;
        }

        public Project(IRentTimeApiClient rentTimeApiClient, IJobNoApiClient jobNoApiClient, int rentTimeId) : this(rentTimeApiClient, jobNoApiClient)
        {
            _editRentTimeId = rentTimeId;
        }

        // =========================================================
        // C) Form Load：初始化 UI
        // =========================================================
        private async void Project_Load(object sender, EventArgs e)
        {
            // ===== 新增：修正 DateEdit 和 TimeEdit 的 Mask 問題 =====
            // 1. 設定 DateEdit（處理日期輸入問題）
            UiSafeRunner.SafeRun(() =>
            {
                ConfigureDateEdit(startDateEdit);
                ConfigureDateEdit(endDateEdit);

                // 2. 設定 TimeEdit - 完整設定（關鍵：先設定格式）
                startTimeEdit.Properties.Mask.MaskType = MaskType.None;
                startTimeEdit.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
                startTimeEdit.Properties.DisplayFormat.FormatString = "HH:mm";
                startTimeEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;  // ← 改成 Custom
                startTimeEdit.Properties.EditFormat.FormatString = "HH:mm";
                startTimeEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Custom;     // ← 改成 Custom
                ConfigureTimeEdit(startTimeEdit);

                endTimeEdit.Properties.Mask.MaskType = MaskType.None;
                endTimeEdit.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
                endTimeEdit.Properties.DisplayFormat.FormatString = "HH:mm";
                endTimeEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;    // ← 改成 Custom
                endTimeEdit.Properties.EditFormat.FormatString = "HH:mm";
                endTimeEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Custom;       // ← 改成 Custom
                ConfigureTimeEdit(endTimeEdit);

                cmbJobNo.EditValueChanged -= cmbJobNo_EditValueChanged;
                cmbJobNo.EditValueChanged += cmbJobNo_EditValueChanged;

                // 手動輸入後，自動保存(Validated 事件)
                cmbJobNo.Validated -= cmbJobNo_Validated;
                cmbJobNo.Validated += cmbJobNo_Validated;

                // 場地改變 -> 自動帶入區域
                cmbLocation.EditValueChanged -= cmbLocation_EditValueChanged;
                cmbLocation.EditValueChanged += cmbLocation_EditValueChanged;

                // 測試模式改變 -> 自動帶出測試項目
                cmbTestMode.EditValueChanged -= cmbTestMode_EditValueChanged;
                cmbTestMode.EditValueChanged += cmbTestMode_EditValueChanged;

                // 綁定租時開始、租時完成、回復狀態
                btnRentTimeStart.Click -= btnRentTimeStart_Click;
                btnRentTimeStart.Click += btnRentTimeStart_Click;

                btnRentTimeEnd.Click -= btnRentTimeEnd_Click;
                btnRentTimeEnd.Click += btnRentTimeEnd_Click;

                btnRestoreRentTime.Click -= btnRestoreRentTime_Click;
                btnRestoreRentTime.Click += btnRestoreRentTime_Click;

                // 綁定：建立 / 刪除 / 複製
                btnCreatedRentTime.Click -= btnCreatedRentTime_Click;
                btnCreatedRentTime.Click += btnCreatedRentTime_Click;

                btnDeletedRentTime.Click -= btnDeletedRentTime_Click;
                btnDeletedRentTime.Click += btnDeletedRentTime_Click;

                btnCopyRentTime.Click -= btnCopyRentTime_Click;
                btnCopyRentTime.Click += btnCopyRentTime_Click;

                // 日期/時間改變就刷新午餐晚餐可用性 + 預估時間
                startDateEdit.EditValueChanged -= AnyTimeOrMealChanged;
                startDateEdit.EditValueChanged += AnyTimeOrMealChanged;

                endDateEdit.EditValueChanged -= AnyTimeOrMealChanged;
                endDateEdit.EditValueChanged += AnyTimeOrMealChanged;

                startTimeEdit.EditValueChanged -= AnyTimeOrMealChanged;
                startTimeEdit.EditValueChanged += AnyTimeOrMealChanged;

                endTimeEdit.EditValueChanged -= AnyTimeOrMealChanged;
                endTimeEdit.EditValueChanged += AnyTimeOrMealChanged;

                // 勾午餐/晚餐、選晚餐分鐘也要刷新預估時間
                chkHasLunch.CheckedChanged -= AnyTimeOrMealChanged;
                chkHasLunch.CheckedChanged += AnyTimeOrMealChanged;

                chkHasDinner.CheckedChanged -= AnyTimeOrMealChanged;
                chkHasDinner.CheckedChanged += AnyTimeOrMealChanged;

                cmbDinnerMinutes.EditValueChanged -= AnyTimeOrMealChanged;
                cmbDinnerMinutes.EditValueChanged += AnyTimeOrMealChanged;

                // Init 下拉選單
                InitTestModeCombo();
                InitEngineerCombo();
                InitDinnerMinutesCombo();

                // 晚餐顯示文字 "xx 分"
                cmbDinnerMinutes.CustomDisplayText -= cmbDinnerMinutes_CustomDisplayText;
                cmbDinnerMinutes.CustomDisplayText += cmbDinnerMinutes_CustomDisplayText;

                // 依 TestMode 更新 TestItem
                UpdateTestItem(cmbTestMode.Text?.Trim() ?? "");
            }, caption: "初始化失敗");

            // JobNo 下拉
            await UiSafeRunner.SafeRunAsync(async () =>
            {
                cmbJobNo.Properties.Items.Clear();
                var jobNos = await _jobNoApiClient.GetActiveJobNoAsync(8, this._jobNoCts?.Token ?? default);
                cmbJobNo.Properties.Items.AddRange(jobNos.ToArray());
            }, caption: "載入 JobNo 失敗", setLoading: SetLoading);

            UiSafeRunner.SafeRun(() =>
            {
                // 清空日期時間
                startDateEdit.EditValue = null;
                endDateEdit.EditValue = null;
                startTimeEdit.EditValue = null;
                endTimeEdit.EditValue = null;

                RefreshMealAndEstimateUI();

                // 編輯模式才顯示的控制項
                btnDeletedRentTime.Visible = _editRentTimeId != null;
                btnRestoreRentTime.Visible = _editRentTimeId != null;
                btnRentTimeEnd.Visible = _editRentTimeId != null;
                btnRentTimeStart.Visible = _editRentTimeId != null;
                chkHandover.Visible = _editRentTimeId != null;
                lblCreatedBy.Visibility = _editRentTimeId != null ? DevExpress.XtraLayout.Utils.LayoutVisibility.Always : DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                txtCreatedBy.Visible = _editRentTimeId != null;
                btnCopyRentTime.Visible = _editRentTimeId != null;

            }, caption: "初始化 UI 失敗(後段)");

            // 新增模式：預設建單人員
            if (_editRentTimeId == null)
            {
                await UiSafeRunner.SafeRunAsync(async () =>
                {
                    _bookingBatchId = await _rentTimeApiClient.CreateBookingBatchAsync();
                    SetBookingNoToUI($"TMP-{_bookingBatchId.Value:D7}-1");

                    txtCreatedBy.Text = "Jimmy"; // 或改成 _currentUser
                    _uiStatus = UiRentStatus.Draft;

                    ApplyUiStatus();
                    ApplyTabByStatus();

                }, caption: "CreateBookingBatch 失敗", setLoading: SetLoading);

                // 若失敗：_bookingBatchId 仍是 null，就比照你原本邏輯把存檔鎖住
                if (_bookingBatchId == null)
                    btnCreatedRentTime.Enabled = false;

                return;
            }

            // 編輯模式：先讀 API，再填 UI（拆成兩段：API / 填回）
            RentTime? data = null;

            await UiSafeRunner.SafeRunAsync(async () =>
            {
                data = await _rentTimeApiClient.GetByIdAsync(_editRentTimeId.Value);
            }, caption: "讀取 RentTime 失敗", setLoading: SetLoading);

            if (data == null)
            {
                XtraMessageBox.Show("找不到此 RentTime（可能已被刪除）", "提示");
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            UiSafeRunner.SafeRun(() =>
            {
                _loadedRentTime = data;
                FillUIFromModel(data);

                SyncJobNoApiFlagsFromLoadedUI();

                _uiStatus = (UiRentStatus)data.Status;
                ApplyUiStatus();
                ApplyTabByStatus();

            }, caption: "填回 RentTime 到 UI 失敗");
        }

        // =========================================================
        // D) 按鈕流程：存檔 / 刪除
        // =========================================================
        private async void btnCreatedRentTime_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                PrintRentTime();
                return;
            }

            await UiSafeRunner.SafeRunAsync(async () =>
            {
                dxErrorProvider1.ClearErrors();

                if (!ValidateLocationUI()) return;
                if (!ValidateCompanyUI()) return;
                if (!ValidateSalesUI()) return;
                if (!ValidateStartDateUI()) return;
                if (!ValidateEndDateUI()) return;
                if (!ValidateStartTimeUI()) return;
                if (!ValidateEndTimeUI()) return;

                var model = BuildModelFormUI();

                // 保險：新增模式若 BookingNo 還是空，代表 InitNewBookingNoAsync/Batch 初始化沒成功
                if (_editRentTimeId == null && string.IsNullOrWhiteSpace(model.BookingNo))
                {
                    // 你若想「自動補救」：就先初始化一次（保留情況A：一開表單就看到暫定號）
                    // await InitNewBookingNoAsync();
                    // model.BookingNo = GetBookingNoFromUI();

                    // 若你要「先擋下來」避免送空值：
                    XtraMessageBox.Show("BookingNo 尚未初始化（CreateBookingBatch 可能失敗或尚未完成）", "提示");
                    return;
                }

                // 新增
                if (_editRentTimeId == null)
                {
                    var result = await _rentTimeApiClient.CreateRentTimeFromApiAsync(model, _bookingBatchId);

                    _lastCreatedBy = "Jimmy"; // 之後要刪除
                    SetBookingNoToUI(result.BookingNo);

                    XtraMessageBox.Show(
                        $"建立成功! \nRentTimeId：{result.RentTimeId}\nBookingNo：{result.BookingNo}",
                        "CreateRentTime");

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }

                // 編輯：必帶 RentTimeId
                model.RentTimeId = _editRentTimeId.Value;

                var confirm = XtraMessageBox.Show(
                    "確認儲存修改嗎?",
                    "確認儲存",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var user = _currentUser;

                await _rentTimeApiClient.UpdateRentTimeFromApiAsync(_editRentTimeId.Value, model, user);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }, caption: "存檔失敗", setLoading: SetLoading);
        }

        private async void btnRentTimeStart_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                UploadScanCopy();
                return;
            }

            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                var confirm = XtraMessageBox.Show(
                    " 確認要「租時開始」嗎？",
                    "租時開始",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                // (1) 先把 UI 當下內容存回 DB（包含 ActualStartAt / ActualEndAt）
                var model = BuildModelFormUI();
                model.RentTimeId = _editRentTimeId.Value;

                // (2) 再把狀態改成 Started（並寫入 ActualStartAt = now）
                var user = _currentUser;
                await _rentTimeApiClient.UpdateRentTimeFromApiAsync(_editRentTimeId.Value, model, user);

                await _rentTimeApiClient.StartRentTimeFromApiAsync(_editRentTimeId.Value, user);

                // (3) 重新讀 DB 刷新 UI
                await ReloadRentTimeFromApiAsync();
                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }, caption: "租時開始失敗", setLoading: SetLoading);
        }

        private async void btnRentTimeEnd_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                SubmitToAssistant();
                return;
            }

            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                if (chkHandover.Checked)
                {
                    var confirm = XtraMessageBox.Show(
                    "確認要「租時完成」嗎？完成後會鎖定不可修改。",
                    "租時單管理 - 提示訊息",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;
                }
                else
                {
                    var confirmHandOver = XtraMessageBox.Show("" +
                        "確認租時單沒有需要交接，並完成租時?", "租時單管理 - 提示訊息",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirmHandOver != DialogResult.Yes) return;
                }

                // (A) 先把 UI 當下內容存回 DB（包含 ActualStartAt / ActualEndAt）
                var model = BuildModelFormUI();
                model.RentTimeId = _editRentTimeId.Value;

                // (B) 再把狀態改成 Finished
                var user = _currentUser;
                await _rentTimeApiClient.UpdateRentTimeFromApiAsync(_editRentTimeId.Value, model, user);
                await _rentTimeApiClient.FinishRentTimeFromApiAsync(_editRentTimeId.Value, user);

                // (C) 立刻刷新 UI
                await ReloadRentTimeFromApiAsync(); // 完成後 UI 應該立刻鎖住 + Copy 亮起

                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }, caption: "租時完成失敗", setLoading: SetLoading);
        }

        private async void btnRestoreRentTime_Click(object sender, EventArgs e)
        {
            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                var confirm = XtraMessageBox.Show(
                    "確認要「回復狀態」到 Draft 嗎？",
                    "回復狀態",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var user = _currentUser;
                await _rentTimeApiClient.RestoreToDraftByIdAsync(_editRentTimeId.Value, user);

                await ReloadRentTimeFromApiAsync();  // 回復後 UI 應該解鎖

                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }, caption: "回復租時失敗", setLoading: SetLoading);
        }

        private async void btnDeletedRentTime_Click(object sender, EventArgs e)
        {
            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                var confirm = XtraMessageBox.Show(
                    "確認刪除嗎?",
                    "確認刪除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var user = _currentUser;
                await _rentTimeApiClient.DeleteRentTimeByIdAsync(_editRentTimeId.Value, user);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }, caption: "刪除租時單失敗", setLoading: SetLoading);
        }

        // 複製單據
        private async void btnCopyRentTime_Click(object sender, EventArgs e)
        {
            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                var createdBy = _currentUser;

                // 真正決定「要走哪一套複製規則」的變數
                bool continueSeq = false; // 預設：沒交接就開新單

                // 新規則：交接有勾 -> 讓使用者選「延續流水」或「開新單」
                if (chkHandover.Checked)
                {
                    var args = new XtraMessageBoxArgs
                    {
                        Caption = "複製租時單 - 選擇方式",
                        Text =
                            "此租時單有勾選交接，請選擇複製方式：\n\n" +
                            "【開新一筆】新的 BookingNo\n" +
                            "【延續流水】同 BookingNo，流水號 + 1",
                        Buttons = new[]
                        {
                            DialogResult.OK, DialogResult.Cancel },

                        DefaultButtonIndex = 1, // 預設選「延續流水」（比較不容易誤按造成開新）
                        Icon = SystemIcons.Warning
                    };

                    args.Showing += (s, e) =>
                    {
                        e.Buttons[DialogResult.OK].Text = "開新一筆";  // DialogResult.OK
                        e.Buttons[DialogResult.Cancel].Text = "延續流水";  // DialogResult.Cancel
                    };

                    var r = XtraMessageBox.Show(args);

                    // OK = 開新一筆 => 不延續
                    // Cancel = 延續流水 => 延續
                    continueSeq = (r != DialogResult.OK);
                }

                // 1. 先複製 -> DB 產生新 RentTime
                var result = await _rentTimeApiClient.CopyRentTimeByIdAsync(_editRentTimeId.Value, continueSeq, createdBy);

                NotifyRentTimeChanged();
                // 2. 直接打開新單（新 RentTimeId）
                this.Hide(); // 先把舊表單藏起來，避免畫面跳來跳去 

                using (var f = new Project(_rentTimeApiClient, _jobNoApiClient, result.RentTimeId))
                {
                    f.ShowDialog(this); // 用 this 當 owner（可不加，但加了比較穩）
                }

                // 3) 新單關掉後：把舊單也關掉，回傳 OK 讓外層刷新列表
                this.DialogResult = DialogResult.OK;
                this.Close();
            }, caption: "複製租時單失敗", setLoading: SetLoading);
        }

        // =========================================================
        // E) 事件：各種 UI 連動 / 刷新
        // =========================================================

        // 列印空方法
        private void PrintRentTime()
        {
            XtraMessageBox.Show("列印功能尚未實作", "列印");
        }

        // 上傳掃描影本空方法
        private void UploadScanCopy()
        {
            XtraMessageBox.Show("上傳掃描影本尚未實作", "上傳掃描影本");
        }

        // 送出給助理空方法
        private async void SubmitToAssistant()
        {
            await UiSafeRunner.SafeRunAsync(async () =>
            {
                if (_editRentTimeId == null) return;

                if (_uiStatus != UiRentStatus.Finished)
                {
                    XtraMessageBox.Show("只有「已完成」的租時單才能送出給助理", "提示");
                    return;
                }

                var confirm = XtraMessageBox.Show(
                    "確認要送出給助理嗎？\n送出後將進入「已送出」狀態", "送出給助理", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var user = _currentUser ?? "";

                if (string.IsNullOrWhiteSpace(user))
                {
                    XtraMessageBox.Show("找不到操作人（CreatedBy），請先確認表單建單人員欄位", "提示");
                    return;
                }

                await _rentTimeApiClient.SubmitToAssistantByIdAsync(_editRentTimeId.Value, user);

                // 重新讀 DB，讓 _uiStatus 變成 送出給助理
                await ReloadRentTimeFromApiAsync();

                //通知外面(ProjectView/Form1)刷新
                NotifyRentTimeChanged();

            }, caption: "送出給助理失敗", setLoading: SetLoading);
        }

        // 午餐/晚餐事件綁定
        private void AnyTimeOrMealChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return; // 你有 _isLoading 就先保護，避免程式塞值時一直連鎖觸發
            UiSafeRunner.SafeRun(() =>
            {
                RefreshMealAndEstimateUI();
            }, caption:"更新午餐/晚餐與預估時間失敗");
        }

        private void SyncJobNoApiFlagsFromLoadedUI()
        {
            UiSafeRunner.SafeRun(() =>
            {
                bool HasText(string? s) => !string.IsNullOrWhiteSpace(s);

                // JobNo 沒值: 一律視為沒查到
                if (!HasText(cmbJobNo.Text))
                {
                    _jobNoApiHasCustomer = false;
                    _jobNoApiHasSales = false;
                    return;
                }
                // 用 ProjectNo / ProjectName / PE 當作「API 有回主檔」的證據
                bool hasMaster = HasText(txtPE.Text) || HasText(txtProjectName.Text) || HasText(txtProjectNo.Text);

                _jobNoApiHasCustomer = hasMaster && HasText(cmbCompany.Text);
                _jobNoApiHasSales = hasMaster && HasText(txtSales.Text);
            }, caption: "同步 JobNo 失敗");
        }
    }
}
