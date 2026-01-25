using System;
using System.Net.Http;
using System.Threading.Tasks;
using DevExpress.XtraEditors;
using RentProject.Shared.Http;

namespace RentProject.UI
{
    public static class UiSafeRunner
    {
        public static void SafeRun(Action action, string caption = "提示", Action<bool>? setLoading = null)
        {
            try
            {
                // 如果呼叫端有提供 setLoading，就先把 loading 打開
                setLoading?.Invoke(true);
                action();
            }
            catch (TaskCanceledException) // 逾時/取消（HttpClient 常用）
            {
                XtraMessageBox.Show("連線逾時或操作已取消。", caption);
            }
            catch (OperationCanceledException)
            {
                // 使用者取消：不當錯誤（不提示）
            }
            catch (ApiException ex)
            {
                ShowApiError(ex, caption);
            }
            catch (HttpRequestException)
            { 
                XtraMessageBox.Show("無法連線，請確認網路或伺服器狀態。", caption);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}：{ex.Message}", caption);
            }
            finally
            {
                setLoading?.Invoke(false);
            }
        }

        public static async Task SafeRunAsync(Func<Task> action, string caption = "提示", Action<bool>? setLoading = null)
        {
            try
            {
                setLoading?.Invoke(true);
                await action(); // 不要 ConfigureAwait(false)，讓回到 UI 執行緒更穩
            }
            catch (TaskCanceledException) // 逾時/取消（HttpClient 常用）
            {
                XtraMessageBox.Show("連線逾時或操作已取消。", caption);
            }
            catch (OperationCanceledException)
            {
                // 使用者取消：不當錯誤（不提示）
            }
            catch (ApiException ex)
            {
                ShowApiError(ex, caption);
            }
            catch (HttpRequestException)
            {
                XtraMessageBox.Show("無法連線，請確認網路或伺服器狀態。", caption);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.GetType().Name}：{ex.Message}", caption);
            }
            finally
            {
                setLoading?.Invoke(false);
            }
        }

        private static void ShowApiError(ApiException ex, string caption)
        {
            string msg;

            if (ex.StatusCode >= 500)
            {
                msg = "系統忙碌或發生錯誤，請稍後再試。";
                if (!string.IsNullOrWhiteSpace(ex.TraceId))
                    msg += $"\n\nTraceId: {ex.TraceId}";
            }
            else
            {
                msg = ex.Message;
                if (!string.IsNullOrWhiteSpace(ex.TraceId) && !msg.Contains(ex.TraceId))
                    msg += $"\n\nTraceId: {ex.TraceId}";
            }

            XtraMessageBox.Show(msg, caption);
        }
    }
}
