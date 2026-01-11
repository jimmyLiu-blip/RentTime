using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraEditors;
namespace RentProject
{
    public partial class Project: XtraForm
    {
        // 取得表單內所有控制
        private static IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls) // 把表單裡「所有控制項」都找出來（包含巢狀）
            {
                // 先吐出「這一層」看到的控制項
                yield return c; // yield return：一個一個吐出結果(不用先把全部放到List才回傳)

                // 再去吐出「c 裡面」的控制項
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }

        // 反射設定 TabStop （確認這個家具有沒有「開關」可以關 Tab；有我才關，沒有就不碰它）
        private static void SetTabStop(Control c, bool value)
        {
            var prop = c.GetType().GetProperty("TabStop");

            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
                prop.SetValue(c, value);
        }

        // 套用某條「Tab 路線」：先全部關，再只開你指定的順序
        private void ApplyTabSequence(params Control[] sequence)
        {
            // 先全部跳過 Tab（避免不該停的欄位被停到）
            foreach (var c in GetAllControls(this))
                SetTabStop(c, false);

            // 再把這條路線上的欄位依序打開 + 排 TabIndex
            for (int i = 0; i < sequence.Length; i++)
            {
                var ctl = sequence[i];
                if (ctl == null) continue;

                SetTabStop(ctl, true);
                ctl.TabIndex = i;
            }

            // 焦點進到第一個欄位
            if (sequence.Length > 0 && sequence[0] != null)
                this.ActiveControl = sequence[0];
        }

        private void ApplyTabByStatus()
        {
            bool isCreate = _editRentTimeId == null;

            // Finished：通常是檢視，不需要填欄位，就把 Tab 都關掉，避免游標亂跑
            if (_uiStatus == UiRentStatus.Finished)
            {
                ApplyTabSequence(
                btnCreatedRentTime,   // 列印
                btnRentTimeStart,     // 上傳掃描影本
                btnRentTimeEnd,       // 送出給助理
                btnCopyRentTime       // 複製
                 );
                return;
            }

            if (_uiStatus == UiRentStatus.Started)
            {
                ApplyTabSequence(
                cmbLocation,
                cmbCompany,
                txtSales,
                txtContactName,
                txtContactPhone,
                memoTestInformation,

                startDateEdit,
                endDateEdit,
                startTimeEdit,
                endTimeEdit,
                chkHasLunch,
                chkHasDinner,
                cmbDinnerMinutes,

                cmbJobNo,
                txtSampleModel,
                txtSampleNo,

                cmbTestMode,
                cmbTestItem,
                memoNote,

                chkHandover,

                btnCreatedRentTime,  // 儲存修改（或建立）
                btnRentTimeEnd       // 租時完成
            );
                return;
            }

            // Draft：新增/編輯各自一套（你說你要分開）
            if (isCreate)
            {
                // 新增 Draft
                ApplyTabSequence(
                    cmbLocation,
                    cmbCompany,
                    txtSales,

                    startDateEdit,
                    endDateEdit,
                    startTimeEdit,
                    endTimeEdit,

                    btnCreatedRentTime // 建立租時單
                );
            }
            else
            {
                // 編輯 Draft
                ApplyTabSequence(
                    cmbLocation,
                    cmbCompany,
                    txtSales,
                    txtContactName,
                    txtContactPhone,
                    memoTestInformation,

                    cmbJobNo,

                    txtSampleModel,
                    txtSampleNo,

                    cmbTestMode,
                    cmbTestItem,
                    memoNote,

                    btnCreatedRentTime,  // 儲存修改
                    btnRentTimeStart,    // 租時開始
                    btnRestoreRentTime,  // 回復
                    btnDeletedRentTime  // 刪除
                );
            }
        }
    }
}
