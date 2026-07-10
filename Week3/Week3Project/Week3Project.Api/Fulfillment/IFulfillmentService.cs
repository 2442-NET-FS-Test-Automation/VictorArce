namespace Week3Project.Api.Fulfillment;

public interface IFulfillmentService
{
    //public Task<BurstResult> FulfillBurstAsync(List<int> orderIds, CancellationToken ct);
    Task ProcessBurstAsync(IReadOnlyList<int> orderIds, CancellationToken cts);
    
    Task MicroPlasticBurstAsync(IReadOnlyList<int> ids, CancellationToken cts);
}