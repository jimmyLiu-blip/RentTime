using System;
using System.Linq;
using DevExpress.XtraEditors;

namespace RentProject
{
    public partial class Project : XtraForm
    {
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

        // =========================================================
        // 晚餐分鐘顯示文字：xx 分
        // =========================================================
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

        // =========================================================
        // 預估時間計算 + 集中刷新
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

        private void RefreshMealAndEstimateUI()
        {
            ApplyMealEnableByEndTime();
            UpdateEstimatedUI();
        }
    }
}
