namespace Week3Project.Api.Fulfillment;

public interface IFulfillmentService
{
    Task ProcessBurstAsync(IReadOnlyList<int> ids, CancellationToken ctk, bool useParallel = false);
    
    Task MicroPlasticBurstAsync(IReadOnlyList<int> ids, CancellationToken cts, bool useParallel = false);
}