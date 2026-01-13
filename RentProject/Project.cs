using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Mask;
using RentProject.Domain;
using RentProject.Service;
using RentProject.UIModels;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // A) 服務 / 狀態欄位 (State)
        // =========================================================
        private readonly RentTimeService _rentTimeService;
        private readonly ProjectService _projectService;
        private readonly JobNoService _jobNoService;

        private List<ProjectItem> _projects = new();

        // 午餐/晚餐規則門檻
        private static readonly TimeSpan LunchEnableAt = new(13, 0, 0);
        private static readonly TimeSpan DinnerEnableAt = new(18, 0, 0);

        // 編輯租時單模式
        private readonly int? _editRentTimeId = null;
        private static string _lastCreatedBy = "Jimmy";

        // 控制「程式在塞值」時，不要被事件誤判為手動修改
        private bool _isLoading = false;

        // 使用者是否手動改過聯絡資訊（同公司就不再覆蓋）
        private bool _contactManuallyEdited = false;

        // 記錄上一次選的公司（用來判斷是否換公司）
        private string _lastCompany = "";

        // 紀錄BookingBatchId
        private long? _bookingBatchId;

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

        // =========================================================
        // C) 建構子
        // =========================================================
        public Project(RentTimeService rentTimeService, ProjectService projectService, JobNoService jobNoService)
        {
            InitializeComponent();
            _rentTimeService = rentTimeService;
            _projectService = projectService;
            _jobNoService = jobNoService;
        }

        public Project(RentTimeService rentTimeService, ProjectService projectService, JobNoService jobNoService, int rentTimeId)
            : this(rentTimeService, projectService, jobNoService)
        {
            _editRentTimeId = rentTimeId;
        }

        // =========================================================
        // D) Form Load：初始化 UI
        // =========================================================
        private void Project_Load(object sender, EventArgs e)
        {
            _projects = _projectService.GetActiveProjects();

            // ===== 新增：修正 DateEdit 和 TimeEdit 的 Mask 問題 =====

            // 1. 設定 DateEdit（處理日期輸入問題）
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

            // 綁定「聯絡資訊」手動修改偵測
            cmbJobNo.EditValueChanged -= cmbJobNo_EditValueChanged;
            cmbJobNo.EditValueChanged += cmbJobNo_EditValueChanged;

            txtContactName.EditValueChanged -= ContactFields_EditValueChanged;
            txtContactName.EditValueChanged += ContactFields_EditValueChanged;

            txtContactPhone.EditValueChanged -= ContactFields_EditValueChanged;
            txtContactPhone.EditValueChanged += ContactFields_EditValueChanged;

            txtSales.EditValueChanged -= ContactFields_EditValueChanged;
            txtSales.EditValueChanged += ContactFields_EditValueChanged;

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

            // 手動輸入後，自動保存(Validated 事件)
            cmbJobNo.Validated -= cmbJobNo_Validated;
            cmbJobNo.Validated += cmbJobNo_Validated;

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
            InitContactCompany();
            InitTestModeCombo();
            InitEngineerCombo();
            InitDinnerMinutesCombo();

            // 晚餐顯示文字 "xx 分"
            cmbDinnerMinutes.CustomDisplayText -= cmbDinnerMinutes_CustomDisplayText;
            cmbDinnerMinutes.CustomDisplayText += cmbDinnerMinutes_CustomDisplayText;

            // 依 TestMode 更新 TestItem
            UpdateTestItem(cmbTestMode.Text?.Trim() ?? "");

            // JobNo 下拉
            cmbJobNo.Properties.Items.Clear();
            cmbJobNo.Properties.Items.AddRange(_jobNoService.GetActiveJobNos());

            cmbCompany.EditValueChanged -= cmbCompany_EditValueChanged;
            cmbCompany.EditValueChanged += cmbCompany_EditValueChanged;

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
            labelCreatedBy.Visible = _editRentTimeId != null;
            txtCreatedBy.Visible = _editRentTimeId != null;
            btnCopyRentTime.Visible = _editRentTimeId != null;

            // 新增模式：預設建單人員
            if (_editRentTimeId == null)
            {
                _bookingBatchId = _rentTimeService.CreateBookingBatch();

                txtBookingNo.Text = $"TMP-{_bookingBatchId.Value:D7}";

                txtBookingSeq.Text = "1";

                txtCreatedBy.Text = "Jimmy";

                _uiStatus = UiRentStatus.Draft;

                ApplyUiStatus();
                ApplyTabByStatus();
                return;
            }

            // 編輯模式：讀 DB 填回 UI
            var data = _rentTimeService.GetRentTimeById(_editRentTimeId.Value);
            _loadedRentTime = data;
            FillUIFromModel(data);

            _uiStatus = (UiRentStatus)data.Status;
            ApplyUiStatus();
            ApplyTabByStatus();
            //btnCreatedRentTime.Text = "儲存修改";
            //labelEstimatedHours.Text = "實際時間";
            //this.Text = "修改租時單";
        }

        // =========================================================
        // E) 按鈕流程：存檔 / 刪除
        // =========================================================
        private void btnCreatedRentTime_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                PrintRentTime();
                return;
            }

            try
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

                // 新增
                if (_editRentTimeId == null)
                {
                    var result = _rentTimeService.CreateRentTime(model, _bookingBatchId);

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

                _rentTimeService.UpdateRentTimeById(model);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        private void btnRentTimeStart_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                UploadScanCopy();
                return;
            }

            try
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
                _rentTimeService.UpdateRentTimeById(model);

                // (2) 再把狀態改成 Started（並寫入 ActualStartAt = now）
                var user = txtCreatedBy.Text.Trim();
                _rentTimeService.StartRentTimeById(_editRentTimeId.Value, user);

                // (3) 重新讀 DB 刷新 UI
                ReloadRentTimeFromDb();

                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "Error");
            }
        }

        private void btnRentTimeEnd_Click(object sender, EventArgs e)
        {
            if (_uiStatus == UiRentStatus.Finished)
            {
                SubmitToAssistant();
                return;
            }

            try
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

                var user = txtCreatedBy.Text.Trim();

                // (A) 先把 UI 當下內容存回 DB（包含 ActualStartAt / ActualEndAt）
                var model = BuildModelFormUI();
                model.RentTimeId = _editRentTimeId.Value;
                _rentTimeService.UpdateRentTimeById(model);

                // (B) 再把狀態改成 Finished
                _rentTimeService.FinishRentTimeById(_editRentTimeId.Value, user);

                // (C) 立刻刷新 UI
                ReloadRentTimeFromDb(); // 完成後 UI 應該立刻鎖住 + Copy 亮起

                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }

        }

        private void btnRestoreRentTime_Click(object sender, EventArgs e)
        {
            try
            {
                if (_editRentTimeId == null) return;

                var confirm = XtraMessageBox.Show(
                    "確認要「回復狀態」到 Draft 嗎？",
                    "回復狀態",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var user = txtCreatedBy.Text.Trim();
                _rentTimeService.RestoreToDraftById(_editRentTimeId.Value, user);

                ReloadRentTimeFromDb(); // 回復後 UI 應該解鎖

                NotifyRentTimeChanged(); // 通知外面刷新 ProjectView
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        private void btnDeletedRentTime_Click(object sender, EventArgs e)
        {
            try
            {
                if (_editRentTimeId == null) return;

                var confirm = XtraMessageBox.Show(
                    "確認刪除嗎?",
                    "確認刪除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                var createdBy = txtCreatedBy.Text.Trim();
                _rentTimeService.DeletedRentTime(_editRentTimeId.Value, createdBy, DateTime.Now);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        // 複製單據
        private void btnCopyRentTime_Click(object sender, EventArgs e)
        {
            try
            {
                if (_editRentTimeId == null) return;

                var createdBy = txtCreatedBy.Text.Trim();

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
                var result = _rentTimeService.CopyRentTime(_editRentTimeId.Value, continueSeq, createdBy);

                NotifyRentTimeChanged();
                // 2. 直接打開新單（新 RentTimeId）
                this.Hide(); // 先把舊表單藏起來，避免畫面跳來跳去 

                using (var f = new Project(_rentTimeService, _projectService, _jobNoService, result.RentTimeId))
                {
                    f.ShowDialog(this); // 用 this 當 owner（可不加，但加了比較穩）
                }

                // 3) 新單關掉後：把舊單也關掉，回傳 OK 讓外層刷新列表
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        // =========================================================
        // G) 事件：各種 UI 連動 / 刷新
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
        private void SubmitToAssistant()
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

            var user = txtCreatedBy.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(user))
            {
                XtraMessageBox.Show("找不到操作人（CreatedBy），請先確認表單建單人員欄位", "提示");
                return;
            }

            _rentTimeService.SubmitToAssistantById(_editRentTimeId.Value, user);

            // 重新讀 DB，讓 _uiStatus 變成 送出給助理
            ReloadRentTimeFromDb();
        }

        private static int ParseIntOrZero(string? s)
            => int.TryParse(s?.Trim(), out var v) ? v : 0;

        // 午餐/晚餐事件綁定
        private void AnyTimeOrMealChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return; // 你有 _isLoading 就先保護，避免程式塞值時一直連鎖觸發
            RefreshMealAndEstimateUI();
        }

        private void labelControl17_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void labelControl26_Click(object sender, EventArgs e)
        {

        }
    }

}
