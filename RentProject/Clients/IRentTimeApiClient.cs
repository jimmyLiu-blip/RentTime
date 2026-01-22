using RentProject.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RentProject.Clients
{
    public interface IRentTimeApiClient
    {
        Task<List<RentTime>> GetProjectViewListAsync(CancellationToken ct = default);

        Task DeleteRentTimeByIdAsync(int rentTimeId, string user, CancellationToken ct = default);

        Task SubmitToAssistantByIdAsync(int rentTimeId, string user, CancellationToken ct = default);

        Task<bool> ChangeDraftPeriodWithSplitAsync(int rentTimeId, DateTime newStart, DateTime newEnd, string user, CancellationToken ct = default);

        Task<RentTime?> GetByIdAsync(int rentTimeId, CancellationToken ct = default);
        
        Task<CreateRentTimeResult> CreateRentTimeFromApiAsync(RentTime model, long? bookingBatchId = null, CancellationToken ct = default);
        
        Task UpdateRentTimeFromApiAsync(int rentTimeId, RentTime model, string user, CancellationToken ct = default);
        
        Task StartRentTimeFromApiAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task FinishRentTimeFromApiAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task RestoreToDraftByIdAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task<CreateRentTimeResult> CopyRentTimeByIdAsync(int rentTimeId, bool isHandOver, string user, CancellationToken ct = default);

        Task<long> CreateBookingBatchAsync(CancellationToken ct = default);

        Task<string> PingDBAsync(CancellationToken ct = default);
    }
}
