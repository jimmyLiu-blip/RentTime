using DevExpress.XtraEditors;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Drawing;
using RentProject.Domain;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RentProject
{
    public partial class CalendarViewControl : DevExpress.XtraEditors.XtraUserControl
    {
        private DateTime _currentMonth; // 永遠存「當月 1 號」
        private List<CalendarRentTimeDetailItem> _detailList = new();
        private Dictionary<DateTime, List<CalendarRentTimeDetailItem>> _detailByDate = new();
        private Dictionary<int, CalendarRentTimeDetailItem> _detailById = new();

        private bool _schedulerInited = false;

        public CalendarViewControl()
        {
            InitializeComponent();
        }

        private void CalendarViewControl_Load(object sender, EventArgs e)
        {
            EnsureSchedulerInit();
        }

        private void btnPrevMonth_Click(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            schedulerControl1.Start = _currentMonth;
            UpdateMonthTitle();
        }

        private void btnNextMonth_Click(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            schedulerControl1.Start = _currentMonth;
            UpdateMonthTitle();
        }

        private void UpdateMonthTitle()
        {
            lblMonthTitle.Text = $"{_currentMonth.Year}年{_currentMonth.Month}月";
        }

        private void EnsureSchedulerInit()
        {
            if (_schedulerInited) return;

            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
            schedulerControl1.MonthView.DateTimeScrollbarVisible = false;
            schedulerControl1.DateNavigationBar.Visible = false;

            // 綁 DataStorage（很重要）
            schedulerControl1.DataStorage = schedulerDataStorage1;

            // Mapping
            schedulerDataStorage1.Appointments.Mappings.AppointmentId = nameof(CalendarRentTimeItem.RentTimeId);
            schedulerDataStorage1.Appointments.Mappings.Start = nameof(CalendarRentTimeItem.StartAt);
            schedulerDataStorage1.Appointments.Mappings.End = nameof(CalendarRentTimeItem.EndAt);
            schedulerDataStorage1.Appointments.Mappings.Subject = nameof(CalendarRentTimeItem.Subject);

            schedulerDataStorage1.Appointments.CustomFieldMappings.Clear();
            schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                new AppointmentCustomFieldMapping("Location", nameof(CalendarRentTimeItem.Location))
            );

            // 顯示設定
            schedulerControl1.MonthView.AppointmentDisplayOptions.AppointmentAutoHeight = true;
            schedulerControl1.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.MonthView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;

            // 預設月份：先給當月
            var today = DateTime.Today;
            _currentMonth = new DateTime(today.Year, today.Month, 1);
            schedulerControl1.Start = _currentMonth;
            UpdateMonthTitle();

            _schedulerInited = true;
        }


        /*
        private void LoadDemoAppointments()
        {
            var list = new List<CalendarRentTimeItem>
            {
                new CalendarRentTimeItem
                {
                    RentTimeId = 1,
                    StartAt = _currentMonth.AddDays(2).AddHours(10),  // 當月第3天 10:00
                    EndAt   = _currentMonth.AddDays(2).AddHours(15),  // 當月第3天 15:00
                    Subject = "TE251130001\r\n好厲害科技\r\nConducted 2",
                    Location = "SAC D"
                },
                new CalendarRentTimeItem
                {
                    RentTimeId = 2,
                    StartAt = _currentMonth.AddDays(9).AddHours(9),
                    EndAt   = _currentMonth.AddDays(9).AddHours(12),
                    Subject = "TE251125001\r\n好厲害科技\r\nConducted 1",
                    Location = "Conducted 1"
                }
            };

            schedulerDataStorage1.Appointments.DataSource = list;

            // 保險：強制刷新一次畫面
            schedulerControl1.Refresh();
        }
        */

        /*
        private void LoadDemoDetails()
        {
            _detailList = new List<CalendarRentTimeDetailItem>
            {
                new CalendarRentTimeDetailItem
                {
                    RentTimeId = 1,
                    BookingNo = "TE251130001",
                    StartAt = _currentMonth.AddDays(2).AddHours(10),
                    EndAt   = _currentMonth.AddDays(2).AddHours(15),

                    ProjectNo = "TE251130001",
                    ProjectName = "專案A",
                    CustomerName = "好厲害科技",
                    ContactName = "王小明",
                    Phone = "0800-080-128",
                    PE = "Martin_Liu",
                    Location = "SAC D",
                    TestItem = "Conducted 2",
                    Notes = "Demo備註A"
                },
                new CalendarRentTimeDetailItem
                {
                    RentTimeId = 2,
                    BookingNo = "TE251125001",
                    StartAt = _currentMonth.AddDays(9).AddHours(9),
                    EndAt   = _currentMonth.AddDays(9).AddHours(12),

                    ProjectNo = "TE251125001",
                    ProjectName = "專案B",
                    CustomerName = "好厲害科技",
                    ContactName = "王小明",
                    Phone = "0800-080-128",
                    PE = "Martin_Liu",
                    Location = "Conducted 1",
                    TestItem = "Conducted 1",
                    Notes = "Demo備註B"
                }
            };

            BuildDetailIndex();
        }
        */

        public void LoadData(List<RentTime> list)
        {
            EnsureSchedulerInit();

            var appts = list
                .Where(x => x.StartDate != null && x.EndDate != null && x.StartTime != null && x.EndTime != null)
                .Select(x => new CalendarRentTimeItem
                {
                    RentTimeId = x.RentTimeId,
                    StartAt = x.StartDate.Value.Date + x.StartTime.Value,
                    EndAt = x.EndDate.Value.Date + x.EndTime.Value,
                    Subject = $"{x.BookingNo}\r\n{x.CustomerName}\r\n{x.TestItem}",
                    Location = x.Location ?? ""
                })
                .ToList();

            // 若沒資料：清空畫面
            schedulerDataStorage1.Appointments.DataSource = null;

            if (appts.Count == 0)
            {
                schedulerControl1.RefreshData();
                return;
            }

            schedulerDataStorage1.Appointments.DataSource = appts;

            // 自動跳到「第一筆資料」所在月份，避免你以為沒資料
            var minStart = appts.Min(a => a.StartAt);
            _currentMonth = new DateTime(minStart.Year, minStart.Month, 1);
            schedulerControl1.Start = _currentMonth;
            UpdateMonthTitle();

            // 正確刷新資料（比 Refresh() 更對）
            schedulerControl1.RefreshData();

            // detail（你原本那段保留）
            _detailList = list
                .Where(x => x.StartDate != null && x.EndDate != null)
                .Select(x => new CalendarRentTimeDetailItem
                {
                    RentTimeId = x.RentTimeId,
                    BookingNo = x.BookingNo ?? "",
                    StartAt = x.StartDate.Value.Date + x.StartTime.Value,
                    EndAt = x.EndDate.Value.Date + x.EndTime.Value,

                    ProjectNo = x.ProjectNo ?? "",
                    ProjectName = x.ProjectName ?? "",
                    CustomerName = x.CustomerName ?? "",
                    ContactName = x.ContactName ?? "",
                    Phone = x.Phone ?? "",
                    PE = x.PE ?? "",
                    Location = x.Location ?? "",
                    TestItem = x.TestItem ?? "",
                    Notes = x.Notes ?? "",
                    Area = x.Area ?? ""
                })
                .ToList();

            BuildDetailIndex();
        }

        private void BuildDetailIndex()
        {
            _detailByDate = _detailList
                .GroupBy(x => x.StartAt.Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartAt).ToList());

            _detailById = _detailList.ToDictionary(x => x.RentTimeId, x => x);
        }


        private void ShowDetail(CalendarRentTimeDetailItem? d)
        {
            if (d == null)
            {
                memoDetail.Text = "";
                return;
            }

            memoDetail.Text =
                $"BookingNo：{d.BookingNo}\r\n" +
                $"{d.CustomerName}\r\n" +
                $"{d.StartAt:HH:mm} - {d.EndAt:HH:mm}\r\n" +
                $"({d.Area}) - {d.Location}\r\n" +

                $"\r\nProject No:\r\n" +
                $"{d.ProjectNo}\r\n" +

                $"\r\n客戶聯絡資訊：\r\n" +
                $"{d.ContactName} ({d.Phone})\r\n" +

                $"\r\nPE：{d.PE}";
        }


        private void schedulerControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // 1) 算你滑鼠點到哪個區塊（格子/appointment/空白）
            var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);

            // 2) 只處理你點到 appointment 的情況
            if (hit.HitTest == SchedulerHitTest.AppointmentContent)
            {
                var apptViewInfo = hit.ViewInfo as AppointmentViewInfo;
                var appt = apptViewInfo?.Appointment;

                if (appt?.Id == null)
                {
                    ShowDetail(null);
                    return;
                }

                if (int.TryParse(appt.Id.ToString(), out var id) && _detailById.TryGetValue(id, out var detail))
                    ShowDetail(detail);
                else
                    ShowDetail(null);

                return;
            }

            ShowDetail(null);
        }
    }
}
