using DevExpress.XtraEditors;
using RentProject.Domain;
using RentProject.Service;
using RentProject.Shared.UIModels;
using RentProject.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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
        private enum UiRentStatus { Draft = 0, Started = 1, Finished = 2};
        private UiRentStatus _uiStatus = UiRentStatus.Draft;

        // =========================================================
        // B) 假資料 / 資料來源
        // =========================================================
        private readonly List<LocationItem> _locations = new()
        {
            new LocationItem { Location = "Conducted 1", Area = "WG" },
            new LocationItem { Location = "Conducted 2", Area = "WG" },
            new LocationItem { Location = "Conducted 3", Area = "WG" },
            new LocationItem { Location = "Conducted 4", Area = "WG" },
            new LocationItem { Location = "Conducted 5", Area = "WG" },
            new LocationItem { Location = "Conducted 6", Area = "WG" },
            new LocationItem { Location = "SAC 1", Area = "WG" },
            new LocationItem { Location = "SAC 2", Area = "WG" },
            new LocationItem { Location = "SAC 3", Area = "WG" },
            new LocationItem { Location = "FAC 1", Area = "WG" },
            new LocationItem { Location = "Setup Room 1", Area = "WG" },
            new LocationItem { Location = "Conducted A", Area = "HY" },
            new LocationItem { Location = "Conducted B", Area = "HY" },
            new LocationItem { Location = "Conducted C", Area = "HY" },
            new LocationItem { Location = "Conducted D", Area = "HY" },
            new LocationItem { Location = "Conducted E", Area = "HY" },
            new LocationItem { Location = "Conducted F", Area = "HY" },
            new LocationItem { Location = "SAC C", Area = "HY" },
            new LocationItem { Location = "SAC D", Area = "HY" },
            new LocationItem { Location = "SAC G", Area = "HY" },
            new LocationItem { Location = "FAC A", Area = "HY" },
            new LocationItem { Location = "Setup Room A", Area = "HY" },
        };

        private readonly List<TestModeTestItem> _tests = new()
        {
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Conducted Power"},
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Band Edge"},
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Spurious Emission"},
            new TestModeTestItem { TestMode = "Radiated", TestItem = "Band Edge"},
            new TestModeTestItem { TestMode = "Radiated", TestItem = "Spurious Emission"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "Adaptivity"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "Receiver Blocking"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "DFS"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "PWS"},
            new TestModeTestItem { TestMode = "Setup", TestItem = "Debug"},
        };

        private readonly List<ContactCompany> _contactCompany = new()
        {
            new ContactCompany { Company = "好厲害科技公司", Sales = "Thomas", ContactName = "Tom", ContactPhone = "0912378999"},
            new ContactCompany { Company = "台灣科技公司", Sales = "Brian", ContactName = "Bob", ContactPhone = "0955577768"},
            new ContactCompany { Company = "台灣傳統公司", Sales = "Chovy", ContactName = "Faker", ContactPhone = "0998765432"},
        };

        private readonly List<string> _engineers = new()
        {
            "Jimmy",
            "Brian",
            "Tom",
            "Bob",
            "Faker"
        };

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

            // 綁定「聯絡資訊」手動修改偵測
            cmbJobNo.EditValueChanged -= cmbJobNo_EditValueChanged;
            cmbJobNo.EditValueChanged += cmbJobNo_EditValueChanged;

            txtContactName.EditValueChanged -= ContactFields_EditValueChanged;
            txtContactName.EditValueChanged += ContactFields_EditValueChanged;

            txtContactPhone.EditValueChanged -= ContactFields_EditValueChanged;
            txtContactPhone.EditValueChanged += ContactFields_EditValueChanged;

            txtSales.EditValueChanged -= ContactFields_EditValueChanged;
            txtSales.EditValueChanged += ContactFields_EditValueChanged;

            // 手動輸入後，自動保存(Validated 事件)
            cmbJobNo.Validated -= cmbJobNo_Validated;
            cmbJobNo.Validated += cmbJobNo_Validated;

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
            //cmbEngineer.ReadOnly = _editRentTimeId != null;
            //txtCreatedBy.ReadOnly = _editRentTimeId != null;
            //cmbCompany.ReadOnly = _editRentTimeId != null;
            //txtSales.ReadOnly = _editRentTimeId != null;

            // 新增模式：預設建單人員
            if (_editRentTimeId == null)
            {
                _bookingBatchId = _rentTimeService.CreateBookingBatch();

                txtBookingNo.Text = $"RF-{_bookingBatchId.Value:D7}";

                txtBookingSeq.Text = "1";

                txtCreatedBy.Text = "Jimmy";

                _uiStatus = UiRentStatus.Draft;

                ApplyUiStatus();

                return;
            }

            // 編輯模式：讀 DB 填回 UI
            var data = _rentTimeService.GetRentTimeById(_editRentTimeId.Value);
            FillUIFromModel(data);

            _uiStatus = (UiRentStatus)data.Status;
            ApplyUiStatus();

            btnCreatedRentTime.Text = "儲存修改";
            labelEstimatedHours.Text = "實際時間";
            this.Text = "修改租時單";
        }

        // =========================================================
        // E) 按鈕流程：存檔 / 刪除
        // =========================================================
        private void btnCreatedRentTime_Click(object sender, EventArgs e)
        {
            try
            {
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

        // =========================================================
        // F) 事件：偵測使用者手動修改聯絡資訊
        // =========================================================
        private void ContactFields_EditValueChanged(object sender, EventArgs e)
        {
            // 程式塞值不算「手動改」
            if (_isLoading) return;
            _contactManuallyEdited = true;
        }

        // =========================================================
        // G) 事件：各種 UI 連動 / 刷新
        // =========================================================

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

        // =========================================================
        // H) Init：填下拉選單
        // =========================================================
        private void InitContactCompany()
        {
            var companies = _contactCompany
                .Select(x => x.Company)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            cmbCompany.Properties.Items.Clear();
            cmbCompany.Properties.Items.AddRange(companies);

            cmbCompany.EditValue = null;
            cmbCompany.SelectedIndex = -1;
        }

        private void InitTestModeCombo()
        {
            var modes = _tests
                .Select(x => x.TestMode)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            cmbTestMode.Properties.Items.Clear();
            cmbTestMode.Properties.Items.AddRange(modes);

            cmbTestMode.EditValue = null;
            cmbTestMode.SelectedIndex = -1;

            cmbTestItem.Properties.Items.Clear();
            cmbTestItem.EditValue = null;
            cmbTestItem.SelectedIndex = -1;

            cmbTestItem.EditValue = false;
        }

        private void UpdateTestItem(string mode)
        {
            var items = _tests
                .Where(x => x.TestMode == mode)
                .Select(x => x.TestItem)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            cmbTestItem.Properties.Items.Clear();
            cmbTestItem.Properties.Items.AddRange(items);

            cmbTestItem.SelectedIndex = -1;
        }

        private void InitEngineerCombo()
        {
            cmbEngineer.Properties.Items.Clear();
            cmbEngineer.Properties.Items.AddRange(_engineers);

            // 新增時先不要預設任何人
            cmbEngineer.EditValue = null;
            cmbEngineer.SelectedIndex = -1;
            cmbEngineer.Text = "";
        }

        private void InitDinnerMinutesCombo()
        {
            cmbDinnerMinutes.Properties.Items.Clear();
            cmbDinnerMinutes.Properties.Items.AddRange(new object[] { 30, 60, 90, 120, 150, 180, 210, 240 });

            // 預設值
            cmbDinnerMinutes.EditValue = 60;
        }

        // =========================================================
        // I) UI 規則：午餐 / 晚餐 / Enable 條件
        // =========================================================
        private void ApplyLunchUI()
        {
            txtLunchMinutes.Properties.ReadOnly = true;
            txtLunchMinutes.Text = chkHasLunch.Checked ? "60分" : "0";
        }

        private void ApplyDinnerUI()
        {
            cmbDinnerMinutes.Enabled = chkHasDinner.Checked;

            if (!chkHasDinner.Checked)
            {
                cmbDinnerMinutes.EditValue = null;
                return;
            }

            if (cmbDinnerMinutes.EditValue is not int)
            {
                cmbDinnerMinutes.EditValue = 60;
            }
        }

        private void ApplyMealEnableByEndTime()
        {
            var startDate = startDateEdit.EditValue as DateTime?;
            var startTime = startTimeEdit.EditValue is DateTime t1 ? t1.TimeOfDay : (TimeSpan?)null;
            var endDate = endDateEdit.EditValue as DateTime?;
            var endTime = endTimeEdit.EditValue is DateTime t2 ? t2.TimeOfDay : (TimeSpan?)null;

            bool canLunch = false;
            bool canDinner = false;

            if (startDate is not null && startTime is not null && endDate is not null && endTime is not null)
            {
                var start = startDate.Value.Date + startTime.Value;
                var end = endDate.Value.Date + endTime.Value;

                canLunch = end.TimeOfDay >= LunchEnableAt && start.TimeOfDay < LunchEnableAt;
                canDinner = end.TimeOfDay >= DinnerEnableAt && start.TimeOfDay < DinnerEnableAt;
            }

            chkHasLunch.Enabled = canLunch;
            if (!canLunch) chkHasLunch.Checked = false;

            chkHasDinner.Enabled = canDinner;
            if (!canDinner) chkHasDinner.Checked = false;

            ApplyLunchUI();
            ApplyDinnerUI();
        }

        private void cmbDinnerMinutes_CustomDisplayText(object sender, DevExpress.XtraEditors.Controls.CustomDisplayTextEventArgs e)
        {
            if (e.Value is int v)
            {
                e.DisplayText = $"{v} 分";
                return;
            }

            if (e.Value != null && int.TryParse(e.Value.ToString(), out var v2))
            {
                e.DisplayText = $"{v2} 分";
                return;
            }

            e.DisplayText = "";
        }

        // 決定哪些按鈕狀態
        private void ApplyUiStatus()
        { 
            bool isEdit = _editRentTimeId != null;
            bool isStarted = _uiStatus == UiRentStatus.Started;
            bool isFinished = _uiStatus == UiRentStatus.Finished;

            // 儲存修改：Finished 不可改
            btnCreatedRentTime.Enabled = !isFinished;

            // 租時開始：只有在編輯模式 + Draft 才能按
            btnRentTimeStart.Enabled = isEdit && !isFinished && !isStarted;

            // 租時完成：只有在編輯模式 + 租時開始 才能按
            btnRentTimeEnd.Enabled = isEdit&& !isFinished && isStarted;

            // 刪除租時：完成租時不能刪
            btnDeletedRentTime.Enabled = isEdit && !isFinished;

            // 回復狀態：Draft/Started 可按，Finished 不可按
            btnRestoreRentTime.Enabled = isEdit && !isFinished;

            // 複製單據：建立完成 + 租時完成才可以按
            btnCopyRentTime.Enabled = isEdit && isFinished;

            // Finished：只能檢視
            SetFormEditable(!isFinished);
        }

        // 決定欄位能不能編輯
        private void SetFormEditable(bool editable)
        { 
            cmbLocation.Properties.ReadOnly = !editable;
            cmbJobNo.Properties.ReadOnly = !editable;
            cmbCompany.Properties.ReadOnly = !editable;

            txtContactName.Properties.ReadOnly = !editable;
            txtContactPhone.Properties.ReadOnly = !editable;
            txtSales.Properties.ReadOnly = !editable;
            txtSampleModel.Properties.ReadOnly = !editable;
            txtSampleNo.Properties.ReadOnly = !editable;

            startDateEdit.Properties.ReadOnly = !editable;
            endDateEdit.Properties.ReadOnly = !editable;
            startTimeEdit.Properties.ReadOnly = !editable;
            endTimeEdit.Properties.ReadOnly = !editable;

            chkHasLunch.Properties.ReadOnly = !editable;
            chkHasDinner.Properties.ReadOnly = !editable;
            cmbDinnerMinutes.Properties.ReadOnly = !editable;

            cmbEngineer.Properties.ReadOnly = !editable;
            cmbTestMode.Properties.ReadOnly = !editable;
            cmbTestItem.Properties.ReadOnly = !editable;

            memoTestInformation.Properties.ReadOnly = !editable;
            memoNote.Properties.ReadOnly = !editable;

            chkHandover.Properties.ReadOnly = !editable;

            // CreatedBy 通常永遠不可改（可留著鎖）
            txtCreatedBy.Properties.ReadOnly = true;
        }

        // =========================================================
        // J) UI <-> Model：組 Model / 回填 UI
        // =========================================================
        private RentTime BuildModelFormUI()
        {
            int dinnerMin = cmbDinnerMinutes.EditValue is int v ? v : 0;

            var jobNo = cmbJobNo.Text?.Trim();
            int? jobId = null;

            if (!string.IsNullOrWhiteSpace(jobNo))
            { 
                jobId = _jobNoService.GetOrCreateJobId(jobNo);
            }

            return new RentTime
            {
                CreatedBy = txtCreatedBy.Text.Trim(),
                Area = txtArea.Text.Trim(),
                CustomerName = cmbCompany.Text.Trim(),
                Sales = txtSales.Text.Trim(),
                JobId = jobId,
                JobNo = jobNo,
                ProjectName = txtProjectName.Text.Trim(),
                PE = txtPE.Text.Trim(),
                ProjectNo = txtProjectNo.Text.Trim(),
                Location = cmbLocation.Text.Trim(),

                ContactName = txtContactName.Text.Trim(),
                Phone = txtContactPhone.Text.Trim(),
                TestInformation = memoTestInformation.Text.Trim(),
                EngineerName = cmbEngineer.Text.Trim(),
                SampleModel = txtSampleModel.Text.Trim(),
                SampleNo = txtSampleNo.Text.Trim(),
                TestMode = cmbTestMode.Text.Trim(),
                TestItem = cmbTestItem.Text.Trim(),
                Notes = memoNote.Text.Trim(),

                HasLunch = chkHasLunch.Checked,
                LunchMinutes = chkHasLunch.Checked ? 60 : 0,

                HasDinner = chkHasDinner.Checked,
                DinnerMinutes = chkHasDinner.Checked ? dinnerMin : 0,

                IsHandOver = chkHandover.Checked,

                StartDate = startDateEdit.EditValue as DateTime?,
                EndDate = endDateEdit.EditValue as DateTime?,
                StartTime = startTimeEdit.EditValue is DateTime t1 ? t1.TimeOfDay : (TimeSpan?)null,
                EndTime = endTimeEdit.EditValue is DateTime t2 ? t2.TimeOfDay : (TimeSpan?)null,
            };
        }

        private void FillUIFromModel(RentTime data)
        {
            _isLoading = true;
            try
            {
                // 文字訊息
                SetBookingNoToUI(data.BookingNo);
                txtCreatedBy.Text = data.CreatedBy ?? "";
                txtArea.Text = data.Area ?? "";
                cmbCompany.Text = data.CustomerName ?? "";
                txtSales.Text = data.Sales ?? "";
                cmbJobNo.Text = data.JobNo ?? "";
                txtProjectNo.Text = data.ProjectNo ?? "";
                txtProjectName.Text = data.ProjectName ?? "";
                txtPE.Text = data.PE ?? "";
                cmbLocation.Text = data.Location ?? "";

                txtContactName.Text = data.ContactName ?? "";
                txtContactPhone.Text = data.Phone ?? "";
                memoTestInformation.Text = data.TestInformation ?? "";
                cmbEngineer.Text = string.IsNullOrWhiteSpace(data.EngineerName) ? "Jimmy" : data.EngineerName;
                txtSampleModel.Text = data.SampleModel ?? "";
                txtSampleNo.Text = data.SampleNo ?? "";
                cmbTestMode.Text = data.TestMode ?? "";
                cmbTestItem.Text = data.TestItem ?? "";
                memoNote.Text = data.Notes ?? "";

                // 日期
                startDateEdit.EditValue = data.StartDate;
                endDateEdit.EditValue = data.EndDate;

                // 時間：TimeEdit 的 EditValue 通常要 DateTime
                startTimeEdit.EditValue = data.StartTime.HasValue ? DateTime.Today.Add(data.StartTime.Value) : null;
                endTimeEdit.EditValue = data.EndTime.HasValue ? DateTime.Today.Add(data.EndTime.Value) : null;

                // 午餐/晚餐
                chkHasLunch.Checked = data.HasLunch;
                chkHasDinner.Checked = data.HasDinner;

                // 交接
                chkHandover.Checked = data.IsHandOver;

                txtLunchMinutes.Text = data.HasLunch ? data.LunchMinutes.ToString() : "0";
                cmbDinnerMinutes.EditValue = data.HasDinner ? data.DinnerMinutes : (object?)null;
            }
            finally
            {
                _isLoading = false;
            }

            RefreshMealAndEstimateUI();
        }

        // =========================================================
        // K) 計算：預估時間 / 集中刷新
        // =========================================================
        private void UpdateEstimatedUI()
        {
            var startDate = startDateEdit.EditValue as DateTime?;
            var endDate = endDateEdit.EditValue as DateTime?;
            var startTime = startTimeEdit.EditValue is DateTime t1 ? t1.TimeOfDay : (TimeSpan?)null;
            var endTime = endTimeEdit.EditValue is DateTime t2 ? t2.TimeOfDay : (TimeSpan?)null;

            int dinnerMin = cmbDinnerMinutes.EditValue is int v ? v : 0;

            if (startDate is null || endDate is null || startTime is null || endTime is null)
                return;

            var start = startDate.Value.Date + startTime.Value;
            var end = endDate.Value.Date + endTime.Value;

            if (end < start)
                return;

            var minutes = (int)(end - start).TotalMinutes;

            if (chkHasLunch.Checked) minutes -= 60;
            if (chkHasDinner.Checked) minutes -= dinnerMin;

            if (minutes < 0) minutes = 0;

            var hours = Math.Round(minutes / 60m, 2);
            txtEstimatedHours.Text = $"{hours}";
        }

        private static int ParseIntOrZero(string? s)
            => int.TryParse(s?.Trim(), out var v) ? v : 0;

        private void RefreshMealAndEstimateUI()
        {
            ApplyMealEnableByEndTime();
            UpdateEstimatedUI();
        }

        private void SetBookingNoToUI(string? bookingNo)
        {
            txtBookingNo.Text = "";
            txtBookingSeq.Text = "";

            if (string.IsNullOrWhiteSpace(bookingNo))
                return;

            var parts = bookingNo.Split('-');

            if (parts.Length < 3)
            { 
                txtBookingNo.Text = bookingNo;
                return;
            }

            // 最後一段是流水號
            var seq = parts[^1]; // "2"

            // 前面全部當作主號
            var prefix = string.Join("-", parts.Take(parts.Length - 1)); // "RF-000004"

            txtBookingNo.Text = prefix;
            txtBookingSeq.Text = seq; 
        }
    }
}
