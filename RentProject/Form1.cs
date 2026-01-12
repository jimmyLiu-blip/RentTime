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
            RefreshProjectView();
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
            var loc = cmbLocationFilter.Text?.Trim();

            var filtered = _allRentTimes;

            if (!string.IsNullOrWhiteSpace(loc) && loc != "全部")
                filtered = _allRentTimes.Where(x => x.Location == loc).ToList();

            _projectView.LoadData(filtered);
            _calendarView.LoadData(filtered);
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

            // 預設選「全部」（第一次載入時）
            if (string.IsNullOrWhiteSpace(cmbLocationFilter.Text))
                cmbLocationFilter.EditValue = "全部";
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
            };

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
    }
}
