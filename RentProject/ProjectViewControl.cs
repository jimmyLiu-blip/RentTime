using DevExpress.Data;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Base;
using RentProject.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using RentProject.Shared.UIModels;
using DevExpress.Export;
using DevExpress.XtraPrinting;
using DevExpress.Utils;


namespace RentProject
{
    public partial class ProjectViewControl : XtraUserControl
    {
        public event Action<int>? EditRequested;

        private int? _selectedRentTimeId = null;

        private RepositoryItemImageComboBox? _riStatus;
        public ProjectViewControl()  //無參數建構子，讓Designer可以正常建立這個UserControl
        {
            InitializeComponent();

            gridView1.RowClick -= gridView1_RowClick;
            gridView1.RowClick += gridView1_RowClick;
        }

        public void LoadData(List<RentTime> list)
        {
            _selectedRentTimeId = null;

            list ??= new List<RentTime>();

            gridControl1.DataSource = list; // 表格中的資料來源為參數List
            gridView1.PopulateColumns();  // 自動產生欄位
            EnsureStatusEditor();

            var actionCol = gridView1.Columns.ColumnByFieldName("Action"); // 先找看看目前欄位是否存在 Action
            if (actionCol == null)
            {
                actionCol = gridView1.Columns.AddField("Action"); // UnboundType：「這欄不綁資料來源」，它不是 RentTime 的屬性，而是你額外加的欄位。
                actionCol.UnboundType = UnboundColumnType.Object; // Object 代表這欄可以放任何東西（我們會用它放按鈕）
            }

            var show = new[]
            {
                "BookingGroupNo",
                "BookingNo", "Area", "Location", "CustomerName", "PE", "JobNo",
                "StartDate", "EndDate", "ProjectNo", "ProjectName","Status","Action"
            };

            foreach (GridColumn col in gridView1.Columns)  //只顯示 show 裡列的欄位，其它全部隱藏。
                col.Visible = show.Contains(col.FieldName);

            actionCol.Caption = "Action";
            actionCol.Visible = true;

            var btnEdit = new RepositoryItemButtonEdit(); // RepositoryItemButtonEdit = 「看起來像一個欄位，但裡面是按鈕」的 UI 零件。
            btnEdit.TextEditStyle = TextEditStyles.HideTextEditor; // ButtonEdit 本質上是「文字輸入框 + 按鈕」，你不想要它像輸入框，所以把文字輸入框隱藏，只留按鈕。
            btnEdit.Buttons.Clear(); // 先把預設按鈕清掉（有些會自帶）
            var editBtn = new EditorButton(ButtonPredefines.Glyph);// ButtonPredefines.Glyph：按鈕樣式（通常用於圖示/按鈕外觀的一種預設）
            editBtn.Caption = "";
            editBtn.ToolTip = "編輯";
            editBtn.ImageOptions.ImageUri.Uri = "Edit";

            btnEdit.Buttons.Add(editBtn);

            actionCol.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            actionCol.OptionsFilter.AllowFilter = false;
            actionCol.OptionsColumn.AllowEdit = true;
            actionCol.ShowButtonMode = ShowButtonModeEnum.ShowAlways;

            gridControl1.RepositoryItems.Add(btnEdit); // 把這個 UI 零件「註冊」到 GridControl
            actionCol.ColumnEdit = btnEdit;            // 把 Action 欄指定使用這個 UI 零件呈現

            btnEdit.ButtonClick -= ActionButton_ButtonClick; // 綁定按鈕點擊事件（避免重複綁定）
            btnEdit.ButtonClick += ActionButton_ButtonClick;

            //gridView1.CustomColumnDisplayText -= GridView1_CustomColumnDisplayText;
            //gridView1.CustomColumnDisplayText += GridView1_CustomColumnDisplayText;

            ApplyProjectViewColumnSetting();

            // 依 BookingGroupNo分組
            gridView1.BeginUpdate();
            try
            {
                // 1. 開啟群組面板（想要上方可拖拉欄位就 true，不想要就 false)
                gridView1.OptionsView.ShowGroupPanel = false;

                // 2. 清掉舊的群組（避免重複呼叫 LoadData 時疊加）
                gridView1.ClearGrouping();

                // 3. 設定 BookingGroupNo 當分組欄位
                var groupCol = gridView1.Columns.ColumnByFieldName("BookingGroupNo");
                if (groupCol == null)
                {
                    XtraMessageBox.Show(
                        "找不到欄位：BookingGroupNo\n請確認 RentTime 是否真的有這個屬性，或 PopulateColumns 是否成功。","LoadData 錯誤");
                    
                    return;
                }

                groupCol.Caption = "Booking No."; // 群組列顯示用文字（你也可以改成 "Booking Group"）
                groupCol.GroupIndex = 0;          // 0 = 第一層群組

                // 勾選框選取列
                gridView1.OptionsSelection.MultiSelect = true;
                gridView1.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;

                // 群組烈也顯示勾選框
                gridView1.OptionsSelection.ShowCheckBoxSelectorInGroupRow = DevExpress.Utils.DefaultBoolean.True;

                // 欄位標題列顯示全選勾選框
                gridView1.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = DevExpress.Utils.DefaultBoolean.True;

                // 4. 預設展開所有群組（先讓你看效果）
                gridView1.ExpandAllGroups();

                // 5. 群組後通常不想再把這欄當一般欄位顯示（可選）
                groupCol.Visible = false;
            }
            finally
            {
                gridView1.EndUpdate();
            }

            // 群組排序
            gridView1.BeginSort();
            try
            {
                gridView1.SortInfo.Clear();

                var groupCol = gridView1.Columns.ColumnByFieldName("BookingGroupNo");
                var dateCol = gridView1.Columns.ColumnByFieldName("StartDate");      // 可選：讓同批次內跨天更直覺
                var idCol = gridView1.Columns.ColumnByFieldName("RentTimeId");     // 保底：讓排序穩定

                if (groupCol != null)
                    gridView1.SortInfo.Add(new GridColumnSortInfo(groupCol, DevExpress.Data.ColumnSortOrder.Descending));

                // 下面兩個是「可選但建議」：避免同 seq 時順序跳來跳去
                if (dateCol != null)
                    gridView1.SortInfo.Add(new GridColumnSortInfo(dateCol, ColumnSortOrder.Ascending));

                if (idCol != null)
                    gridView1.SortInfo.Add(new GridColumnSortInfo(idCol, ColumnSortOrder.Ascending));

            }
            finally
            {
                gridView1.EndSort();
            }

            gridView1.BestFitColumns(); //自動調整每個欄位寬度，讓內容比較不會被截掉
        }

        // 條件篩選
        public void ApplyFilter(RentTimeFilter filter, List<RentTime> all)
        {
            var list = all;

            var location = filter.Location?.Trim();

            if(!string.IsNullOrWhiteSpace(location) && location != "全部")
                list = all.Where(x => x.Location == location).ToList();

            LoadData(list);
        }

        private void ApplyProjectViewColumnSetting()
        {
            gridView1.Columns["BookingNo"].Caption = "Booking No.";
            gridView1.Columns["Area"].Caption = "區域";
            gridView1.Columns["Location"].Caption = "場地";
            gridView1.Columns["CustomerName"].Caption = "客戶名稱";
            gridView1.Columns["PE"].Caption = "PE";
            gridView1.Columns["StartDate"].Caption = "開始日期";
            gridView1.Columns["EndDate"].Caption = "結束日期";
            gridView1.Columns["JobNo"].Caption = "Job No.";
            gridView1.Columns["ProjectNo"].Caption = "Project No.";
            gridView1.Columns["ProjectName"].Caption = "Project Name";
            gridView1.Columns["Status"].Caption = "狀態";

            gridView1.Columns["BookingNo"].VisibleIndex = 1;
            gridView1.Columns["Area"].VisibleIndex = 2;
            gridView1.Columns["Location"].VisibleIndex = 3;
            gridView1.Columns["CustomerName"].VisibleIndex = 4;
            gridView1.Columns["PE"].VisibleIndex = 5;
            gridView1.Columns["StartDate"].VisibleIndex = 6;
            gridView1.Columns["EndDate"].VisibleIndex = 7;
            gridView1.Columns["JobNo"].VisibleIndex = 8;
            gridView1.Columns["ProjectNo"].VisibleIndex = 9;
            gridView1.Columns["ProjectName"].VisibleIndex = 10;
            gridView1.Columns["Status"].VisibleIndex = 11;
            gridView1.Columns["Action"].VisibleIndex = 12;

            gridView1.Columns["Status"].AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            gridView1.Columns["Status"].AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        }

        public event Action? RentTimeSaved;

        private void ActionButton_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var row = gridView1.GetRow(gridView1.FocusedRowHandle) as RentTime; // FocusedRowHandle：目前選到的那一列
            
            if (row == null) return;                                // GetRow(handle)：把那一列的資料物件取出來（就是 RentTime）

            EditRequested?.Invoke(row.RentTimeId);
        }

        // 真的點到資料列才記住 Id
        private void gridView1_RowClick(object sneder, DevExpress.XtraGrid.Views.Grid.RowClickEventArgs e)
        {
            if (e.RowHandle < 0)
            {
                _selectedRentTimeId = null;
                return;
            }

            if(gridView1.GetRow(e.RowHandle) is RentTime rt)
                _selectedRentTimeId = rt.RentTimeId;
        }

        // 取得選取的列租時單RentTimeId
        public int? GetFocusedRentTimeId()
        {
            return _selectedRentTimeId;
        }

        public List<RentTime> GetCheckedRentTime()
        { 
            var result = new List<RentTime>();

            // 會回傳「被勾選的資料列」(group row 通常是負數 handle)
            foreach (var handle in gridView1.GetSelectedRows())
            {
                if (handle < 0) continue;

                if (gridView1.GetRow(handle) is RentTime rt)
                { 
                    result.Add(rt);
                }
            }
            return result;
        }

        private void GridView1_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            // CustomColumnDisplayTextEventArgs e 就是 DevExpress 給你的「顯示用資料包」
            // 把 e 想成：「現在要顯示的這一格是誰？原始值是什麼？要顯示成什麼？」
            if (e.Column.FieldName != "Status") return;

            if (e.Value == null) return;

            var s = Convert.ToInt32(e.Value);

            e.DisplayText = s switch
            {
                0 => "草稿",
                1 => "租時中",
                2 => "已完成",
                3 => "已送出給助理",
                _ => $"未知({s})"
            };
        }

        public int GetCheckCount()
        {
            // 內建 CheckBoxRowSelect：勾選列 = SelectedRows
            // group row 會是負數 handle，要排除

            return gridView1.GetSelectedRows().Count(h => h >= 0);
        }

        public void ExportCheckRowsToXlsx(string path)
        { 
            var handles = gridView1.GetSelectedRows().Where(h => h >= 0).ToArray();
            if (handles.Length == 0) return;

            // ===== 1. 備份：避免匯出影響使用者畫面 =====
            var oldSelOnly = gridView1.OptionsPrint.PrintSelectedRowsOnly;
            var oldExpand = gridView1.OptionsPrint.ExpandAllGroups;
            var oldAutoWidth = gridView1.OptionsPrint.AutoWidth;

            var oldMultiSelect = gridView1.OptionsSelection.MultiSelect;
            var oldMultiMode = gridView1.OptionsSelection.MultiSelectMode;
            var oldShowGroup = gridView1.OptionsSelection.ShowCheckBoxSelectorInGroupRow;
            var oldShowHeader = gridView1.OptionsSelection.ShowCheckBoxSelectorInColumnHeader;

            // 備份欄寬（BestFit 會改到畫面）
            var oldWidths = new Dictionary<GridColumn, int>();
            foreach (GridColumn c in gridView1.Columns)
                oldWidths[c] = c.Width;

            // Action 欄不要匯出
            var actionCol = gridView1.Columns.ColumnByFieldName("Action");
            DefaultBoolean? oldActionPrintable = null;
            if (actionCol != null)
                oldActionPrintable = actionCol.OptionsColumn.Printable;

            try
            {
                // ===== 2. 確保只匯出勾選列 =====
                gridView1.ClearSelection();
                foreach (var h in handles)
                    gridView1.SelectRow(h);

                gridView1.OptionsPrint.PrintSelectedRowsOnly = true;
                gridView1.OptionsPrint.ExpandAllGroups = true;

                // ===== 3. 移除 Check 欄：暫時把 MultiSelectMode 從 CheckBoxRowSelect 改掉 =====
                // 這樣匯出時就不會多一欄「勾選框欄位」
                gridView1.OptionsSelection.MultiSelect = true;
                gridView1.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.RowSelect;
                gridView1.OptionsSelection.ShowCheckBoxSelectorInGroupRow = DefaultBoolean.False;
                gridView1.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = DefaultBoolean.False;

                // ===== 4. 移除 Action 欄（只影響匯出/列印）=====
                if (actionCol != null)
                    actionCol.OptionsColumn.Printable = DefaultBoolean.False;

                // ===== 5. 欄寬：用 BestFit 讓 Excel 看起來正常 =====
                // AutoWidth=false：不要硬擠成一頁，避免變超窄
                gridView1.OptionsPrint.AutoWidth = false;

                // 先 BestFit（會改畫面，所以我們有備份 width，finally 會還原）
                gridView1.BestFitColumns();

                // ===== 6. 匯出：一定要用 WYSIWYG 才會吃顯示文字（狀態才會是「草稿/租時中」）=====
                var opt = new XlsxExportOptionsEx
                {
                    ExportType = ExportType.WYSIWYG,
                    SheetName = "RentTimes"
                };

                using var ps = new PrintingSystem();
                using var link = new PrintableComponentLink(ps)
                {
                    Component = gridControl1
                };

                link.CreateDocument();

                // 重要：是 ExportToXlsx，不是 ExportToXls
                link.ExportToXlsx(path, opt);
            }
            finally
            {
                // ===== 7. 還原畫面設定 =====
                if (actionCol != null && oldActionPrintable.HasValue)
                    actionCol.OptionsColumn.Printable = oldActionPrintable.Value;

                foreach (var kv in oldWidths)
                    kv.Key.Width = kv.Value;

                gridView1.OptionsPrint.PrintSelectedRowsOnly = oldSelOnly;
                gridView1.OptionsPrint.ExpandAllGroups = oldExpand;
                gridView1.OptionsPrint.AutoWidth = oldAutoWidth;

                gridView1.OptionsSelection.MultiSelect = oldMultiSelect;
                gridView1.OptionsSelection.MultiSelectMode = oldMultiMode;
                gridView1.OptionsSelection.ShowCheckBoxSelectorInGroupRow = oldShowGroup;
                gridView1.OptionsSelection.ShowCheckBoxSelectorInColumnHeader = oldShowHeader;
            }
        }

        private void EnsureStatusEditor()
        {
            var col = gridView1.Columns.ColumnByFieldName("Status");
            if (col == null) return;

            // Status 可能是 int / int? / enum / enum?
            var t = Nullable.GetUnderlyingType(col.ColumnType) ?? col.ColumnType;

            object V(int n) => t.IsEnum ? Enum.ToObject(t, n) : n;

            if (_riStatus == null)
            {
                _riStatus = new RepositoryItemImageComboBox();
                _riStatus.Items.Add(new ImageComboBoxItem("草稿", V(0)));
                _riStatus.Items.Add(new ImageComboBoxItem("租時中", V(1)));
                _riStatus.Items.Add(new ImageComboBoxItem("已完成", V(2)));
                _riStatus.Items.Add(new ImageComboBoxItem("已送出給助理", V(3)));

                // 如果你的 Status 有可能是 null，想顯示「—」
                _riStatus.NullText = "—";

                gridControl1.RepositoryItems.Add(_riStatus);
            }

            col.ColumnEdit = _riStatus;
            col.OptionsColumn.AllowEdit = false; // 純顯示
        }
    }
}
