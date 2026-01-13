using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using RentProject.Domain;
using RentProject.Repository;
using RentProject.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;
using RentProject.Shared.UIModels;


namespace RentProject
{
    public partial class Form1 : RibbonForm
    {
        private readonly DapperRentTimeRepository _rentTimeRepo;
        private readonly RentTimeService _rentTimeservice;
        private readonly DapperProjectRepository _projectRepo;
        private readonly ProjectService _projectService;
        private readonly DapperJobNoRepository _jobNoRepo;
        private readonly JobNoService _jobNoService;

        private ProjectViewControl _projectView;
        private CalendarViewControl _calendarView;

        private List<RentTime> _allRentTimes = new();

        private AdvancedFilter? _advanceFilter = null; // 暫存進階條件

        // true = 目前顯示 CalendarView；false = 目前顯示 ProjectView
        private bool _isCalendarView = true;

        public Form1()
        {
            InitializeComponent();

            var connectionString =
                ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString;

            _rentTimeRepo = new DapperRentTimeRepository(connectionString);
            _rentTimeservice = new RentTimeService(_rentTimeRepo);
            _projectRepo = new DapperProjectRepository(connectionString);
            _projectService = new ProjectService(_projectRepo);
            _jobNoRepo = new DapperJobNoRepository(connectionString);
            _jobNoService = new JobNoService(_jobNoRepo);

            _projectView = new ProjectViewControl(_rentTimeservice, _projectService, _jobNoService) { Dock = DockStyle.Fill };

            _projectView.RentTimeSaved += RefreshProjectView; //ProjectViewControl 說「存好了」→ Form1 刷新列表

            _calendarView = new CalendarViewControl { Dock = DockStyle.Fill };
        }

        private void Form1_Load(object sender, System.EventArgs e)
        {
            mainPanel.Controls.Add(_projectView);
            mainPanel.Controls.Add(_calendarView);

            cmbLocationFilter.EditValueChanged -= cmbLocationFilter_EditValueChanged;
            cmbLocationFilter.EditValueChanged += cmbLocationFilter_EditValueChanged;

            cmbStatusFilter.Properties.Items.Clear();
            cmbStatusFilter.Properties.Items.AddRange(new[]
            {
                "全部",
                "草稿",
                "租時中",
                "已完成",
                "已送出給助理"
            });

            cmbStatusFilter.EditValue = "全部";

            // 綁定事件：狀態改變就刷新
            cmbStatusFilter.EditValueChanged -= cmbStatusFilter_EditValueChanged;
            cmbStatusFilter.EditValueChanged += cmbStatusFilter_EditValueChanged;

            btnAdvancedFilter.Click -= btnAdvancedFilter_Click;
            btnAdvancedFilter.Click += btnAdvancedFilter_Click;

            // 先抓資料 + 塞場地下拉（但不顯示資料）
            RefreshProjectView();

            // 強制啟動時沒選場地 -> 觸發空白顯示
            cmbLocationFilter.EditValue = null;

            ShowProjectView();
        }

        private void btnAddRentTime_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var form = new Project(_rentTimeservice, _projectService, _jobNoService);

            var dr = form.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                RefreshProjectView();
                ShowProjectView();
            }
        }

        private void btnTestConnection_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string msg = _rentTimeRepo.TestConnection();

            XtraMessageBox.Show(msg, "TestConnection");
        }

        private void ShowProjectView()
        {
            _projectView.BringToFront();

            _isCalendarView = false;
            btnView.Caption = "切換到日曆";
        }

        private void ShowCalendarView()
        {
            _calendarView.BringToFront();
            _isCalendarView = true;

            btnView.Caption = "切換到案件";
        }

        private void btnView_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_isCalendarView)
            {
                ShowProjectView();
            }
            else
                ShowCalendarView();
        }

        private void RefreshProjectView()
        {
            _allRentTimes = _rentTimeservice.GetProjectViewList();
            RefreshLocationFilterItems();   // 先把場地選項填進下拉
            ApplyLocationFilterAndRefresh(); // 再用目前選的場地去刷新兩個畫面
        }

        private void ApplyLocationFilterAndRefresh()
        {
            var loc = cmbLocationFilter.EditValue?.ToString()?.Trim();

            // ======= 沒選場地：畫面先空白 + Disable =======
            if (string.IsNullOrWhiteSpace(loc))
            {
                _projectView.LoadData(new List<RentTime>());
                _calendarView.LoadData(new List<RentTime>());

                SetExtraFiltersEnabled(false); // 狀態/進階先鎖住

                return;
            }

            // 有選場地：狀態/進階才可以選
            SetExtraFiltersEnabled(true);

            var filtered = _allRentTimes;

            if (loc != "全部")
                filtered = _allRentTimes.Where(x => x.Location == loc).ToList();

            // 狀態過濾
            var statusText = cmbStatusFilter.EditValue?.ToString()?.Trim() ?? "全部";

            int? status = statusText switch
            {
                "草稿" => 0,
                "租時中" => 1,
                "已完成" => 2,
                "已送出給助理" => 3,
                _ => null // 全部
            };

            if (status.HasValue)
                filtered = filtered.Where(x => x.Status == status.Value).ToList();

            // ======= 進階過濾（套用你暫存的 _advanceFilter） =======
            if (_advanceFilter != null && IsAdvanceFilterActive(_advanceFilter))
            {
                filtered = ApplyAdvancedFilter(filtered, _advanceFilter);
            }

            // 讓按鈕顏色跟著變（有套用進階篩選就變色）


            _projectView.LoadData(filtered);
            _calendarView.LoadData(filtered);
        }

        private static bool IsAdvanceFilterActive(AdvancedFilter f)
        {
            // 只要任一個條件有填，就算「有啟用進階篩選」
            return
                !string.IsNullOrWhiteSpace(f.BookingNo)
                || !string.IsNullOrWhiteSpace(f.Area)
                || !string.IsNullOrWhiteSpace(f.Location)
                || !string.IsNullOrWhiteSpace(f.PE)
                || !string.IsNullOrWhiteSpace(f.ProjectNo)
                || !string.IsNullOrWhiteSpace(f.ProjectName)
                || !string.IsNullOrWhiteSpace(f.CustomerName)
                || f.Status.HasValue
                || f.StartDate.HasValue
                || f.EndDate.HasValue;
        }

        private static List<RentTime> ApplyAdvancedFilter(List<RentTime> source, AdvancedFilter f)
        {
            var q = source.AsEnumerable();

            bool ContainsIgnoreCase(string? src, string keyword)
                => !string.IsNullOrWhiteSpace(src)
                    && src.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!string.IsNullOrWhiteSpace(f.BookingNo))
                q = q.Where(x => ContainsIgnoreCase(x.BookingNo, f.BookingNo));

            if (!string.IsNullOrWhiteSpace(f.Area))
                q = q.Where(x => ContainsIgnoreCase(x.Area, f.Area));

            if (!string.IsNullOrWhiteSpace(f.Location))
                q = q.Where(x => ContainsIgnoreCase(x.Location, f.Location));

            if (!string.IsNullOrWhiteSpace(f.PE))
                q = q.Where(x => ContainsIgnoreCase(x.PE, f.PE));

            if (!string.IsNullOrWhiteSpace(f.ProjectNo))
                q = q.Where(x => ContainsIgnoreCase(x.ProjectNo, f.ProjectNo));

            if (!string.IsNullOrWhiteSpace(f.ProjectName))
                q = q.Where(x => ContainsIgnoreCase(x.ProjectName, f.ProjectName));

            if (!string.IsNullOrWhiteSpace(f.CustomerName))
                q = q.Where(x => ContainsIgnoreCase(x.CustomerName, f.CustomerName));

            if (f.Status.HasValue)
                q = q.Where(x => x.Status == f.Status.Value);

            // 日期篩選：這裡用「RentTime 的 StartDate」做 From/To（最常見用法）
            // 如果你 RentTime 欄位不是 StartDate，改成你實際的欄位名稱即可
            if (f.StartDate.HasValue)
            {
                var from = f.StartDate.Value.Date;
                q = q.Where(x => x.StartDate >= from);
            }

            if (f.EndDate.HasValue)
            {
                var toExclusive = f.EndDate.Value.Date.AddDays(1);
                q = q.Where(x => x.StartDate < toExclusive);
            }

            return q.ToList();
        }

        private void SetExtraFiltersEnabled(bool enabled)
        {
            // 先用 Find 避免還沒做出狀態/進階控制項就編譯失敗
            // Controls.Find("cmbStatusFilter", true) => 去整個 Form1 裡面找 Name = cmbStatusFilter 的控制項
            // true 代表「包含子容器內的控制項也找」（例如在 Panel 裡）
            var statusCtrl = this.Controls.Find("cmbStatusFilter", true).FirstOrDefault();
            if (statusCtrl != null) statusCtrl.Enabled = enabled;

            var advancedCtrl = this.Controls.Find("btnAdvancedFilter", true).FirstOrDefault();
            if (advancedCtrl != null) advancedCtrl.Enabled = enabled;
        }

        private void cmbLocationFilter_EditValueChanged(object sender, System.EventArgs e)
        {
            ApplyLocationFilterAndRefresh();
        }

        private void RefreshLocationFilterItems()
        {
            // 取出所有不重複的 Location
            var locations = _allRentTimes
                .Select(x => x.Location?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // 塞進下拉選單
            cmbLocationFilter.Properties.Items.BeginUpdate();
            try
            {
                cmbLocationFilter.Properties.Items.Clear();
                cmbLocationFilter.Properties.Items.Add("全部");
                cmbLocationFilter.Properties.Items.AddRange(locations.ToArray());
            }
            finally
            {
                cmbLocationFilter.Properties.Items.EndUpdate();
            }
        }

        // 刪除租時單(可多選)
        private void btnDelete_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 假設目前顯示的是ProjectView
            // 1) 取出勾選
            var selected = _projectView.GetCheckedRentTime();

            if (selected.Count == 0)
            {
                XtraMessageBox.Show("請先勾選要刪除的租時單", "提示");
                return;
            }

            // 1-2) 擋 Finished、Submit 不能刪
            var blocked = selected.Where(x => x.Status == 2 || x.Status == 3).ToList();
            if (blocked.Count > 0)
            {
                var previewFinished = string.Join("\n",
                    blocked.Take(10).Select(x => $"{x.BookingNo}(Id:{x.RentTimeId})"));

                XtraMessageBox.Show(
                    $"你勾選的資料包含「已完成(Finished)」狀態，不能刪除。\n" +
                    $"請取消勾選後再刪除。\n\n" +
                    $"筆數：{blocked.Count}\n" +
                    $"{previewFinished}",
                    "禁止刪除",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 2) 確認視窗

            var confirm = XtraMessageBox.Show(
                $"確認要刪除 {selected.Count} 筆租時單嗎?\n（刪除後會從清單移除）", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            // 3) 批次刪除（沿用你原本單張刪除的 service）
            try
            {
                // 這裡先用同一個 createdBy（你目前專案是用 CreatedBy 當操作人）
                // 之後做登入系統，再改成 currentUserName

                var createdBy = "Jimmy";

                foreach (var rt in selected)
                {
                    _rentTimeservice.DeletedRentTime(rt.RentTimeId, createdBy, DateTime.Now);
                }

                XtraMessageBox.Show($"刪除完成:{selected.Count} 筆", " 完成");

                // 4) 刷新列表
                RefreshProjectView();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        // 按鈕：送出給助理
        private void btnSubmitToAssistant_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var selected = _projectView.GetCheckedRentTime();

            if (selected.Count == 0)
            {
                XtraMessageBox.Show("請先勾選要送出給助理的租時單", "提示");
                return;
            }
            ;

            var blocked = selected.Where(x => x.Status != 2).ToList();
            if (blocked.Count > 0)
            {
                var preview = string.Join("\n",
                    blocked.Take(10).Select(x => $"{x.BookingNo}(Id:{x.RentTimeId})"));

                XtraMessageBox.Show(
                    $"你勾選的資料包含「非已完成(Finished)」狀態，不能送出。\n" +
                    $"請取消勾選後再刪除。\n\n" +
                    $"筆數：{blocked.Count}\n" +
                    $"{preview}",
                    "禁止送出",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = XtraMessageBox.Show($"確認要送出 {selected.Count} 筆租時單給助理嗎",
                "確認送出",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var user = "Jimmy";

                foreach (var rt in selected)
                {
                    _rentTimeservice.SubmitToAssistantById(rt.RentTimeId, user);
                }

                XtraMessageBox.Show($"送出完成：{selected.Count}筆", "完成");

                RefreshProjectView();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
        }

        private void cmbStatusFilter_EditValueChanged(object sender, EventArgs e)
        {
            ApplyLocationFilterAndRefresh();
        }

        // 進階按鈕
        private void btnAdvancedFilter_Click(object sender, EventArgs e)
        {
            // 1. 沒選場地時不要開
            var loc = cmbLocationFilter.EditValue?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(loc))
            {
                XtraMessageBox.Show("請先選擇場地才能使用進階篩選", "提示");
                return;
            }

            // 2-1. 先建立「進階表單下拉要用的資料來源」：套用場地 + 狀態（不套用進階）
            var baseList = _allRentTimes.AsEnumerable();

            if (loc != "全部")
                baseList = baseList.Where(x => x.Location == loc);

            var statusText = cmbLocationFilter.EditValue?.ToString()?.Trim() ?? "全部";

            int? status = statusText switch
            {
                "草稿" => 0,
                "租時中" => 1,
                "已完成" => 2,
                "已送出給助理" => 3,
                _ => null
            };

            if (status.HasValue)
                baseList = baseList.Where(x => x.Status == status.Value);

            var ListForAdvanced = baseList.ToList();

            // 2-2. 開進階篩選視窗
            using (var f = new AdvancedFilterForm(ListForAdvanced, _advanceFilter))
            {
                f.StartPosition = FormStartPosition.CenterParent;

                f.TopMost = true;

                var dr = f.ShowDialog(this);

                if (dr != DialogResult.OK) return;

                // 3. 存起來（之後 ApplyLocationFilterAndRefresh 會用到）
                _advanceFilter = f.FilterResult;

                // 4. 刷新列表
                ApplyLocationFilterAndRefresh();
            }
        }

        // 重新整理
        private void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 1. 清掉進階篩選
            _advanceFilter = null;

            // 2. 如果狀態也回到「全部」，把這行打開
            cmbStatusFilter.EditValue = "全部";

            // 3) 重新抓 DB + 重新套用篩選（此時進階已經是 null）
            RefreshProjectView();

            if (_isCalendarView) ShowCalendarView();
            else ShowProjectView();
        }
    }
}
