using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using RentProject.Clients;
using RentProject.Domain;
using RentProject.Repository;
using RentProject.Service;
using RentProject.Shared.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace RentProject
{
    public partial class Form1 : RibbonForm
    {
        private readonly DapperRentTimeRepository _rentTimeRepo;
        private readonly RentTimeService _rentTimeservice;
        private readonly ProjectService _projectService;
        private readonly DapperJobNoRepository _jobNoRepo;
        private readonly JobNoService _jobNoService;
        private readonly IRentTimeApiClient _rentTimeApiClient;

        private ProjectViewControl _projectView;
        private CalendarViewControl _calendarView;

        private List<RentTime> _allRentTimes = new();

        private AdvancedFilter? _advanceFilter = null; // 暫存進階條件

        // true = 目前顯示 CalendarView；false = 目前顯示 ProjectView
        private bool _isCalendarView = true;

        public Form1(
            DapperRentTimeRepository rentTimeRepo,
            RentTimeService rentTimeService,
            ProjectService projectService,
            DapperJobNoRepository jobNoRepo,
            JobNoService jobNoService,
            IRentTimeApiClient rentTimeApiClient
        )
        {
            InitializeComponent();

            _rentTimeRepo = rentTimeRepo;
            _rentTimeservice = rentTimeService;
            _projectService = projectService;
            _jobNoRepo = jobNoRepo;
            _jobNoService = jobNoService;
            _rentTimeApiClient = rentTimeApiClient;

            _projectView = new ProjectViewControl(_rentTimeservice, _projectService, _jobNoService) { Dock = DockStyle.Fill };
            _projectView.RentTimeSaved += async () =>
            {
                try { await RefreshProjectViewAsync(); }
                catch (Exception ex) { XtraMessageBox.Show(ex.Message, "Error"); }
            };

            _calendarView = new CalendarViewControl { Dock = DockStyle.Fill };

            _projectView.EditRequested += OpenEditRentTime;
            _calendarView.EditRequested += OpenEditRentTime;
        }

        private async void Form1_Load(object sender, System.EventArgs e)
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

            _calendarView.PeriodChangeRequested -= CalendarView_PeriodChangeRequested;
            _calendarView.PeriodChangeRequested += CalendarView_PeriodChangeRequested;


            // 先抓資料 + 塞場地下拉（但不顯示資料）
            await RefreshProjectViewAsync();

            // 強制啟動時沒選場地 -> 觸發空白顯示
            cmbLocationFilter.EditValue = null;

            ShowProjectView();
        }

        private async void btnAddRentTime_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var form = new Project(_rentTimeApiClient, _projectService, _jobNoService);

            var dr = form.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                await RefreshProjectViewAsync();
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

        private void OpenEditRentTime(int rentTimedId)
        {
            var form = new Project(_rentTimeApiClient, _projectService, _jobNoService, rentTimedId);

            // 只要表單內狀態有變（開始/完成/送出）就刷新
            // handler 就代表「刷新並保持畫面」這個動作
            Action handler = () =>
            {
                _ = RefreshAndKeepViewAsync();
            };

            // Project 表單某些操作（開始、完成、送出…）會觸發 RentTimeChanged?.Invoke()
            // 一旦觸發，就會通知所有有訂閱它的人
            // 「當 Project 表單說『租時狀態變了！』時，請幫我執行 handler 這個動作」
            form.RentTimeChanged += handler;

            var dr = form.ShowDialog();

            if (dr == DialogResult.OK)
            {
                handler();// 存檔/OK 就刷新
            }

            form.RentTimeChanged -= handler;
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

        private async Task RefreshProjectViewAsync()
        {
            _allRentTimes = await _rentTimeApiClient.GetProjectViewListAsync();
            RefreshLocationFilterItems();
            ApplyLocationFilterAndRefresh();
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
        private async void btnDelete_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 0. 先把「選取來源」統一成同一種格式：只保留刪除需要的欄位
            var selected = new List<(int RentTimeId, string BookingNo, int Status)>();

            if (_isCalendarView)
            {
                // CalendarView：GetSelectedDetail() 會回傳 0 或 1 筆（目前設計）
                var calSelected = _calendarView.GetSelectedDetail();

                selected = calSelected
                    .Where(x => x.RentTimeId.HasValue)
                    .Select(x => (x.RentTimeId!.Value, x.BookingNo ?? "", x.Status))
                    .ToList();
            }
            else
            {
                // ProjectView：原本就是可多選
                var projectSelected = _projectView.GetCheckedRentTime();

                selected = projectSelected
                    .Select(x => (x.RentTimeId, x.BookingNo ?? "", x.Status))
                    .ToList();
            }

            if (selected.Count == 0)
            {
                XtraMessageBox.Show(
                    _isCalendarView ? "請先點選日曆上的案件並選擇右側 BookingNo 再刪除" : "請先勾選要刪除的租時單",
            "提示");
                return;
            }

            // 1-2) 擋 Finished、Submit 不能刪
            var blocked = selected.Where(x => x.Status == 2 || x.Status == 3).ToList();
            if (blocked.Count > 0)
            {
                var preview = string.Join("\n",
                    blocked.Take(10).Select(x => $"{x.BookingNo}(Id:{x.RentTimeId})"));

                XtraMessageBox.Show(
                    $"你勾選的資料包含「已完成/已送出」狀態，不能刪除。\n" +
                    $"請取消選取後再刪除。\n\n" +
                    $"筆數：{blocked.Count}\n" +
                    $"{preview}",
                    "禁止刪除",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 2) 確認視窗

            var confirm = XtraMessageBox.Show(
                $"確認要刪除 {selected.Count} 筆租時單嗎?\n（刪除後會從清單移除）", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            // 3) 批次刪除（改成走 WebAPI）
            btnDelete.Enabled = false; // 防止連點（你的按鈕名稱若不同就改掉）
            try
            {
                // 這裡先用同一個 createdBy（你目前專案是用 CreatedBy 當操作人）
                // 之後做登入系統，再改成 currentUserName

                var user = GetCurrentUser();

                foreach (var rt in selected)
                {
                    await _rentTimeApiClient.DeleteAsync(rt.RentTimeId, user);
                }

                XtraMessageBox.Show($"刪除完成:{selected.Count} 筆", " 完成");

                // 4) 刷新列表
                await RefreshProjectViewAsync();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
            finally
            {
                btnDelete.Enabled = true;
            }
        }

        // 按鈕：送出給助理
        private async void btnSubmitToAssistant_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            btnSubmitToAssistant.Enabled = false; // 避免連點

            try
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

                var user = GetCurrentUser();

                foreach (var rt in selected)
                {
                    await _rentTimeApiClient.SubmitToAssistantAsync(rt.RentTimeId, user);
                }

                XtraMessageBox.Show($"送出完成：{selected.Count}筆", "完成");

                await RefreshProjectViewAsync();

            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name} - {ex.Message}", "Error");
            }
            finally
            {
                btnSubmitToAssistant.Enabled = true;
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

            var statusText = cmbStatusFilter.EditValue?.ToString()?.Trim() ?? "全部";

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

                var dr = f.ShowDialog();

                if (dr != DialogResult.OK) return;

                // 3. 存起來（之後 ApplyLocationFilterAndRefresh 會用到）
                _advanceFilter = f.FilterResult;

                // 4. 刷新列表
                ApplyLocationFilterAndRefresh();
            }
        }

        // 重新整理
        private async void btnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // 1. 清掉進階篩選
            _advanceFilter = null;

            // 2. 如果狀態也回到「全部」，把這行打開
            cmbStatusFilter.EditValue = "全部";

            // 3) 重新抓 DB + 重新套用篩選（此時進階已經是 null）
            await RefreshProjectViewAsync();

            if (_isCalendarView) ShowCalendarView();
            else ShowProjectView();
        }

        // SaveFileDialog：Windows 內建的「另存新檔」視窗物件
        // 如果使用者只打 RentTimes_123 沒打 .xlsx，系統會自動補上副檔名。
        // XLWorkbook 是 ClosedXML 的「整本 Excel」物件（等於一個 .xlsx 檔）
        private void TestExportExcel()
        {
            // 1. 先只支援 ProjectView 匯出
            if (_isCalendarView)
            {
                XtraMessageBox.Show("請先切換到「案件」畫面再匯出", "提示");
                return;
            }

            // 2. 檢查是否有勾選（用你在 ProjectViewControl 新增的 GetCheckCount）
            if (_projectView.GetCheckCount() == 0)
            {
                XtraMessageBox.Show("請先勾選要匯出的租時單", "提示");
                return;
            }

            // 3. 選擇匯出路徑
            using var sfd = new SaveFileDialog
            {
                Title = "匯出 Excel",
                Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
                FileName = $"RentTimes_{DateTime.Now:yyyyMMdd}.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            // 4. 交給 ProjectViewControl 匯出（會吃目前欄位顯示/欄位順序/顯示文字）
            _projectView.ExportCheckRowsToXlsx(sfd.FileName);

            XtraMessageBox.Show($"已輸出:{sfd.FileName}", "OK");

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"匯出完成，但自動開啟失敗：{ex.Message}", "提示");
            }
        }

        private void btnExportExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            TestExportExcel();
        }

        private void btnEditRentTime_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (_isCalendarView)
            {
                _calendarView.RequestEditSelected();
                return;
            }

            var id = _projectView.GetFousedRentTimeId();

            if (!id.HasValue)
            {
                XtraMessageBox.Show("請先點擊要編輯的租時單", "提示");
                return;
            }

            // 直接沿用你已寫好的流程：開 Project 表單 + 刷新
            OpenEditRentTime(id.Value);
        }

        private void btnImportExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
        }

        private bool CalendarView_PeriodChangeRequested(int rentTimeId, DateTime newStart, DateTime newEnd)
        {
            try
            {
                // 1) 基本防呆
                if (newEnd < newStart)
                {
                    XtraMessageBox.Show("結束時間不能早於開始時間", "提示");
                    return false;
                }

                var now = DateTime.Now;
                var user = GetCurrentUser();  // 你自己的登入者名稱變數

                // 2) 寫回 DB（含跨日拆單）
                // 建議放在 Service，Form1 不要直接寫 SQL
                bool ok = _rentTimeApiClient
                    .ChangeDraftPeriodWithSplitAsync(rentTimeId, newStart, newEnd, user)
                    .GetAwaiter()
                    .GetResult();

                if (!ok)
                {
                    // 常見原因：已不是 Draft / 已刪除 / 資料被改過
                    XtraMessageBox.Show("只有 Draft 才能拖拉調整（或資料已更新）", "提示");
                    return false;
                }

                // 關鍵：不要在 Scheduler 的 Drop/Resize 事件「同步」刷新
                // 不要現在立刻做 RefreshProjectView()，改成等 UI 有空再做
                this.BeginInvoke(new Action(async () =>
                {
                    await RefreshProjectViewAsync();
                }));

                return true;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"更新失敗：{ex.Message}", "錯誤");
                return false;
            }
        }

        private string GetCurrentUser() => "Jimmy"; // 之後接登入系統就改這裡

        private async Task RefreshAndKeepViewAsync()
        {
            await RefreshProjectViewAsync();

            // 刷新後保持你原本畫面不跳走
            if (_isCalendarView) ShowCalendarView();
            else ShowProjectView();
        }
    }
}
