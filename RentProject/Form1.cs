using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors;
using RentProject.Domain;
using RentProject.Repository;
using RentProject.Service;
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

    }
}
