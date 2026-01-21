using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Mask;
using System;
using System.Windows.Forms;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // 設定 DateEdit：讓日期可以正常連續輸入
        private void ConfigureDateEdit(DateEdit dateEdit)
        {
            dateEdit.Properties.Mask.MaskType = MaskType.None;
            dateEdit.Properties.DisplayFormat.FormatString = "yyyy/M/d";
            dateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit.Properties.EditFormat.FormatString = "yyyy/M/d";
            dateEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            dateEdit.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

            dateEdit.Leave -= DateEdit_Leave;
            dateEdit.Leave += DateEdit_Leave;

            // 改用 Click 事件（更可靠）
            dateEdit.Click -= DateTimeEdit_Click;
            dateEdit.Click += DateTimeEdit_Click;
        }

        private void DateEdit_Leave(object sender, EventArgs e)
        {
            if (sender is not DateEdit dateEdit) return;

            var input = dateEdit.Text?.Trim().Replace("/", "").Replace("-", "").Replace(" ", "") ?? "";
            if (string.IsNullOrWhiteSpace(input)) return;

            // 處理 8 位數字：20260105 → 2026/1/5
            if (input.Length == 8 && int.TryParse(input, out _))
            {
                var year = input.Substring(0, 4);
                var month = input.Substring(4, 2);
                var day = input.Substring(6, 2);

                if (DateTime.TryParse($"{year}/{month}/{day}", out DateTime parsed))
                {
                    dateEdit.EditValue = parsed;
                    dateEdit.Text = parsed.ToString("yyyy/M/d"); // 強制更新顯示
                    return;
                }
            }

            // 處理 6 位數字：260105 → 2026/1/5（假設 20xx 年）
            if (input.Length == 6 && int.TryParse(input, out _))
            {
                var year = "20" + input.Substring(0, 2);
                var month = input.Substring(2, 2);
                var day = input.Substring(4, 2);

                if (DateTime.TryParse($"{year}/{month}/{day}", out DateTime parsed))
                {
                    dateEdit.EditValue = parsed;
                    dateEdit.Text = parsed.ToString("yyyy/M/d");
                    return;
                }
            }

            // 處理一般格式：2026/1/5 或 2026-1-5
            if (DateTime.TryParse(input, out DateTime result))
            {
                dateEdit.EditValue = result;
                dateEdit.Text = result.ToString("yyyy/M/d");
            }
        }

        private void ConfigureTimeEdit(TimeEdit timeEdit)
        {
            timeEdit.KeyPress -= TimeEdit_KeyPress;
            timeEdit.KeyPress += TimeEdit_KeyPress;

            timeEdit.Leave -= TimeEdit_Leave;
            timeEdit.Leave += TimeEdit_Leave;

            timeEdit.Properties.Spin -= TimeEdit_Spin;
            timeEdit.Properties.Spin += TimeEdit_Spin;

            // 改用 Click 事件（更可靠）
            timeEdit.Click -= DateTimeEdit_Click;
            timeEdit.Click += DateTimeEdit_Click;
        }

        private void TimeEdit_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 只允許數字和冒號
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ':' && e.KeyChar != '\b')
            {
                e.Handled = true;
            }
        }

        private void TimeEdit_Leave(object sender, EventArgs e)
        {
            if (sender is not TimeEdit timeEdit) return;

            var input = timeEdit.Text?.Trim().Replace("_", "").Replace(" ", "") ?? "";
            if (string.IsNullOrWhiteSpace(input)) return;

            DateTime? parsed = null;

            // 純數字當小時：12 → 12:00
            if (!input.Contains(":") && int.TryParse(input, out int h) && h >= 0 && h <= 23)
            {
                parsed = new DateTime(1900, 1, 1, h, 0, 0);  // ← 改用固定日期
            }
            // HH:mm 格式：12:30
            else if (input.Contains(":"))
            {
                var parts = input.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int hour) &&
                    int.TryParse(parts[1], out int min) &&
                    hour >= 0 && hour <= 23 && min >= 0 && min <= 59)
                {
                    parsed = new DateTime(1900, 1, 1, hour, min, 0);  // ← 改用固定日期
                }
            }
            // 處理 4 位數字：1230 → 12:30
            else if (input.Length == 4 && int.TryParse(input, out int hhmm))
            {
                int hour = hhmm / 100;
                int min = hhmm % 100;

                if (hour >= 0 && hour <= 23 && min >= 0 && min <= 59)
                {
                    parsed = new DateTime(1900, 1, 1, hour, min, 0);  // ← 改用固定日期
                }
            }
            // 處理 3 位數字：930 → 09:30
            else if (input.Length == 3 && int.TryParse(input, out int hmm))
            {
                int hour = hmm / 100;
                int min = hmm % 100;

                if (hour >= 0 && hour <= 23 && min >= 0 && min <= 59)
                {
                    parsed = new DateTime(1900, 1, 1, hour, min, 0);  // ← 改用固定日期
                }
            }

            // 強制更新 - 改用 Time 屬性
            if (parsed.HasValue)
            {
                timeEdit.Time = parsed.Value;  // ← 改用 Time 屬性，不用清空
            }
        }

        private void TimeEdit_Spin(object sender, SpinEventArgs e)
        {
            if (sender is not TimeEdit timeEdit) return;

            // 取得目前時間（如果是空的，預設 00:00）
            DateTime current;
            try
            {
                current = timeEdit.Time;
            }
            catch
            {
                // 如果 Time 是 null 或無效，設為 00:00
                current = new DateTime(1900, 1, 1, 0, 0, 0);
                timeEdit.Time = current;
                e.Handled = true;
                return;
            }

            // 上下調整（每次 30 分鐘）
            if (e.IsSpinUp)
            {
                current = current.AddMinutes(30);

                // 超過 23:59 就停在 23:59
                if (current.Hour > 23 || (current.Hour == 23 && current.Minute > 59))
                {
                    current = new DateTime(1900, 1, 1, 23, 30, 0); // 最大值 23:30
                }
            }
            else
            {
                // 減少 30 分鐘，但不能低於 00:00
                if (current.Hour == 0 && current.Minute == 0)
                {
                    // 已經是 00:00，不再減少
                    e.Handled = true;
                    return;
                }

                current = current.AddMinutes(-30);

                // 防止變成前一天
                if (current.Hour < 0 || current.Day != 1)
                {
                    current = new DateTime(1900, 1, 1, 0, 0, 0);
                }
            }

            timeEdit.Time = current;
            e.Handled = true;
        }


        // 改名並改用 Click 事件
        private void DateTimeEdit_Click(object sender, EventArgs e)
        {
            if (sender is BaseEdit edit)
            {
                edit.SelectAll();
            }
        }
    }
}
