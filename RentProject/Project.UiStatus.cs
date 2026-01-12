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
            bool isStarted = _uiStatus == UiRentStatus.Started;
            bool isFinished = _uiStatus == UiRentStatus.Finished;

            // 先處理標題/按鈕文字
            ApplyUiTextByStatus();

            if (!isFinished)
            {
                // 原本狀態邏輯
                btnCreatedRentTime.Enabled = true;                 // 建立/儲存修改
                btnRentTimeStart.Enabled = isEdit && !isStarted;   // Draft 才能開始
                btnRentTimeEnd.Enabled = isEdit && isStarted;      // Started 才能完成
                btnDeletedRentTime.Enabled = isEdit;               // 未完成可刪
                btnRestoreRentTime.Enabled = isEdit;               // 未完成可回復
                btnCopyRentTime.Enabled = isEdit && isFinished;    // 這行其實永遠 false，但先保留你原本的樣子也行
            }
            else
            {
                // Finished：只能檢視 + 允許三個新動作
                btnCreatedRentTime.Enabled = true;   // 列印
                btnRentTimeStart.Enabled = true;     // 上傳掃描影本
                btnRentTimeEnd.Enabled = true;       // 送出給助理


                btnDeletedRentTime.Enabled = false;  // 完成後不能刪
                btnDeletedRentTime.Visible = false;
                btnRestoreRentTime.Enabled = false;  // 完成後不能回復（依你需求）
                btnRestoreRentTime.Visible = false;
                chkHandover.Visible = false;
                btnCopyRentTime.Enabled = isEdit;    // 完成後可複製（你原本也是這個想法）
            }

            // Finished：只能檢視
            SetFormEditable(!isFinished);

            // 只要不是 Draft（Started / Finished）工程師就鎖住
            bool lockEngineer = _uiStatus != UiRentStatus.Draft;

            cmbEngineer.Enabled = !lockEngineer;

            // 原本額外鎖的欄位保留（雖然 SetFormEditable 已鎖，這段可留）
            if (_uiStatus == UiRentStatus.Finished)
            {
                startDateEdit.Properties.ReadOnly = true;
                startTimeEdit.Properties.ReadOnly = true;
            }
        }

        // 決定欄位能不能編輯
        private void SetFormEditable(bool editable)
        {
            cmbLocation.Properties.ReadOnly = !editable;
            cmbJobNo.Properties.ReadOnly = !editable;
            cmbCompany.Properties.ReadOnly = !editable;

            txtContactName.Properties.ReadOnly = !editable;
            txtContactPhone.Properties.ReadOnly = !editable;
            txtSales.Properties.ReadOnly = !editable;
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

        private void ApplyUiTextByStatus()
        {
            bool isFinished = _uiStatus == UiRentStatus.Finished;

            if (isFinished)
            {
                var full = _loadedRentTime?.BookingNo.Trim();

                // full 例：RF-0000123-1  或 TMP-0000123-1
                // 目標：只留下 RF-0000123 或 TMP-0000123
                string bookingMain;

                if (string.IsNullOrWhiteSpace(full) && full.Contains("-"))
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
