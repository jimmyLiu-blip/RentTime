using DevExpress.XtraEditors;
using DevExpress.XtraScheduler;
using RentProject.Domain;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraScheduler.Drawing;

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



        // ====== 建構 / 生命週期 ======
        public CalendarViewControl()
        {
            InitializeComponent();
        }

        private void CalendarViewControl_Load(object sender, EventArgs e)
        {
            EnsureSchedulerInit();
        }

        // ====== Scheduler 初始化（只做一次） ======
        private void EnsureSchedulerInit()
        {
            if (_schedulerInited) return;

            // 1) 顯示模式與 UI 外觀
            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
            schedulerControl1.MonthView.DateTimeScrollbarVisible = false; 
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

            schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                new AppointmentCustomFieldMapping("IsSummary", nameof(CalendarRentTimeItem.IsSummary)));
            schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                new AppointmentCustomFieldMapping("RentTimeIds", nameof(CalendarRentTimeItem.RentTimeIds)));

            // 5) 月曆上的 appointment 顯示設定
            schedulerControl1.MonthView.AppointmentDisplayOptions.AppointmentAutoHeight =true;
            schedulerControl1.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.MonthView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;

            // 6) 預設顯示當月
            var today = DateTime.Today;
            _currentMonth = new DateTime(today.Year, today.Month, 1);
            schedulerControl1.Start = _currentMonth;

            schedulerControl1.ActiveViewChanged += (s, e) =>
            {
                ApplyAppointForCurrentView();
            };

            schedulerControl1.MouseDown -= schedulerControl1_MouseDown;
            schedulerControl1.MouseDown += schedulerControl1_MouseDown;

            _schedulerInited = true;
        }

        // ====== 載入資料（給外部呼叫） ======
        public void LoadData(List<RentTime> list)
        {
            EnsureSchedulerInit();

            // 1) 轉成 Scheduler 要吃的 Appointment DataSource
            var appts = list
                .Where(x => x.StartDate != null && x.EndDate != null && x.StartTime != null && x.EndTime != null)
                .Select(x => new CalendarRentTimeItem
                {
                    RentTimeId = x.RentTimeId,
                    StartAt = x.StartDate.Value.Date + x.StartTime.Value,
                    EndAt = x.EndDate.Value.Date + x.EndTime.Value,
                    Subject = $"{x.BookingNo}\r\n{x.CustomerName}",
                    Location = x.Location ?? ""
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
                    Phone = x.Phone ?? ""
                })
                .ToList();

            _detailById = _details
                .Where(d => d.RentTimeId.HasValue)
                .ToDictionary(d => d.RentTimeId.Value, d => d);

            _apptsAll = appts;
            _apptsMonth = BuildMonthSummaryAppointments(_apptsAll);

            // 2) 沒資料就刷新後結束
            if (_apptsAll.Count == 0)
            {
                schedulerDataStorage1.Appointments.DataSource = null;
                schedulerControl1.RefreshData();
                return;
            }

            // 5) 自動跳到第一筆資料的月份（避免你停在別的月份以為沒資料）
            var minStart = _apptsAll.Min(a => a.StartAt);
            _currentMonth = new DateTime(minStart.Year, minStart.Month, 1);
            schedulerControl1.Start = _currentMonth;

            ApplyAppointForCurrentView();
        }


        // ===== 視圖切換 =====
        private void btnViewDay_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            schedulerControl1.ActiveViewType = SchedulerViewType.Day;
        }

        private void btnViewWeek_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            schedulerControl1.ActiveViewType = SchedulerViewType.Week;
        }

        private void btnViewMonth_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
        }

        private void btnViewTimeLine_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            schedulerControl1.ActiveViewType = SchedulerViewType.Timeline;
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
            schedulerControl1.Start = DateTime.Today;
        }

        private void GoToPreviousPeriod()
        {
            switch (schedulerControl1.ActiveViewType)
            {
                case SchedulerViewType.Day:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(-1);
                    break;
                case SchedulerViewType.Week:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(-7);
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
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(1);
                    break;
                case SchedulerViewType.Week:
                    schedulerControl1.Start = schedulerControl1.Start.AddDays(7);
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
                    RentTimeIds = string.Join(",", ids)
                });
            }
            return result;
        }

        // 點到 appointment 就能進來，並抓到 Appointment 物件
        private void schedulerControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);

            // Month/Week/Day 點到 appointment 時，常見是 AppointmentContent 或 Appointment
            if (hit.HitTest != SchedulerHitTest.AppointmentContent) return;

            if (hit.ViewInfo is not AppointmentViewInfo appointmentViewInfo) return;

            var appt = appointmentViewInfo.Appointment;
            if (appt == null) return;

            // 先用這行測試：確認真的點得到 appointment
            XtraMessageBox.Show($"你點到：{appt.Subject}");
        }
    }
}
