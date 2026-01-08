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

            gridView1.Columns["Action"].VisibleIndex = 9;

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

            gridView1.Columns["BookingNo"].VisibleIndex = 0;
            gridView1.Columns["Area"].VisibleIndex = 1;
            gridView1.Columns["Location"].VisibleIndex = 2;
            gridView1.Columns["CustomerName"].VisibleIndex = 3;
            gridView1.Columns["PE"].VisibleIndex = 4;
            gridView1.Columns["StartDate"].VisibleIndex = 5;
            gridView1.Columns["EndDate"].VisibleIndex = 6;
            gridView1.Columns["ProjectNo"].VisibleIndex = 7;
            gridView1.Columns["ProjectName"].VisibleIndex = 8;
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

        private void gridControl1_Click(object sender, EventArgs e)
        {

        }
    }
}
