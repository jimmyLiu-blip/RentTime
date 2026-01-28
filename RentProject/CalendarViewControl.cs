using DevExpress.XtraEditors;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Drawing;
using RentProject.Domain;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using RentProject.UI;

namespace RentProject
{
    public partial class CalendarViewControl : XtraUserControl
    {
        // ====== 狀態欄位（State） ======
        private DateTime _currentMonth; // 永遠存「當月 1 號」

        // 防止 Scheduler 重複初始化
        private bool _schedulerInited = false;

        private List<CalendarRentTimeItem> _apptsAll = new();
        private List<CalendarRentTimeItem> _apptsMonth = new();

        private List<CalendarRentTimeDetailItem> _details = new();
        private Dictionary<int, CalendarRentTimeDetailItem> _detailById = new();
        private Dictionary<string, CalendarRentTimeDetailItem> _pickByBookingNo = new(StringComparer.OrdinalIgnoreCase);

        // CalendarView 只負責「發出我要編輯哪一筆」
        public event Action<int>? EditRequested;

        public event Func<int, DateTime, DateTime, Task<bool>>? PeriodChangeRequested;

        // 讓 Form1 可以把 SetMainLoading 傳進來
        public Action<bool>? SetLoadingAction { get; set; }

        // 統一入口：async
        private Task SafeRunAsync(Func<Task> action, string caption)
            => UiSafeRunner.SafeRunAsync(action, caption: caption, setLoading: SetLoadingAction);

        // 統一入口：sync action 也能用
        private Task SafeRunAsync(Action action, string caption)
            => UiSafeRunner.SafeRunAsync(() => { action(); return Task.CompletedTask; },
                                         caption: caption,
                                         setLoading: SetLoadingAction);

        // 給「不能 async」的方法用（LoadData / GetSelectedDetail 這種）
        private void SafeRunSync(Action action, string caption)
            => SafeRunAsync(action, caption).GetAwaiter().GetResult();

        // BeginInvoke 也走 UiSafeRunner（避免你到處 try/catch）
        private void SafeBeginInvoke(Func<Task> action, string caption)
        {
            this.BeginInvoke(new Action(() => _ = SafeRunAsync(action, caption)));
        }

        // 是否為「7天週時間表模式」
        private bool _isWeek7Mode = false;

        // 防止遞迴的旗標
        private bool _symcingStart = false;

        // ====== 建構 / 生命週期 ======
        public CalendarViewControl()
        {
            InitializeComponent();
        }

        private async void CalendarViewControl_Load(object sender, EventArgs e)
        {
            await SafeRunAsync(() =>
            {
                EnsureSchedulerInit();

                HideDetailPanel();
                ClearDetailPanel();
            }, "CalendarView 初始化失敗");
        }

        // ====== Scheduler 初始化（只做一次） ======
        private void EnsureSchedulerInit()
        {
            if (_schedulerInited) return;

            try
            {
                // 1) 顯示模式與 UI 外觀
                schedulerControl1.ActiveViewType = SchedulerViewType.Month;
                schedulerControl1.MonthView.DateTimeScrollbarVisible = true;
                schedulerControl1.DateNavigationBar.Visible = false;  // 關閉Calendar中內建的日期切換

                // 2) 綁 DataStorage（Scheduler 必要）
                schedulerControl1.DataStorage = schedulerDataStorage1;

                // 3) Appointment Mapping
                schedulerDataStorage1.Appointments.Mappings.AppointmentId = nameof(CalendarRentTimeItem.RentTimeId);
                schedulerDataStorage1.Appointments.Mappings.Start = nameof(CalendarRentTimeItem.StartAt);
                schedulerDataStorage1.Appointments.Mappings.End = nameof(CalendarRentTimeItem.EndAt);
                schedulerDataStorage1.Appointments.Mappings.Subject = nameof(CalendarRentTimeItem.Subject);
                schedulerDataStorage1.Appointments.Mappings.Location = nameof(CalendarRentTimeItem.Location);
                schedulerDataStorage1.Appointments.Mappings.Label = nameof(CalendarRentTimeItem.LabelId);

                schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                    new AppointmentCustomFieldMapping("IsSummary", nameof(CalendarRentTimeItem.IsSummary)));
                schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                    new AppointmentCustomFieldMapping("RentTimeIds", nameof(CalendarRentTimeItem.RentTimeIds)));

                // 5) 月曆上的 appointment 顯示設定
                schedulerControl1.MonthView.AppointmentDisplayOptions.AppointmentAutoHeight = true;
                schedulerControl1.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
                schedulerControl1.MonthView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;

                // 6) 預設顯示當月
                var today = DateTime.Today;
                _currentMonth = new DateTime(today.Year, today.Month, 1);
                schedulerControl1.Start = _currentMonth;

                schedulerControl1.OptionsCustomization.AllowAppointmentDrag = UsedAppointmentType.Custom;
                schedulerControl1.OptionsCustomization.AllowAppointmentResize = UsedAppointmentType.Custom;

                schedulerControl1.AllowAppointmentDrag -= schedulerControl1_AllowAppointmentDrag;
                schedulerControl1.AllowAppointmentDrag += schedulerControl1_AllowAppointmentDrag;

                schedulerControl1.AllowAppointmentResize -= schedulerControl1_AllowAppointmentResize;
                schedulerControl1.AllowAppointmentResize += schedulerControl1_AllowAppointmentResize;

                schedulerControl1.AppointmentDrop -= schedulerControl1_AppointmentDrop;
                schedulerControl1.AppointmentDrop += schedulerControl1_AppointmentDrop;

                schedulerControl1.AppointmentResized -= schedulerControl1_AppointmentResized;
                schedulerControl1.AppointmentResized += schedulerControl1_AppointmentResized;

                schedulerControl1.ActiveViewChanged += (s, e) =>
                {
                    try
                    {
                        ApplyAppointForCurrentView();
                    }
                    catch (Exception ex)
                    {
                        XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換視圖刷新失敗");
                    }
                };

                schedulerControl1.MouseDown -= schedulerControl1_MouseDown;
                schedulerControl1.MouseDown += schedulerControl1_MouseDown;

                schedulerControl1.MouseDoubleClick -= schedulerControl1_MouseDoubleClick;
                schedulerControl1.MouseDoubleClick += schedulerControl1_MouseDoubleClick;

                schedulerControl1.VisibleIntervalChanged -= schedulerControl1_VisibleIntervalChanged;
                schedulerControl1.VisibleIntervalChanged += schedulerControl1_VisibleIntervalChanged;

                cmbBookingNo.EditValueChanged -= cmbBookingNo_EditValueChanged;
                cmbBookingNo.EditValueChanged += cmbBookingNo_EditValueChanged;

                schedulerControl1.EditAppointmentFormShowing -= schedulerControl1_EditAppointmentFormShowing;
                schedulerControl1.EditAppointmentFormShowing += schedulerControl1_EditAppointmentFormShowing;

                InitStatusLabels();

                _schedulerInited = true;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "Scheduler初始化失敗");
            }
        }

        private void InitStatusLabels()
        {
            var labels = schedulerDataStorage1.Appointments.Labels;

            labels.Clear();

            void AddLabel(int id, string name, Color color)
            {
                var label = labels.CreateNewLabel(id, name);
                label.Color = color;
                labels.Add(label);
            }

            AddLabel(0, "草稿", Color.LightGreen);
            AddLabel(1, "租時中", Color.LightSkyBlue);
            AddLabel(2, "已完成", Color.LightGray);
            AddLabel(3, "已送出給助理", Color.Khaki);
            AddLabel(99, "摘要", Color.Gainsboro);
        }

        // ====== 外部可呼叫：載入資料 ======
        public void LoadData(List<RentTime> list)
        {
            try
            {
                string StatusToText(int s) => s switch
                {
                    0 => "草稿",
                    1 => "租時中",
                    2 => "已完成",
                    3 => "已送出",
                    _ => "未知"
                };

                EnsureSchedulerInit();

                // 先同步一次（避免 MonthView 的 Start 落在上個月）
                SyncCurrentMonthFromScheduler();

                var keepStart = (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                    ? _currentMonth
                    : schedulerControl1.Start;

                // 1) 轉成 Scheduler 要吃的 Appointment DataSource
                var appts = list
                    .Where(x => x.StartDate != null && x.EndDate != null && x.StartTime != null && x.EndTime != null)
                    .Select(x => new CalendarRentTimeItem
                    {
                        RentTimeId = x.RentTimeId,
                        StartAt = x.StartDate.Value.Date + x.StartTime.Value,
                        EndAt = x.EndDate.Value.Date + x.EndTime.Value,
                        Subject = $"[{StatusToText(x.Status)}]\r\n{x.BookingNo}\r\n{x.CustomerName}\r\n",
                        Location = x.Location ?? "",
                        Status = x.Status,
                        LabelId = x.Status
                    })
                    .ToList();

                _details = list
                    .Where(x => x.StartDate != null && x.EndDate != null && x.StartTime != null && x.EndTime != null)
                    .Select(x => new CalendarRentTimeDetailItem
                    {
                        RentTimeId = x.RentTimeId,
                        StartAt = x.StartDate.Value.Date + x.StartTime.Value,
                        EndAt = x.EndDate.Value.Date + x.EndTime.Value,
                        Location = x.Location ?? "",
                        Area = x.Area ?? "",
                        PE = x.PE ?? "",
                        ProjectNo = x.ProjectNo ?? "",
                        BookingNo = x.BookingNo ?? "",
                        CustomerName = x.CustomerName ?? "",
                        ContactName = x.ContactName ?? "",
                        Phone = x.Phone ?? "",
                        Status = x.Status
                    })
                    .ToList();

                _detailById = _details
                    .Where(d => d.RentTimeId.HasValue)
                    .GroupBy(d => d.RentTimeId.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                _apptsAll = appts;
                _apptsMonth = BuildMonthSummaryAppointments(_apptsAll);

                // 2) 沒資料就刷新後結束
                if (_apptsAll.Count == 0)
                {
                    schedulerDataStorage1.Appointments.DataSource = null;
                    schedulerControl1.RefreshData();
                    return;
                }

                ApplyAppointForCurrentView();

                // 資料更新後，把畫面拉回使用者剛剛在看的日期
                if (_isWeek7Mode) schedulerControl1.Start = GetWeekStartMonday(keepStart);
                else schedulerControl1.Start = keepStart;

                // 若你剛好在月視圖，讓 _currentMonth 跟著現在顯示月份一致（避免上下月按鈕邏輯怪）
                if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                    SyncCurrentMonthFromScheduler();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "CalenndarView LoadData失敗");

                // 失敗時把畫面恢復到安全狀態（避免半套資料）
                try
                {
                    schedulerDataStorage1.Appointments.DataSource = null;
                    schedulerControl1.RefreshData();
                    HideDetailPanel();
                    ClearDetailPanel();
                }
                catch { }
            }
        }

        // ====== 外部可呼叫：取得/要求編輯 ======
        public List<CalendarRentTimeDetailItem> GetSelectedDetail()
        {
            try
            {
                var key = cmbBookingNo.EditValue?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                    return new List<CalendarRentTimeDetailItem>();

                if (!_pickByBookingNo.TryGetValue(key, out var d) || d == null)
                    return new List<CalendarRentTimeDetailItem>();

                if (!d.RentTimeId.HasValue)
                    return new List<CalendarRentTimeDetailItem>();

                return new List<CalendarRentTimeDetailItem> { d };
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "取得日曆選取項目失敗");
                return new List<CalendarRentTimeDetailItem>();
            }
        }

        public void RequestEditSelected()
        {
            try
            {
                var selected = GetSelectedDetail();

                if (selected.Count == 0 || !selected[0].RentTimeId.HasValue)
                {
                    XtraMessageBox.Show("請先選擇右側 BookingNo", "提示");
                    return;
                }

                EditRequested?.Invoke(selected[0].RentTimeId.Value);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "要求開啟編輯失敗");
            }
        }

        // ===== 視圖切換 =====
        private void btnViewDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                _isWeek7Mode = false;
                schedulerControl1.ActiveViewType = SchedulerViewType.Day;
                schedulerControl1.DayView.DayCount = 1;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換日視圖失敗");
            }
        }

        private void btnViewWeek_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                _isWeek7Mode = true;
                schedulerControl1.ActiveViewType = SchedulerViewType.Day;
                schedulerControl1.DayView.DayCount = 7;

                // 週一為第一天
                schedulerControl1.Start = GetWeekStartMonday(schedulerControl1.Start);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換週視圖失敗");
            }
        }

        private void btnViewMonth_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                _isWeek7Mode = false;
                schedulerControl1.ActiveViewType = SchedulerViewType.Month;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換月視圖失敗");
            }
        }

        private void btnViewTimeLine_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                schedulerControl1.ActiveViewType = SchedulerViewType.Timeline;

                var tv = schedulerControl1.TimelineView;

                // 清掉預設刻度，自己定義（比較可控）
                tv.Scales.Clear();

                // 上層刻度（可有可無）
                tv.Scales.Add(new TimeScaleMonth() { Width = 80 });
                tv.Scales.Add(new TimeScaleWeek() { Width = 60 });

                // 底層刻度：日，把 Width 調大 = 畫面顯示的天數就變少
                tv.Scales.Add(new TimeScaleDay() { Width = 120 });
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換時間軸失敗");
            }
        }

        // ===== 上一段 / 下一段 / 今天 =====
        private void btnPrevDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                GoToPreviousPeriod();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換上一段失敗");
            }
            ;
        }

        private void btnNextDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                GoToNextPeriod();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "切換下一段失敗");
            }
            ;
        }

        private void btnToday_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                {
                    schedulerControl1.Start = DateTime.Today;
                    return;
                }

                schedulerControl1.Start = _isWeek7Mode ?
                    GetWeekStartMonday(DateTime.Today)
                    : DateTime.Today;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "回到今天失敗");
            }
        }

        private void GoToPreviousPeriod()
        {
            switch (schedulerControl1.ActiveViewType)
            {
                case SchedulerViewType.Day:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(_isWeek7Mode ? -7 : -1);
                    break;
                case SchedulerViewType.Month:
                    _currentMonth = _currentMonth.AddMonths(-1);
                    schedulerControl1.Start = _currentMonth;
                    break;
                case SchedulerViewType.Timeline:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(-7);
                    break;
                default:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(-7);
                    break;
            }
        }

        private void GoToNextPeriod()
        {
            switch (schedulerControl1.ActiveViewType)
            {
                case SchedulerViewType.Day:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(_isWeek7Mode ? 7 : 1);
                    break;
                case SchedulerViewType.Month:
                    _currentMonth = _currentMonth.AddMonths(1);
                    schedulerControl1.Start = _currentMonth;
                    break;
                case SchedulerViewType.Timeline:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(7);
                    break;
                default:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(7);
                    break;
            }
        }

        // 設定一週第一天是星期一
        private static DateTime GetWeekStartMonday(DateTime date)
        {
            int offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-offset);
        }

        private void SyncCurrentMonthFromScheduler()
        {
            if (schedulerControl1.ActiveViewType != SchedulerViewType.Month) return;

            var anchor = schedulerControl1.Start.Date.AddDays(15);
            _currentMonth = new DateTime(anchor.Year, anchor.Month, 1);
        }

        // ===== 資料源套用 / 月視圖摘要 =====
        private void ApplyAppointForCurrentView()
        {
            if (_apptsAll == null || _apptsAll.Count == 0)
            {
                schedulerDataStorage1.Appointments.DataSource = null;
                schedulerControl1.RefreshData();
                return;
            }

            var isMonth = schedulerControl1.ActiveViewType == SchedulerViewType.Month;

            schedulerDataStorage1.Appointments.DataSource = isMonth ? _apptsMonth : _apptsAll;

            schedulerControl1.RefreshData();
        }

        private List<CalendarRentTimeItem> BuildMonthSummaryAppointments(List<CalendarRentTimeItem> all)
        {
            var result = new List<CalendarRentTimeItem>();

            foreach (var g in all.GroupBy(a => a.StartAt.Date).OrderBy(g => g.Key))
            {
                if (g.Count() == 1)
                {
                    result.Add(g.First());
                    continue;
                }

                var date = g.Key;
                var ids = g.Select(x => x.RentTimeId).ToList();

                var summaryId = -(date.Year * 10000 + date.Month * 100 + date.Day);

                result.Add(new CalendarRentTimeItem
                {
                    RentTimeId = summaryId,
                    StartAt = date,
                    EndAt = date.AddDays(1),
                    Subject = $"{ids.Count} 筆",
                    Location = "",
                    IsSummary = true,
                    RentTimeIds = string.Join(",", ids),
                    LabelId = 99
                });
            }
            return result;
        }

        // ===== 行事曆互動事件（點擊/切換/阻止內建表單）=====
        private async void schedulerControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            await SafeRunAsync(() =>
            {
                var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);

                if (hit.HitTest != SchedulerHitTest.AppointmentContent)
                {
                    HideDetailPanel();
                    return;
                }

                if (hit.ViewInfo is not AppointmentViewInfo appointmentViewInfo) return;

                var appt = appointmentViewInfo.Appointment;
                if (appt == null) return;

                ShowDetailPanel();
                PopulateBookingNoPicker(appt);
            }, "點選日曆失敗");
        }

        private async void schedulerControl1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            await SafeRunAsync(() =>
            {
                var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);
                if (hit.HitTest != SchedulerHitTest.AppointmentContent) return;
                if (hit.ViewInfo is not AppointmentViewInfo appointmentViewInfo) return;

                var appt = appointmentViewInfo.Appointment;
                if (appt == null) return;

                ShowDetailPanel();
                PopulateBookingNoPicker(appt);
                RequestEditSelected();
            }, "雙擊開啟編輯失敗");
        }

        private void schedulerControl1_VisibleIntervalChanged(object sender, EventArgs e)
        {
            try
            {
                if (_symcingStart) return;

                if (_isWeek7Mode && schedulerControl1.ActiveViewType == SchedulerViewType.Day && schedulerControl1.DayView.DayCount == 7)
                {
                    var monday = GetWeekStartMonday(schedulerControl1.Start);
                    if (schedulerControl1.Start.Date != monday.Date)
                    {
                        _symcingStart = true;
                        try { schedulerControl1.Start = monday; }
                        finally { _symcingStart = false; }
                    }
                }

                if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                {
                    SyncCurrentMonthFromScheduler();
                }

                ApplyAppointForCurrentView();
            }
            catch (Exception)
            {
                try
                {
                    HideDetailPanel();
                    ClearDetailPanel();
                }
                catch { }

                // XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "日曆刷新失敗");
            }
        }

        private void schedulerControl1_EditAppointmentFormShowing(object sender, AppointmentFormEventArgs e)
        {
            e.Handled = true;
        }

        // ===== 右側明細與下拉 =====
        private void PopulateBookingNoPicker(Appointment appt)
        {
            try
            {
                var candidates = GetCandidatesForPicker(appt);

                cmbBookingNo.Properties.Items.Clear();
                _pickByBookingNo.Clear();

                foreach (var d in candidates)
                {
                    cmbBookingNo.Properties.Items.Add(d.BookingNo);
                    _pickByBookingNo[d.BookingNo] = d;
                }

                if (candidates.Count > 0)
                {
                    var clickedId = 0;
                    _ = int.TryParse(appt.Id?.ToString(), out clickedId);

                    var idx = candidates.FindIndex(x => x.RentTimeId == clickedId);
                    cmbBookingNo.SelectedIndex = (idx >= 0) ? idx : 0;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "載入右側Booking清單失敗");
                try
                {
                    cmbBookingNo.Properties.Items.Clear();
                    _pickByBookingNo.Clear();
                    ClearDetailPanel();
                }
                catch { }
            }
        }

        private List<CalendarRentTimeDetailItem> GetCandidatesForPicker(Appointment appt)
        {
            try
            {
                var result = new List<CalendarRentTimeDetailItem>();

                var isSummary = appt.CustomFields["IsSummary"] as bool? == true;

                if (isSummary)
                {
                    var csv = appt.CustomFields["RentTimeIds"]?.ToString() ?? "";
                    var ids = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                                 .Where(id => id.HasValue)
                                 .Select(id => id!.Value)
                                 .ToList();

                    foreach (var id in ids)
                    {
                        if (_detailById.TryGetValue(id, out var d))
                            result.Add(d);
                    }

                    return result.OrderBy(x => x.StartAt).ToList();
                }

                var day = appt.Start.Date;
                result = _details
                    .Where(d => d.StartAt.Date == day)
                    .OrderBy(d => d.StartAt)
                    .ToList();

                return result;
            }
            catch
            {
                return new List<CalendarRentTimeDetailItem>();
            }
        }

        private void cmbBookingNo_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                var key = cmbBookingNo.EditValue?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    ClearDetailPanel();
                    return;
                }

                if (!_pickByBookingNo.TryGetValue(key, out var d) || d == null)
                {
                    ClearDetailPanel();
                    return;
                }

                ApplyDetailToUI(d);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "更新詳細資訊的BookingNo失敗");
                try { ClearDetailPanel(); }
                catch { }
            }
        }

        private void ApplyDetailToUI(CalendarRentTimeDetailItem d)
        {
            txtBookingNo.Text = d.BookingNo;
            txtCustomerName.Text = d.CustomerName;
            txtLocation.Text = d.Location;
            txtArea.Text = "(" + d.Area + ")";
            txtProjectNo.Text = d.ProjectNo;
            txtPhone.Text = d.Phone;
            txtContactName.Text = d.ContactName;
            txtPE.Text = d.PE;
            txtStartTime.Text = d.StartAt.ToString("HH:mm");
            txtEndTime.Text = d.EndAt.ToString("HH:mm");
        }

        private void ClearDetailPanel()
        {
            txtBookingNo.Text = "";
            txtCustomerName.Text = "";
            txtLocation.Text = "";
            txtArea.Text = "";
            txtProjectNo.Text = "";
            txtPhone.Text = "";
            txtContactName.Text = "";
            txtPE.Text = "";
            txtStartTime.Text = "";
            txtEndTime.Text = "";
        }

        private void HideDetailPanel()
        {
            splitContainerControl1.PanelVisibility = DevExpress.XtraEditors.SplitPanelVisibility.Panel1;
        }

        private void ShowDetailPanel()
        {
            splitContainerControl1.PanelVisibility = DevExpress.XtraEditors.SplitPanelVisibility.Both;
        }

        // ===== 拖曳 / Resize 規則與事件 =====
        public sealed class RentTimeMoveRequest
        {
            public int RentTimeId { get; set; }

            public DateTime NewStart { get; set; }

            public DateTime NewEnd { get; set; }

            public SchedulerViewType ViewType { get; init; }
        }

        private bool IsDraftAndNotSummary(Appointment apt)
        {
            var isSummary = apt.CustomFields["IsSummary"] as bool? == true;
            var status = apt.LabelId;
            return status == 0 && !isSummary;
        }

        private void schedulerControl1_AllowAppointmentDrag(object sender, AppointmentOperationEventArgs e)
        {
            if (e.Appointment == null)
            {
                e.Allow = false;
                return;
            }
            e.Allow = IsDraftAndNotSummary(e.Appointment);
        }

        private void schedulerControl1_AllowAppointmentResize(object sender, AppointmentOperationEventArgs e)
        {
            if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
            {
                e.Allow = false;
                return;
            }

            if (e.Appointment == null)
            {
                e.Allow = false;
                return;
            }

            e.Allow = IsDraftAndNotSummary(e.Appointment);
        }

        private void schedulerControl1_AppointmentDrop(object sender, AppointmentDragEventArgs e)
        {
            try
            {
                e.Allow = false;

                var src = e.SourceAppointment;
                var edited = e.EditedAppointment;

                if (src == null || edited == null) return;

                if (!IsDraftAndNotSummary(e.SourceAppointment)) return;

                if (PeriodChangeRequested == null)
                {
                    XtraMessageBox.Show("PeriodChangeRequested 尚未綁到 Form1", "提示");
                    return;
                }

                if (src.Id == null || !int.TryParse(src.Id.ToString(), out var rentTimeId))
                    return;

                DateTime newStart = e.EditedAppointment.Start;
                DateTime newEnd = e.EditedAppointment.End;

                if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                {
                    var targetDate = newStart.Date;

                    newStart = targetDate + src.Start.TimeOfDay;
                    newEnd = targetDate + src.End.TimeOfDay;

                    edited.Start = newStart;
                    edited.End = newEnd;
                }

                e.Allow = true;

                SafeBeginInvoke(async () =>
                {
                    // 這裡會回到 Form1 的 CalendarView_PeriodChangeRequestedAsync
                    await PeriodChangeRequested(rentTimeId, newStart, newEnd);
                }, "拖曳更新失敗");
            }
            catch (Exception ex)
            {
                e.Allow = false;
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "拖曳更新失敗");
            }
        }

        private void schedulerControl1_AppointmentResized(object sender, AppointmentResizeEventArgs e)
        {
            try
            {
                e.Allow = false;

                var src = e.SourceAppointment;
                var edited = e.EditedAppointment;

                if (src == null || edited == null) return;

                if (schedulerControl1.ActiveViewType == SchedulerViewType.Month) return;

                if (!IsDraftAndNotSummary(src)) return;

                if (PeriodChangeRequested == null)
                {
                    XtraMessageBox.Show("PeriodChangeRequested 尚未綁到 Form1", "提示");
                    return;
                }

                if (src.Id == null || !int.TryParse(src.Id.ToString(), out var rentTimeId))
                    return;

                DateTime newStart = src.Start;
                DateTime newEnd = edited.End;

                e.Allow = true;

                SafeBeginInvoke(async () =>
                {
                    await PeriodChangeRequested(rentTimeId, newStart, newEnd);
                }, "調整時間更新失敗");
            }
            catch (Exception ex)
            {
                e.Allow = false;
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "調整時間失敗");
            }
        }
    }
}
