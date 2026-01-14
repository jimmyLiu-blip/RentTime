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

                cmbLocation.Focus();

                return false;
            }

            dxErrorProvider1.SetError(cmbLocation, "");
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

                // 2. 直接跳到欄位
                cmbCompany.Focus();

                return false;
            }

            dxErrorProvider1.SetError(cmbCompany, "");
            return true;
        }

        // 驗證業務必填
        private bool ValidateSalesUI()
        {
            var sales = txtSales.Text?.Trim();

            if (string.IsNullOrWhiteSpace(sales))
            {
                dxErrorProvider1.SetError(txtSales, "業務必填");

                txtSales.Focus();

                return false;
            }

            dxErrorProvider1.SetError(txtSales, "");

            return true;
        }

        private bool ValidateStartDateUI()
        {
            var startDate = startDateEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(startDate))
            {
                dxErrorProvider1.SetError(startDateEdit, "開始日期必填");

                startDateEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(startDateEdit, "");

            return true;
        }

        private bool ValidateEndDateUI()
        {
            var endDate = endDateEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(endDate))
            {
                dxErrorProvider1.SetError(endDateEdit, "結束日期必填");

                endDateEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(endDateEdit, "");

            return true;
        }

        private bool ValidateStartTimeUI()
        {
            var startTime = startTimeEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(startTime))
            {
                dxErrorProvider1.SetError(startTimeEdit, "開始時間必填");

                startTimeEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(startTimeEdit, "");

            return true;
        }

        private bool ValidateEndTimeUI()
        {
            var endTime = endTimeEdit.Text?.Trim();

            if (string.IsNullOrWhiteSpace(endTime))
            {
                dxErrorProvider1.SetError(endTimeEdit, "結束時間必填");

                endTimeEdit.Focus();

                return false;
            }

            dxErrorProvider1.SetError(endTimeEdit, "");

            return true;
        }
    }
}
