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

        Task DeleteAsync(int rentTimeId, string user, CancellationToken ct = default);

        Task SubmitToAssistantAsync(int rentTimeId, string user, CancellationToken ct = default);

        Task<bool> ChangeDraftPeriodWithSplitAsync(int rentTimeId, DateTime newStart, DateTime newEnd, string user, CancellationToken ct = default);

        Task<RentTime?> GetByIdAsync(int rentTimeId, CancellationToken ct = default);
        
        Task<CreateRentTimeResult> CreateAsync(RentTime model, long? bookingBatchId = null, CancellationToken ct = default);
        
        Task UpdateAsync(int rentTimeId, RentTime model, string user, CancellationToken ct = default);
        
        Task StartAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task FinishAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task RestoreAsync(int rentTimeId, string user, CancellationToken ct = default);
        
        Task<CreateRentTimeResult> CopyAsync(int rentTimeId, bool isHandOver, string user, CancellationToken ct = default);

        Task<long> CreateBookingBatchAsync(CancellationToken ct = default);

    }
}
