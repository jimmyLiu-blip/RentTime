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
using RentProject.Service;
using RentProject.Shared.UIModels;


namespace RentProject
{
    public partial class ProjectViewControl : XtraUserControl
    {
        private readonly RentTimeService _rentTimeService;
        private readonly ProjectService _projectService;
        private readonly JobNoService _jobNoService;

        public ProjectViewControl()  //無參數建構子，讓Designer可以正常建立這個UserControl
        {
            InitializeComponent();
        }

        public ProjectViewControl(RentTimeService rentTimeService, ProjectService projectService, JobNoService jobNoService):this() //有參數建構子，注入Service，this()的意思是先跑初始化設定，把畫面元件都建立好後，才把下面那兩行Service填進去
        {
            _rentTimeService = rentTimeService 
                ?? throw new ArgumentNullException(nameof(rentTimeService));

            _projectService = projectService 
                ?? throw new ArgumentNullException(nameof(projectService));

            _jobNoService = jobNoService
                ?? throw new ArgumentNullException(nameof(jobNoService));
        }

        public void LoadData(List<RentTime> list)
        {
            gridControl1.DataSource = list; // 表格中的資料來源為參數List
            gridView1.PopulateColumns();  // 自動產生欄位

            var actionCol = gridView1.Columns.ColumnByFieldName("Action"); // 先找看看目前欄位是否存在 Action
            if (actionCol == null)
            {
                actionCol = gridView1.Columns.AddField("Action"); // UnboundType：「這欄不綁資料來源」，它不是 RentTime 的屬性，而是你額外加的欄位。
                actionCol.UnboundType = UnboundColumnType.Object; // Object 代表這欄可以放任何東西（我們會用它放按鈕）
            }

            var show = new[]
            {
                "BookingGroupNo",
                "BookingNo", "Area", "Location", "CustomerName", "PE",
                "StartDate", "EndDate", "ProjectNo", "ProjectName","Action"
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
                var groupCol = gridView1.Columns["BookingGroupNo"];
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
            gridView1.Columns["ProjectNo"].Caption = "Project No.";
            gridView1.Columns["ProjectName"].Caption = "Project Name";

            gridView1.Columns["BookingNo"].VisibleIndex = 1;
            gridView1.Columns["Area"].VisibleIndex = 2;
            gridView1.Columns["Location"].VisibleIndex = 3;
            gridView1.Columns["CustomerName"].VisibleIndex = 4;
            gridView1.Columns["PE"].VisibleIndex = 5;
            gridView1.Columns["StartDate"].VisibleIndex = 6;
            gridView1.Columns["EndDate"].VisibleIndex = 7;
            gridView1.Columns["ProjectNo"].VisibleIndex = 8;
            gridView1.Columns["ProjectName"].VisibleIndex = 9;
            gridView1.Columns["Action"].VisibleIndex = 10;
        }

        public event Action? RentTimeSaved;

        private void ActionButton_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var row = gridView1.GetRow(gridView1.FocusedRowHandle) as RentTime; // FocusedRowHandle：目前選到的那一列
            if (row == null) return;                                // GetRow(handle)：把那一列的資料物件取出來（就是 RentTime）

            var form = new Project(_rentTimeService, _projectService, _jobNoService, row.RentTimeId);

            var dr = form.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                RentTimeSaved?.Invoke(); // 通知外面刷新
            }
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


    }
}
