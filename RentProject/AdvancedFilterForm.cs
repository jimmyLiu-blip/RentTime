using DevExpress.XtraEditors;
using RentProject.Domain;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RentProject
{
    public partial class AdvancedFilterForm : DevExpress.XtraEditors.XtraForm
    {
        // =========================
        // 1) 狀態/資料欄位（State）
        // =========================

        // 回傳給 Form1 的篩選條件
        public AdvancedFilter FilterResult { get; private set; } = new AdvancedFilter();

        private readonly List<RentTime> _data;     //「傳進來的資料清單」，用來生成下拉選單的候選值
        private readonly AdvancedFilter? _current; //「上一輪已套用的進階條件」，讓你打開視窗時能把選項回填

        // =========================
        // 2) 建構/初始化（Init）
        // =========================

        public AdvancedFilterForm(List<RentTime> data, AdvancedFilter? current = null)
        {
            InitializeComponent();

            try
            {
                _data = data ?? new List<RentTime>();
                _current = current;

                btnFiltered.Click -= btnFiltered_Click;
                btnFiltered.Click += btnFiltered_Click;

                btnCancel.Click -= btnCancel_Click;
                btnCancel.Click += btnCancel_Click;

                InitDropdowns();
                ApplyCurrentToUi();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "進階篩選初始化失敗");
                this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                this.Close();
            }
        }

        // =========================
        // 3) 使用者操作（Events）
        // =========================

        private void btnFiltered_Click(object sender, System.EventArgs e)
        {
            try
            {
                // 1. 讀取畫面上的值 (空白就當作 null = 不篩)
                var bookingNo = GetTextOrNull(cmbBookingNo);
                var area = GetTextOrNull(cmbArea);
                var location = GetTextOrNull(cmbLocation);
                var pe = GetTextOrNull(cmbPE);

                var projectNo = GetTextOrNull(cmbProjectNo);
                var projectName = GetTextOrNull(cmbProjectName);
                var customerName = GetTextOrNull(cmbCompany);

                var start = GetDateOrNull(StartDate);
                var end = GetDateOrNull(EndDate);

                // 2. 狀態文字 => int
                var statusText = GetTextOrNull(cmbStatus);
                int? status = statusText switch
                {
                    "草稿" => 0,
                    "租時中" => 1,
                    "已完成" => 2,
                    "已送出給助理" => 3,
                    "全部" => null,
                    _ => null,
                };

                // 3. 塞進回物件
                FilterResult = new AdvancedFilter
                {
                    BookingNo = bookingNo,
                    Area = area,
                    Location = location,
                    PE = pe,
                    ProjectNo = projectNo,
                    ProjectName = projectName,
                    CustomerName = customerName,
                    StartDate = start,
                    EndDate = end,
                    Status = status
                };

                // 4. 回傳 OK給Form1
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "套用篩選失敗");
            }
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        // =========================
        // 4) 讀取 UI 值的工具方法
        // =========================

        private static string? GetTextOrNull(DevExpress.XtraEditors.BaseEdit ctrl)
        {
            var s = ctrl.EditValue?.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static DateTime? GetDateOrNull(DevExpress.XtraEditors.DateEdit ctrl)
        {
            return ctrl.EditValue is DateTime d ? d.Date : (DateTime?)null;
        }

        // =========================
        // 5) 初始化下拉內容
        // =========================

        private void InitDropdowns()
        {
            // 讓 ComboBoxEdit 仍可手打（不是只能選）
            cmbBookingNo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbArea.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbLocation.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbPE.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbProjectNo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbProjectName.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbCompany.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;
            cmbStatus.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

            // 依 _data 取出 Distinct 後塞進 Items
            FillCombo(cmbBookingNo, DistinctList(_data.Select(x => x.BookingNo)));
            FillCombo(cmbArea, DistinctList(_data.Select(x => x.Area)));
            FillCombo(cmbLocation, DistinctList(_data.Select(x => x.Location)));
            FillCombo(cmbPE, DistinctList(_data.Select(x => x.PE)));

            FillCombo(cmbProjectNo, DistinctList(_data.Select(x => x.ProjectNo)));
            FillCombo(cmbProjectName, DistinctList(_data.Select(x => x.ProjectName)));
            FillCombo(cmbCompany, DistinctList(_data.Select(x => x.CustomerName)));

            // 狀態是固定選單（不要用 _data）
            cmbStatus.Properties.Items.BeginUpdate();
            try
            {
                cmbStatus.Properties.Items.Clear();
                cmbStatus.Properties.Items.AddRange(new[]
                {
                    "全部",
                    "草稿",
                    "租時中",
                    "已完成",
                    "已送出給助理"
                });
            }
            finally
            {
                cmbStatus.Properties.Items.EndUpdate();
            }
        }

        // =========================
        // 6) 把既有篩選條件套回 UI
        // =========================

        private void ApplyCurrentToUi()
        {
            if (_current == null) return;

            // 文字類
            cmbBookingNo.EditValue = _current.BookingNo;
            cmbArea.EditValue = _current.Area;
            cmbLocation.EditValue = _current.Location;
            cmbPE.EditValue = _current.PE;
            cmbProjectNo.EditValue = _current.ProjectNo;
            cmbProjectName.EditValue = _current.ProjectName;
            cmbCompany.EditValue = _current.CustomerName;

            // 日期
            StartDate.EditValue = _current.StartDate;
            EndDate.EditValue = _current.EndDate;

            // 狀態 int -> 文字
            cmbStatus.EditValue = _current.Status switch
            {
                0 => "草稿",
                1 => "租時中",
                2 => "已完成",
                3 => "已送出給助理",
                _ => "全部"
            };
        }

        // =========================
        // 7) 下拉資料處理工具
        // =========================

        private static List<string> DistinctList(IEnumerable<string?> src)
        {
            return src
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList()!;
        }

        private static void FillCombo(DevExpress.XtraEditors.ComboBoxEdit cmb, List<string> items)
        {
            cmb.Properties.Items.BeginUpdate();
            try
            {
                cmb.Properties.Items.Clear();
                cmb.Properties.Items.AddRange(items.ToArray());
            }
            finally
            {
                cmb.Properties.Items.EndUpdate();
            }
        }
    }
}
