using DevExpress.XtraEditors;
using System.Linq;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // 決定哪些按鈕狀態
        private void ApplyUiStatus()
        {
            bool isEdit = _editRentTimeId != null;
            bool isDraft = _uiStatus == UiRentStatus.Draft;
            bool isStarted = _uiStatus == UiRentStatus.Started;
            bool isFinished = _uiStatus == UiRentStatus.Finished;
            bool isSubmitToAssistant = _uiStatus == UiRentStatus.SubmittedToAssistant;
            // 只要完成(2) 或 已送出(3) 算「鎖定」
            bool isLocked = isFinished ||  isSubmitToAssistant;

            // 先處理標題/按鈕文字
            ApplyUiTextByStatus();

            if (!isLocked)
            {
                // 非鎖定：Draft / Started
                btnCreatedRentTime.Visible = true;

                btnCreatedRentTime.Enabled = true;                 // 建立/儲存修改
                btnRentTimeStart.Enabled = isEdit && !isStarted;   // Draft 才能開始
                btnRentTimeEnd.Enabled = isEdit && isStarted;      // Started 才能完成

                btnDeletedRentTime.Visible = isEdit;
                btnDeletedRentTime.Enabled = isEdit;               // 未完成可刪

                btnRestoreRentTime.Visible = isStarted;
                btnRestoreRentTime.Enabled = isStarted;            // 未完成可回復

                chkHandover.Visible = isEdit;
                chkHandover.Enabled = isEdit;

                btnCopyRentTime.Visible = isStarted;
                btnCopyRentTime.Enabled = false; // 未鎖定時先不給複製（你原本也是想完成後才複製）
            }
            else
            {
                //  鎖定：Finished / SubmittedToAssistant
                btnDeletedRentTime.Enabled = false;
                btnDeletedRentTime.Visible = false;

                btnRestoreRentTime.Enabled = false;
                btnRestoreRentTime.Visible = false;

                chkHandover.Visible = false;

                btnCopyRentTime.Visible = isEdit;
                btnCopyRentTime.Enabled = isEdit;

                if (isFinished)
                {
                    // Finished(2)：顯示三顆動作鍵
                    btnCreatedRentTime.Visible = true; // 列印
                    btnRentTimeStart.Visible = true;   // 上傳掃描影本
                    btnRentTimeEnd.Visible = true;     // 送出給助理

                    btnCreatedRentTime.Enabled = true;
                    btnRentTimeStart.Enabled = true;
                    btnRentTimeEnd.Enabled = true;
                }
                else
                {
                    // Submitted(3)： 隱藏三顆動作鍵
                    btnCreatedRentTime.Visible = false;
                    btnRentTimeStart.Visible = false;
                    btnRentTimeEnd.Visible = false;
                }
            }

            //  Finished(2) / Submitted(3) 都要鎖欄位
            SetFormEditable(!isLocked);

            // 只要不是 Draft，工程師就鎖住（你原本規則保留）
            cmbEngineer.Enabled = isDraft;
        }

        // 決定欄位能不能編輯
        private void SetFormEditable(bool editable)
        {
            cmbLocation.Properties.ReadOnly = !editable;
            cmbJobNo.Properties.ReadOnly = !editable;
            cmbCompany.Properties.ReadOnly = !editable || ShouldLockCompanyByJobNo();

            txtContactName.Properties.ReadOnly = !editable;
            txtContactPhone.Properties.ReadOnly = !editable;
            txtSales.Properties.ReadOnly = !editable || ShouldLockSalesByJobNo();
            txtSampleModel.Properties.ReadOnly = !editable;
            txtSampleNo.Properties.ReadOnly = !editable;

            startDateEdit.Properties.ReadOnly = !editable;
            endDateEdit.Properties.ReadOnly = !editable;
            startTimeEdit.Properties.ReadOnly = !editable;
            endTimeEdit.Properties.ReadOnly = !editable;

            chkHasLunch.Properties.ReadOnly = !editable;
            chkHasDinner.Properties.ReadOnly = !editable;
            cmbDinnerMinutes.Properties.ReadOnly = !editable;

            cmbEngineer.Properties.ReadOnly = !editable;
            cmbTestMode.Properties.ReadOnly = !editable;
            cmbTestItem.Properties.ReadOnly = !editable;

            memoTestInformation.Properties.ReadOnly = !editable;
            memoNote.Properties.ReadOnly = !editable;

            chkHandover.Properties.ReadOnly = !editable;

            // CreatedBy 通常永遠不可改（可留著鎖）
            txtCreatedBy.Properties.ReadOnly = true;
        }

        private bool ShouldLockCompanyByJobNo()
        {
            // JobNo 沒填：一定不鎖
            if (string.IsNullOrWhiteSpace(cmbJobNo.Text)) return false;

            // JobNo 有填：只有「API 有回客戶名稱」才鎖
            return _jobNoApiHasCustomer;
        }

        private bool ShouldLockSalesByJobNo()
        {
            // JobNo 沒填：一定不鎖
            if (string.IsNullOrWhiteSpace(cmbJobNo.Text)) return false;

            // JobNo 有填：只有「API 有回客戶名稱」才鎖
            return _jobNoApiHasSales;
        }

        private void ApplyUiTextByStatus()
        {
            bool isFinished = _uiStatus == UiRentStatus.Finished;

            if (isFinished)
            {
                var full = _loadedRentTime?.BookingNo.Trim();

                // full 例：RF-0000123-1  或 TMP-0000123-1
                // 目標：只留下 RF-0000123 或 TMP-0000123
                string bookingMain;

                if (!string.IsNullOrWhiteSpace(full) && full.Contains("-"))
                {
                    var parts = full.Split('-');    // ["RF","0000123","1"]
                    bookingMain = string.Join("-", parts.Take(parts.Length - 1)); // "RF-0000123"
                }
                else
                {
                    // DB沒有就退回UI主號
                    bookingMain = !string.IsNullOrWhiteSpace(txtBookingNo.Text)
                        ? txtBookingNo.Text.Trim()
                        : (full ?? "");
                }

                this.Text = $"檢視租時單 - Booking No. {bookingMain}";

                btnCreatedRentTime.Text = "列印";
                btnRentTimeStart.Text = "上傳掃描影本";
                btnRentTimeEnd.Text = "送出給助理";
            }
            else
            {
                // 還原成原本流程用字
                this.Text = _editRentTimeId == null ? "新增租時單" : "編輯租時單";
                btnCreatedRentTime.Text = _editRentTimeId == null ? "建立租時單" : "儲存修改";
                btnRentTimeStart.Text = "租時開始";
                btnRentTimeEnd.Text = "租時完成";
            }
        }
    }
}
