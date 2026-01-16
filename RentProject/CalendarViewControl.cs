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
    public partial class CalendarViewControl : XtraUserControl
    {
        #region ====== 狀態欄位（State） ======
        private DateTime _currentMonth; // 永遠存「當月 1 號」

        // 詳細資料（給右側 memoDetail 顯示用）
        private List<CalendarRentTimeDetailItem> _detailList = new();

        // 快速查詢用索引（Index）
        private Dictionary<DateTime, List<CalendarRentTimeDetailItem>> _detailByDate = new();
        private Dictionary<int, CalendarRentTimeDetailItem> _detailById = new();

        // 防止 Scheduler 重複初始化
        private bool _schedulerInited = false;
        #endregion

        #region ====== 建構 / 生命週期 ======
        public CalendarViewControl()
        {
            InitializeComponent();
        }

        private void CalendarViewControl_Load(object sender, EventArgs e)
        {
            EnsureSchedulerInit();
        }
        #endregion

        #region ====== 月份切換（UI：上一個月 / 下一個月） ======
        private void btnPrevMonth_Click(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            schedulerControl1.Start = _currentMonth;
        }

        private void btnNextMonth_Click(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            schedulerControl1.Start = _currentMonth;
        }

        #endregion

        #region ====== Scheduler 初始化（只做一次） ======
        private void EnsureSchedulerInit()
        {
            if (_schedulerInited) return;

            // 1) 顯示模式與 UI 外觀
            schedulerControl1.ActiveViewType = SchedulerViewType.Month;
            //schedulerControl1.MonthView.DateTimeScrollbarVisible = false;
            //schedulerControl1.DateNavigationBar.Visible = false;

            // 2) 綁 DataStorage（Scheduler 必要）
            schedulerControl1.DataStorage = schedulerDataStorage1;

            // 3) Appointment Mapping：告訴 Scheduler「欄位對應到哪個屬性」
            schedulerDataStorage1.Appointments.Mappings.AppointmentId = nameof(CalendarRentTimeItem.RentTimeId);
            schedulerDataStorage1.Appointments.Mappings.Start = nameof(CalendarRentTimeItem.StartAt);
            schedulerDataStorage1.Appointments.Mappings.End = nameof(CalendarRentTimeItem.EndAt);
            schedulerDataStorage1.Appointments.Mappings.Subject = nameof(CalendarRentTimeItem.Subject);

            // 4) 自訂欄位 Mapping（Custom Field）
            schedulerDataStorage1.Appointments.CustomFieldMappings.Clear();
            schedulerDataStorage1.Appointments.CustomFieldMappings.Add(
                new AppointmentCustomFieldMapping("Location", nameof(CalendarRentTimeItem.Location))
            );

            // 5) 月曆上的 appointment 顯示設定
            schedulerControl1.MonthView.AppointmentDisplayOptions.AppointmentAutoHeight = true;
            schedulerControl1.MonthView.AppointmentDisplayOptions.StartTimeVisibility = AppointmentTimeVisibility.Never;
            schedulerControl1.MonthView.AppointmentDisplayOptions.EndTimeVisibility = AppointmentTimeVisibility.Never;

            // 6) 預設顯示當月
            var today = DateTime.Today;
            _currentMonth = new DateTime(today.Year, today.Month, 1);
            schedulerControl1.Start = _currentMonth;

            _schedulerInited = true;
        }
        #endregion

        #region ====== 載入資料（外部呼叫） ======
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
                    Subject = $"{x.BookingNo}\r\n{x.CustomerName}\r\n{x.TestItem}",
                    Location = x.Location ?? ""
                })
                .ToList();

            // 2) 先清空，避免殘留舊資料
            schedulerDataStorage1.Appointments.DataSource = null;

            // 3) 沒資料就刷新後結束
            if (appts.Count == 0)
            {
                schedulerControl1.RefreshData();
                return;
            }

            // 4) 綁回資料
            schedulerDataStorage1.Appointments.DataSource = appts;

            // 5) 自動跳到第一筆資料的月份（避免你停在別的月份以為沒資料）
            var minStart = appts.Min(a => a.StartAt);
            _currentMonth = new DateTime(minStart.Year, minStart.Month, 1);
            schedulerControl1.Start = _currentMonth;

            // 6) 正確刷新 Scheduler 資料
            schedulerControl1.RefreshData();

            // 7) 建立右側詳細資訊用的資料與索引
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
        #endregion

        #region ====== 索引建立（讓查詢更快） ======
        private void BuildDetailIndex()
        {
            // 依日期分組：如果你之後想「點日期顯示當天所有租時單」會很好用
            _detailByDate = _detailList
                .GroupBy(x => x.StartAt.Date)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.StartAt).ToList());

            // 依 RentTimeId 建索引：點某個 appointment 時能 O(1) 查到 detail
            _detailById = _detailList.ToDictionary(x => x.RentTimeId, x => x);
        }
        #endregion

        #region ====== 詳細資訊顯示（右側 memo） ======
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
        #endregion

        #region ====== 使用者互動（點月曆 appointment 顯示詳細） ======
        private void schedulerControl1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // 1) 計算點到哪個區塊（格子/appointment/空白）
            var hit = schedulerControl1.ActiveView.ViewInfo.CalcHitInfo(e.Location, false);

            // 2) 只處理點到 appointment 的情況
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

            // 點到空白就清除詳細
            ShowDetail(null);
        }

        #endregion
    }
}
