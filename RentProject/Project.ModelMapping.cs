using System;
using System.Linq;
using DevExpress.XtraEditors;
using RentProject.Domain;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // J) UI <-> Model：組 Model / 回填 UI
        // =========================================================
        private RentTime BuildModelFormUI()
        {
            int dinnerMin = cmbDinnerMinutes.EditValue is int v ? v : 0;

            var jobNo = cmbJobNo.Text?.Trim();
            int? jobId = null;

            if (!string.IsNullOrWhiteSpace(jobNo) && string.Equals(jobNo, _currentJobNo, StringComparison.OrdinalIgnoreCase))
            {
                jobId = _currentJobId;
            }

            var uiStart = GetUiStartDateTime();
            var uiEnd = GetUiEndDateTime();

            var model = new RentTime
            {
                BookingNo = GetBookingNoFromUI(),   // 補這行

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
            };

            // 時間欄位：依狀態分流
            // Draft：用 UI 寫「預排」欄位
            if (_uiStatus == UiRentStatus.Draft || _loadedRentTime == null)
            {
                model.StartDate = startDateEdit.EditValue as DateTime?;
                model.EndDate = endDateEdit.EditValue as DateTime?;
                model.StartTime = startTimeEdit.EditValue is DateTime t1 ? t1.TimeOfDay : (TimeSpan?)null;
                model.EndTime = endTimeEdit.EditValue is DateTime t2 ? t2.TimeOfDay : (TimeSpan?)null;

                // Actual 先沿用 DB（通常是 null）
                model.ActualStartAt = _loadedRentTime?.ActualStartAt;
                model.ActualEndAt = _loadedRentTime?.ActualEndAt;
            }
            else
            {
                // Started/Finished：預排沿用 DB，不讓 UI 改壞
                model.StartDate = _loadedRentTime.StartDate;
                model.EndDate = _loadedRentTime.EndDate;
                model.StartTime = _loadedRentTime.StartTime;
                model.EndTime = _loadedRentTime.EndTime;

                // Actual 用 UI
                model.ActualStartAt = uiStart ?? _loadedRentTime.ActualStartAt;
                model.ActualEndAt = uiEnd ?? _loadedRentTime.ActualEndAt; ;
            }

            return model;
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
                cmbEngineer.Text = string.IsNullOrWhiteSpace(data.EngineerName) ? "" : data.EngineerName;
                txtSampleModel.Text = data.SampleModel ?? "";
                txtSampleNo.Text = data.SampleNo ?? "";
                cmbTestMode.Text = data.TestMode ?? "";
                cmbTestItem.Text = data.TestItem ?? "";
                memoNote.Text = data.Notes ?? "";

                // ===== 顯示用時間：有 Actual 就顯示 Actual，沒有就顯示預排 =====
                var plannedStart = Combine(data.StartDate, data.StartTime);
                var plannedEnd = Combine(data.EndDate, data.EndTime);

                DateTime? displayStart;
                DateTime? displayEnd;

                if (data.Status == 0) // Draft:顯示預排
                {
                    displayStart = plannedStart;
                    displayEnd = plannedEnd;
                }
                else if (data.Status == 1) //Started：Start 顯示實際，End 先空白(除非已填實際)
                {
                    displayStart = data.ActualStartAt ?? plannedStart;
                    displayEnd = data.ActualEndAt ?? plannedEnd;
                }
                else // Finished：顯示實際（沒有就退回預排）
                {
                    displayStart = data.ActualStartAt ?? plannedStart;
                    displayEnd = data.ActualEndAt ?? plannedEnd;
                }

                // 日期
                startDateEdit.EditValue = displayStart?.Date;
                endDateEdit.EditValue = displayEnd?.Date;

                // 時間：TimeEdit 的 EditValue 通常要 DateTime
                // startTimeEdit.EditValue = displayStart.HasValue ? DateTime.Today.Add(displayStart.Value.TimeOfDay) : null;
                // endTimeEdit.EditValue = displayEnd.HasValue ? DateTime.Today.Add(displayEnd.Value.TimeOfDay) : null;
                // 時間：用 Time 屬性
                if (displayStart.HasValue)
                {
                    startTimeEdit.Time = new DateTime(1900, 1, 1, displayStart.Value.Hour, displayStart.Value.Minute, 0);
                }
                else
                {
                    startTimeEdit.EditValue = null;
                }

                if (displayEnd.HasValue)
                {
                    endTimeEdit.Time = new DateTime(1900, 1, 1, displayEnd.Value.Hour, displayEnd.Value.Minute, 0);
                }
                else
                {
                    endTimeEdit.EditValue = null;
                }
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

        private string? GetBookingNoFromUI()
        {
            var main = txtBookingNo.Text?.Trim();   // 例如 "TMP-0000123"
            var seq = txtBookingSeq.Text?.Trim();  // 例如 "1"

            if (string.IsNullOrWhiteSpace(main)) return null;

            // 沒有流水號就只回主號（但你的情況通常會有）
            if (string.IsNullOrWhiteSpace(seq)) return main;

            return $"{main}-{seq}"; // => "TMP-0000123-1"
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

        // 組合預排的 Date+Time
        private static DateTime? Combine(DateTime? date, TimeSpan? time)
        {
            if (date is null || time is null) return null;
            return date.Value.Date + time.Value;
        }

        private DateTime? GetUiStartDateTime()
        {
            var d = startDateEdit.EditValue as DateTime?;
            var t = startTimeEdit.EditValue is DateTime dt ? dt.TimeOfDay : (TimeSpan?)null;
            return Combine(d, t);
        }

        private DateTime? GetUiEndDateTime()
        {
            var d = endDateEdit.EditValue as DateTime?;
            var t = endTimeEdit.EditValue is DateTime dt ? dt.TimeOfDay : (TimeSpan?)null;
            return Combine(d, t);
        }
    }
}
