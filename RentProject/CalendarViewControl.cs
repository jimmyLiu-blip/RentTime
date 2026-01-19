using DevExpress.XtraBars.Docking;
using DevExpress.XtraEditors;
using DevExpress.XtraRichEdit.Model;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Drawing;
using RentProject.Domain;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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
        // Action<int> 是一種委派（delegate）型別，代表「一個不回傳值的方法」，而且這個方法需要 1 個 int 參數
        // 只要有人訂閱我，就要提供一個方法；這個方法會吃一個 int（RentTimeId），不用回傳
        // 外面的人只能訂閱/取消訂閱（+= / -=)；外面的人不能直接觸發（Invoke）事件；只有在這個類別裡面才能 Invoke（發射事件）
        public event Action<int>? EditRequested;

        public event Func<int, DateTime, DateTime, bool>? PeriodChangeRequested;

        // 是否為「7天週時間表模式」
        private bool _isWeek7Mode = false;

        // 防止遞迴的旗標
        private bool _symcingStart = false;

        // ====== 建構 / 生命週期 ======
        public CalendarViewControl()
        {
            InitializeComponent();
        }

        private void CalendarViewControl_Load(object sender, EventArgs e)
        {
            EnsureSchedulerInit();

            HideDetailPanel();
            ClearDetailPanel();
        }

        // ====== Scheduler 初始化（只做一次） ======
        private void EnsureSchedulerInit()
        {
            if (_schedulerInited) return;

            // 1) 顯示模式與 UI 外觀
            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
            schedulerControl1.MonthView.DateTimeScrollbarVisible = true;
            schedulerControl1.DateNavigationBar.Visible = false;  // 關閉Calendar中內建的日期切換

            // 2) 綁 DataStorage（Scheduler 必要），schedulerDataStorage中常用：Appointments：行程/案件；Resources：資源/場地/機台；Labels / Statuses：顏色標籤、忙碌狀態
            schedulerControl1.DataStorage = schedulerDataStorage1;

            // 3) Appointment Mapping：告訴 Scheduler「欄位對應到哪個屬性」，常用還有
            // (1)Location：地點 (2)Description：備註/說明 (3)AllDay：是否整天 (4)Status：忙碌狀態（Free/Busy…） (5)Label：標籤顏色分類 (6)ResourceId：對應哪個 Resource (7)RecurrenceInfo：週期性（每週/每月重複）(8)ReminderInfo：提醒
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
                ApplyAppointForCurrentView();
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

        // 設定一週第一天是星期一
        private static DateTime GetWeekStartMonday(DateTime date)
        { 
            int offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday +7) % 7;  
            return date.Date.AddDays(-offset);
        }

        // ====== 載入資料（給外部呼叫） ======
        public void LoadData(List<RentTime> list)
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

            // 先記住使用者目前在看的日期
            var keepStart = schedulerControl1.Start;

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
                .ToDictionary(g => g.Key, g => g.First()); // g.First()這組裡的第一筆資料（例如 A）

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
                _currentMonth = new DateTime(schedulerControl1.Start.Year, schedulerControl1.Start.Month, 1);
        }

        // ===== 視圖切換 =====
        private void btnViewDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _isWeek7Mode = false;
            schedulerControl1.ActiveViewType = SchedulerViewType.Day;
            schedulerControl1.DayView.DayCount = 1;
        }

        private void btnViewWeek_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _isWeek7Mode = true;
            schedulerControl1.ActiveViewType = SchedulerViewType.Day;
            schedulerControl1.DayView.DayCount = 7;

            // 週一為第一天
            schedulerControl1.Start = GetWeekStartMonday(schedulerControl1.Start);
        }

        private void btnViewMonth_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _isWeek7Mode = false;
            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
        }

        private void btnViewTimeLine_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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

        // ===== 上一段 / 下一段 / 今天 =====
        private void btnPrevDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GoToPreviousPeriod();
        }

        private void btnNextDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            GoToNextPeriod();
        }

        private void btnToday_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
            {
                schedulerControl1.Start = DateTime.Today;
                return;
            }

            schedulerControl1.Start = _isWeek7Mode ? 
                GetWeekStartMonday(DateTime.Today)
                :DateTime.Today;
        }

        private void GoToPreviousPeriod()
        {
            switch (schedulerControl1.ActiveViewType)
            {
                case SchedulerViewType.Day:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(_isWeek7Mode ? -7:-1);
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

        // 依照你目前看的視圖（Month / Day / Week / Timeline），切換要顯示的資料來源
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

        // 把「同一天多筆案件」變成「一筆摘要」：
        private List<CalendarRentTimeItem> BuildMonthSummaryAppointments(List<CalendarRentTimeItem> all)
        {
            var result = new List<CalendarRentTimeItem>();

            // Key是分組的標籤
            foreach (var g in all.GroupBy(a => a.StartAt.Date).OrderBy(g => g.Key))
            {
                if (g.Count() == 1)
                {
                    // 把那筆直接放進月視圖清單
                    result.Add(g.First());
                    continue;
                }

                var date = g.Key;
                var ids = g.Select(x => x.RentTimeId).ToList();

                // 用負數當摘要ID（假設你資料庫 RentTimeId 都是正數）
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

        // 點到 appointment 就能進來，並抓到 Appointment 物件
        // MouseDown：滑鼠按鍵「按下」的瞬間 ； MouseClick（按下再放開，形成一次點擊）
        private void schedulerControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // hit：「滑鼠位置底下」到底是什麼 UI 元素（可能是 appointment、格子 cell、標題、More(+2)按鈕、時間刻度…等）
            // e.Location = 滑鼠點下去的座標
            // true：把 Appointment 當成透明的（你想知道「它底下的格子/區域是什麼」）
            // false：連 Appointment 本身也算進去（你想知道「我是不是點到某一筆單」）
            var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);

            // Month/Week/Day 點到 appointment 時，常見是 AppointmentContent 或 Appointment
            // HitTest 就像「點擊偵測結果」
            if (hit.HitTest != SchedulerHitTest.AppointmentContent)
            {
                    HideDetailPanel();
                    return;
            } 

            // 你滑鼠點到的那個元素，它的 ViewInfo 不是「AppointmentViewInfo」這一種型別 → 那就不能當 appointment 來處理，所以 return。
            if (hit.ViewInfo is not AppointmentViewInfo appointmentViewInfo) return;

            var appt = appointmentViewInfo.Appointment;
            if (appt == null) return;

            // 先用這行測試：確認真的點得到 appointment
            ShowDetailPanel();
            PopulateBookingNoPicker(appt);
        }

        private void schedulerControl1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);
            if (hit.HitTest != SchedulerHitTest.AppointmentContent) return;
            if (hit.ViewInfo is not AppointmentViewInfo appointmentViewInfo) return;

            var appt = appointmentViewInfo.Appointment;
            if (appt == null) return;

            // 先用這行測試：確認真的點得到 appointment
            ShowDetailPanel();
            PopulateBookingNoPicker(appt);
            // 然後直接要求編輯「目前選到的 BookingNo」
            RequestEditSelected();
        }

        private void schedulerControl1_VisibleIntervalChanged(object sender, EventArgs e)
        {
            if (_symcingStart) return;

            // 1.如果你現在是「7天週時間表」（DayView + DayCount=7）
            //    就把 Start 對齊到「週一」，避免滾輪後跑到週中導致你覺得案件不見
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

            // 2. 如果是月視圖：同步 _currentMonth，避免後面 Prev/Next 或其他邏輯用到舊月份
            if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
            { 
                _currentMonth = new DateTime(schedulerControl1.Start.Year, schedulerControl1.Start.Month, 1);
            }

            // 3. 重新套用資料源 + Refresh（解決「日期變了但畫面沒重畫」的感覺）
            ApplyAppointForCurrentView();
        }

        private void schedulerControl1_EditAppointmentFormShowing(object sender, AppointmentFormEventArgs e)
        {
            // 直接不讓 DevExpress 跳出內建 Appointment 編輯視窗
            // Handled = false（預設）：代表「事件沒有被你處理」→ 控制項會繼續跑它的預設行為
            // Handled = true：代表「我已經自己處理完了」→ 控制項就會停止預設行為
            e.Handled = true;
        }

        private void PopulateBookingNoPicker(Appointment appt)
        {
            // 1) 取得「這次點擊」要顯示的 BookingNo 清單
            var candidates = GetCandidatesForPicker(appt);

            // 2) 清空下拉與對照表
            cmbBookingNo.Properties.Items.Clear();
            _pickByBookingNo.Clear();

            // 3) 灌入下拉
            foreach (var d in candidates)
            {
                cmbBookingNo.Properties.Items.Add(d.BookingNo);
                _pickByBookingNo[d.BookingNo] = d;
            }

            // 4) 預選：優先選你點到的那一筆
            if (candidates.Count > 0)
            {
                var clickedId = 0;
                _ = int.TryParse(appt.Id?.ToString(), out clickedId);

                var idx = candidates.FindIndex(x => x.RentTimeId == clickedId);
                cmbBookingNo.SelectedIndex = (idx >= 0) ? idx : 0;
            }
        }

        // 把「這次要顯示在右側下拉的明細清單」找出來
        private List<CalendarRentTimeDetailItem> GetCandidatesForPicker(Appointment appt)
        {
            var result = new List<CalendarRentTimeDetailItem>();

            // 判斷是不是 Month 的「摘要」
            var isSummary = appt.CustomFields["IsSummary"] as bool? == true;

            if (isSummary)
            {
                // 摘要：從 RentTimeIds 把那一天的 id 全撈出來
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

            // 一般 appointment：顯示「同一天」全部 BookingNo
            var day = appt.Start.Date;
            result = _details
                .Where(d => d.StartAt.Date == day)
                .OrderBy(d => d.StartAt)
                .ToList();

            return result;
        }

        // cmbBookingNo_EditValueChanged
        private void cmbBookingNo_EditValueChanged(object sender, EventArgs e)
        {
            var key = cmbBookingNo.EditValue?.ToString()?.Trim();

            // 1. 下拉沒值：清空欄位
            if (string.IsNullOrWhiteSpace(key))
            {
                ClearDetailPanel();
                return;
            }

            // 2. 用BookingNo 找到對應值； !TryGetValue(...)：字典裡沒有這個 BookingNo
            if (!_pickByBookingNo.TryGetValue(key, out var d) || d == null)
            {
                ClearDetailPanel();
                return;
            }

            ApplyDetailToUI(d);
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

        // SplitPanel只顯示左側（行事曆）
        private void HideDetailPanel()
        {
            splitContainerControl1.PanelVisibility = DevExpress.XtraEditors.SplitPanelVisibility.Panel1;
        }

        // SplitPanel左右都顯示
        private void ShowDetailPanel()
        { 
            splitContainerControl1.PanelVisibility = DevExpress.XtraEditors.SplitPanelVisibility.Both;
        }

        // 取得目前選取編輯、刪除項
        public List<CalendarRentTimeDetailItem> GetSelectedDetail()
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

        public void RequestEditSelected()
        {
            var selected = GetSelectedDetail();

            if (selected.Count == 0 || !selected[0].RentTimeId.HasValue)
            { 
                XtraMessageBox.Show("請先選擇右側 BookingNo", "提示");
                return;
            }

            // Invoke(...) 就是「呼叫/執行」這個委派（或事件背後訂閱的方法清單）
            // 都 OK → 發射事件，把 RentTimeId 傳出去，讓外面（Form1）決定怎麼編輯
            // ?. 只跟 EditRequested 有關 => 有訂閱的人才呼叫；沒訂閱就什麼都不做，也不會爆
            EditRequested?.Invoke(selected[0].RentTimeId.Value);
        }

        // 在 CalendarViewControl 裡加事件（帶回成功/失敗）
        public sealed class RentTimeMoveRequest
        { 
            public int RentTimeId { get; set; }

            public DateTime NewStart { get; set; }

            public DateTime NewEnd { get; set; }

            public SchedulerViewType ViewType { get; init; }
        }

        // 加「判斷能不能拖/resize」的小工具
        private bool IsDraftAndNotSummary(Appointment apt)
        {
            var isSummary = apt.CustomFields["IsSummary"] as bool? == true;

            // LabelId 0=草稿, 1=租時中, 2=已完成, 3=已送出, 99=摘要
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
            // 規則B：月視圖一律禁止 resize
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
            // 預設允許拖拉（等下視情況否決）
            e.Allow = true;

            var src = e.SourceAppointment;
            var edited = e.EditedAppointment;

            // 核心:避免 NullReferenceException
            if(src == null || edited == null)
                return;

            // 非 Draft 或 Summary：禁止
            if (!IsDraftAndNotSummary(e.SourceAppointment))
            {
                return;
            }

            // 沒有訂閱者：也禁止（不然 UI 變了但 DB 不會變）
            if (PeriodChangeRequested == null)
            {
                XtraMessageBox.Show("PeriodChangeRequested 尚未綁到 Form1", "提示");
                return;
            }

            if (src.Id == null || !int.TryParse(src.Id.ToString(), out var rentTimeId))
                return;

            DateTime newStart = e.EditedAppointment.Start;
            DateTime newEnd = e.EditedAppointment.End;

            // 月視圖：只改日期，時間沿用原本
            if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
            {
                var targetDate = newStart.Date;

                newStart = targetDate + src.Start.TimeOfDay;
                newEnd = targetDate + src.End.TimeOfDay;

                // 很重要：把修正後的時間寫回 EditedAppointment
                edited.Start = newStart;
                edited.End = newEnd;
            }

            bool ok;
            try
            {
                ok = PeriodChangeRequested(rentTimeId, newStart, newEnd);
            }
            catch (Exception ex) 
            { 
                XtraMessageBox.Show($"更新失敗：{ex.Message}", "錯誤");
                ok = false;
            }

            e.Allow = ok;
        }

        private void schedulerControl1_AppointmentResized(object sender, AppointmentResizeEventArgs e)
        {
            e.Allow = false;

            var src = e.SourceAppointment;
            var edited = e.EditedAppointment;

            if (src == null || edited == null)
                return;

            if (schedulerControl1.ActiveViewType == SchedulerViewType.Month)
                return;

            // 非 Draft 或 Summary：禁止
            if (!IsDraftAndNotSummary(src))
            {
                return;
            }

            // 沒有訂閱者：禁止
            if (PeriodChangeRequested == null)
            {
                XtraMessageBox.Show("PeriodChangeRequested 尚未綁到 Form1", "提示");
                return;
            }

            if (src.Id == null || !int.TryParse(src.Id.ToString(), out var rentTimeId))
                return;

            DateTime newStart = src.Start;
            DateTime newEnd = edited.End;

            bool ok;
            try
            {
                ok = PeriodChangeRequested(rentTimeId, newStart, newEnd);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"更新失敗：{ex.Message}", "錯誤");
                ok = false;
            }

            e.Allow = ok;
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
    }
}
