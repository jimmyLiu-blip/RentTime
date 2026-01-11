using DevExpress.XtraEditors;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // 驗證場地必填
        private bool ValidateLocationUI()
        {
            var location = cmbLocation.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(location))
            {
                dxErrorProvider1.SetError(cmbLocation, "場地名稱必填");

                lblLocationRequired.Visible = true;

                cmbLocation.Focus();

                return false;
            }

            dxErrorProvider1.SetError(cmbLocation, "");
            lblLocationRequired.Visible = false;
            return true;
        }


        // 驗證客戶名稱必填，回傳true代表通過驗證
        private bool ValidateCompanyUI()
        {
            var company = cmbCompany.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(company))
            {
                // 1. 顯示 DevExpress 錯誤圖示 + 提示文字
                dxErrorProvider1.SetError(cmbCompany, "客戶名稱必填");

                // 2. 顯示你做的「欄位下方紅字」
                lblCompanyRequired.Visible = true;

                // 3. 直接跳到欄位
                cmbCompany.Focus();

                return false;
            }

            dxErrorProvider1.SetError(cmbCompany, "");
            lblCompanyRequired.Visible = false;
            return true;
        }

        // 驗證業務必填
        private bool ValidateSalesUI()
        {
            var sales = txtSales.Text?.Trim();

            if (string.IsNullOrWhiteSpace(sales))
            {
                dxErrorProvider1.SetError(txtSales, "業務必填");

                lblSalesRequired.Visible = true;

                txtSales.Focus();

                return false;
            }

            dxErrorProvider1.SetError(txtSales, "");
            lblSalesRequired.Visible = false;
            return true;
        }

        private bool ValidateStartDateUI()
        {
            var startDate = startDateEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(startDate))
            {
                dxErrorProvider1.SetError(startDateEdit, "開始日期必填");

                lblStartDateRequired.Visible = true;

                startDateEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(startDateEdit, "");
            lblStartDateRequired.Visible = false;
            return true;
        }

        private bool ValidateEndDateUI()
        {
            var endDate = endDateEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(endDate))
            {
                dxErrorProvider1.SetError(endDateEdit, "結束日期必填");

                lblEndDateRequired.Visible = true;

                endDateEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(endDateEdit, "");
            lblEndDateRequired.Visible = false;
            return true;
        }

        private bool ValidateStartTimeUI()
        {
            var startTime = startTimeEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(startTime))
            {
                dxErrorProvider1.SetError(startTimeEdit, "開始時間必填");

                lblStartTimeRequired.Visible = true;

                startTimeEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(startTimeEdit, "");
            lblStartTimeRequired.Visible = false;
            return true;
        }

        private bool ValidateEndTimeUI()
        {
            var endTime = endTimeEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(endTime))
            {
                dxErrorProvider1.SetError(endTimeEdit, "結束時間必填");

                lblEndTimeRequired.Visible = true;

                endTimeEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(endTimeEdit, "");
            lblEndTimeRequired.Visible = false;
            return true;
        }
    }
}
