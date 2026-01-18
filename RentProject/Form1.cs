using ClosedXML.Excel;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using RentProject.Domain;
using RentProject.Repository;
using RentProject.Service;
using RentProject.Shared.UIModels;
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
        private readonly IJobNoApiClient _api;

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

            var baseUrl = ConfigurationManager.AppSettings["JobNoApi:BaseUrl"];
            var path = ConfigurationManager.AppSettings["JobNoApi:Path"];
            var apiKey = ConfigurationManager.AppSettings["JobNoApi:ApiKey"];
            var apiKeyHeader = ConfigurationManager.AppSettings["JobNoApi:ApiKeyHeader"];

            var timeoutText = ConfigurationManager.AppSettings["JobNoApi:TimeoutSeconds"];
            int timeoutSeconds = 10;
            if (!string.IsNullOrWhiteSpace(timeoutText) && int.TryParse(timeoutText, out var t) && t > 0)
                timeoutSeconds = t;

            // apiKey 允許空字串 -> 轉成 null
            apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;

            _api = new RealJobNoApiClient(
                baseUrl: baseUrl,
                path: path,
                apiKey: apiKey,
                apiKeyHeader: apiKeyHeader,
                timeoutSeconds: timeoutSeconds
            );


            _rentTimeRepo = new DapperRentTimeRepository(connectionString);
            _rentTimeservice = new RentTimeService(_rentTimeRepo);
            _projectRepo = new DapperProjectRepository(connectionString);
            _projectService = new ProjectService(_projectRepo);
            _jobNoRepo = new DapperJobNoRepository(connectionString);     
            _jobNoService = new JobNoService(_jobNoRepo, _api); 

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
            // 0. 先把「選取來源」統一成同一種格式：只保留刪除需要的欄位
            var selected = new List<(int RentTimeId, string BookingNo, int Status)>();

            if (_isCalendarView)
            {
                // CalendarView：GetSelectedForDelete() 會回傳 0 或 1 筆（目前設計）
                var calSelected = _calendarView.GetSelectedForDelete();

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

        // SaveFileDialog：Windows 內建的「另存新檔」視窗物件
        // 如果使用者只打 RentTimes_123 沒打 .xlsx，系統會自動補上副檔名。
        // XLWorkbook 是 ClosedXML 的「整本 Excel」物件（等於一個 .xlsx 檔）
        private void TestExportExcel()
        {
            var rows = _projectView.GetCheckedRentTime();

            if (rows.Count == 0)
            {
                XtraMessageBox.Show("請先勾選要匯出的租時單", "提示");
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "匯出 Excel",
                Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
                FileName = $"RentTimes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = "xlsx"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            var path = sfd.FileName;

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("RentTimes");
                ws.Cell(1, 1).Value = "RentTimeId";
                ws.Cell(1, 2).Value = "BookingNo";
                ws.Cell(1, 3).Value = "區域";
                ws.Cell(1, 4).Value = "場地";
                ws.Cell(1, 5).Value = "客戶名稱";
                ws.Cell(1, 6).Value = "PE";
                ws.Cell(1, 7).Value = "開始日期";
                ws.Cell(1, 8).Value = "結束日期";
                ws.Cell(1, 9).Value = "Job No.";
                ws.Cell(1, 10).Value = "ProjectNo";
                ws.Cell(1, 11).Value = "ProjectName";
                ws.Cell(1, 12).Value = "狀態";

                int r = 2;
                foreach (var x in rows)
                {
                    ws.Cell(r, 1).Value = x.RentTimeId;
                    ws.Cell(r, 2).Value = x.BookingNo;
                    ws.Cell(r, 3).Value = x.Area;
                    ws.Cell(r, 4).Value = x.Location;
                    ws.Cell(r, 5).Value = x.CustomerName;
                    ws.Cell(r, 6).Value = x.PE;
                    ws.Cell(r, 7).Value = x.StartDate;
                    ws.Cell(r, 8).Value = x.EndDate;
                    ws.Cell(r, 9).Value = x.JobNo;
                    ws.Cell(r, 10).Value = x.ProjectNo;
                    ws.Cell(r, 11).Value = x.ProjectName;
                    ws.Cell(r, 12).Value = StatusToText(x.Status);
                    r++;
                }

                ws.Column(7).Style.DateFormat.Format = "yyyy-mm-dd";
                ws.Column(8).Style.DateFormat.Format = "yyyy-mm-dd";

                // 3) 讓欄寬自動貼合內容（看起來像報表）
                ws.Columns().AdjustToContents();
                ws.Column(5).Width = 18; // E欄(客戶名稱) 手動加寬，數字可自行調大/調小


                wb.SaveAs(path);
            }
            ;

            XtraMessageBox.Show($"已輸出:{path}", "OK");
        }

        private void btnExportExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            TestExportExcel();
        }

        private static string StatusToText(int status) => status switch
        {
            0 => "草稿",
            1 => "租時中",
            2 => "已完成",
            3 => "已送出給助理",
            _ => "未知"
        };

        private void btnImportExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
       
            var job = _jobNoRepo.GetJobNoMasterByJobNo("JOB0001");

            XtraMessageBox.Show(job == null ? "查不到" : $"查到：{job.JobId} / {job.JobNo}");
            /*
            using var ofd = new OpenFileDialog
            {
                Title = "匯入 Excel",
                Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
                Multiselect = false
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            var path = ofd.FileName;

            List<ImportRentTimeRow> rows;
            try
            {
                rows = ReadImportRow(path);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}-{ex.Message}", "匯入失敗");
                return;
            }

            XtraMessageBox.Show($"讀取完成：{rows.Count} 筆", "OK");
            */
        }

        /*
        private static List<ImportRentTimeRow> ReadImportRow(string path)
        { 
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1); //讀第一張工作表

            var headerRow = ws.Row(1);
            var lastCol = headerRow.LastCellUsed().Address.ColumnNumber; //在這一列中，找「最後一格有內容的儲存格」

            // 建立表頭文字 =>　欄位索引
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); //StringComparer.OrdinalIgnoreCase：不分大小寫比較

            for (int c = 1; c <= lastCol; c++)
            {
                var name = headerRow.Cell(c).GetString().Trim();
                if(!string.IsNullOrWhiteSpace(name) && !map.ContainsKey(name)) //表頭如果是空的，不放進 map & 如果表頭重複，也不要覆蓋（避免壞掉）
                    map[name] = c; //把「表頭文字」對應到「欄位數字」
            }

            // 小工具：依表頭拿字串/日期
            string? GetStr(IXLRow row, string header) 
                => map.TryGetValue(header, out var col) ? row.Cell(col).GetString().Trim() : null;  

            DateTime? GetDate(IXLRow row, string header)
            { 
                if(!map.TryGetValue(header, out var col)) return null;
                var cell = row.Cell(col); // row.Cell(col) = 取得「這一列」的「第 col 欄」那一格儲存格（Cell）
                if (cell.IsEmpty()) return null;

                // Excel 日期可能是DateTime，也可能是字串
                if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();

                var s = cell.GetString().Trim();
                return DateTime.TryParse(s, out var dt) ? dt : (DateTime?)null;
            }

            TimeSpan? GetTime(IXLRow row, string header)
            {
                if (!map.TryGetValue(header, out var col)) return null;
                var cell = row.Cell(col);
                if (cell.IsEmpty()) return null;

                // 情況1：Excel 儲存格是 DateTime（可能是 1899/12/30 09:00 這種）
                if (cell.DataType == XLDataType.DateTime) 
                    return cell.GetDateTime().TimeOfDay;

                // 情況2：使用者輸入文字：09:00、9:00、0900
                var s = cell.GetString().Trim();

                if (TimeSpan.TryParse(s, out var ts)) return ts;

                // 0900 這種（四碼）
                if (s.Length == 4 && int.TryParse(s, out var hhmm))
                {
                    var hh = hhmm / 100;
                    var mm = hhmm % 100;
                    if (hh is >= 0 and <= 23 && mm is >= 0 and <= 59)
                        return new TimeSpan(hh, mm, 0);
                }
                return null;
            }

            var result = new List<ImportRentTimeRow>();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int r = 2; r <= lastRow; r++)
            { 
                var row = ws.Row(r);

                //用 IsEmpty 判斷整列是否真的沒內容；row.Cells(1, lastCol) = 這一列從 第 1 欄(A) 到 第 lastCol 欄 的所有儲存格。
                if (row.Cells(1, lastCol).All(c => c.IsEmpty())) continue; 

                var item = new ImportRentTimeRow
                {
                    ExcelRowNo = r,
                    Location = GetStr(row, "場地"),
                    JobNo = GetStr(row, "JobNo"),

                    // 這些可能會被 JobNo 覆蓋
                    Sales = GetStr(row, "Sales"),
                    CustomerName = GetStr(row, "客戶名稱"),
                    PE = GetStr(row, "PE"),
                    ProjectNo = GetStr(row, "ProjectNo"),
                    ProjectName = GetStr(row, "ProjectName"),

                    StartDate = GetDate(row, "開始日期"),
                    EndDate = GetDate(row, "結束日期"),
                    StartTime = GetTime(row, "開始時間"),
                    EndTime = GetTime(row, "結束時間")
                };
                result.Add(item);
            }
            return result;
        }*/
    }
}
