using DevExpress.XtraEditors;
using System.Linq;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // H) Init：填下拉選單
        // =========================================================
        private void InitTestModeCombo()
        {
            var modes = _tests
            .Select(x => x.TestMode)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

            cmbTestMode.Properties.Items.Clear();
            cmbTestMode.Properties.Items.AddRange(modes);

            cmbTestMode.EditValue = null;
            cmbTestMode.SelectedIndex = -1;

            cmbTestItem.Properties.Items.Clear();
            cmbTestItem.EditValue = null;
            cmbTestItem.SelectedIndex = -1;

            cmbTestItem.EditValue = false;
        }

        private void UpdateTestItem(string mode)
        {
            var items = _tests
            .Where(x => x.TestMode == mode)
            .Select(x => x.TestItem)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

            cmbTestItem.Properties.Items.Clear();
            cmbTestItem.Properties.Items.AddRange(items);

            cmbTestItem.SelectedIndex = -1;
        }

        private void InitEngineerCombo()
        {
            cmbEngineer.Properties.Items.Clear();
            cmbEngineer.Properties.Items.AddRange(_engineers);

            // 新增時先不要預設任何人
            cmbEngineer.EditValue = null;
            cmbEngineer.SelectedIndex = -1;
            cmbEngineer.Text = "";
        }

        private void InitDinnerMinutesCombo()
        {
            cmbDinnerMinutes.Properties.Items.Clear();
            cmbDinnerMinutes.Properties.Items.AddRange(new object[] { 30, 60, 90, 120, 150, 180, 210, 240 });

            // 預設值
            cmbDinnerMinutes.EditValue = 60;
        }
    }
}
