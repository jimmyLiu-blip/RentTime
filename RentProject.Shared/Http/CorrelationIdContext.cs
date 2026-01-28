namespace RentProject.Shared.Http
{
    /// <summary>
    /// 用來保存「同一次 UI 操作」的 CorrelationId。
    /// AsyncLocal：可讓 async/await 鏈路中的呼叫都拿到同一個值。
    /// 使用者「按一次按鈕」→ 可能呼叫 3 支 API → 這 3 支 API 都用同一個 ID
    /// 這樣你在 WebAPI log 只要查這個 ID，就能看到整個操作流程。
    /// </summary>
    public static class CorrelationIdContext
    {
        // AsyncLocal 是一種「跟著 async/await 流程走的變數」
        private static readonly AsyncLocal<string?> _current = new();

        public static string? Current => _current.Value;

        /// <summary>
        /// 開啟一個新的操作範圍 CorrelationId。
        /// 建議：每次按鈕操作（SafeRun / SafeRunAsync）包一次。
        /// </summary>
        public static IDisposable BeginNew()
        {
            var prev = _current.Value;
            _current.Value = Guid.NewGuid().ToString("N"); // Guid.NewGuid().ToString("N") 會得到 32 碼、沒有破折號的字串
            return new Scope(() => _current.Value = prev);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Action _onDispose;

            private bool _disposed;

            // _onDispose() 是在 BeginNew() 裡塞進來的那個動作：_current.Value = prev
            public Scope(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose();
            }
        }
    }
}
