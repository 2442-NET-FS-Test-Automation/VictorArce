namespace Week3Project.Api.Fulfillment;

public interface IFulfillmentService
{
    public Task<BurstResult> FulfillBurstAsync(List<int> orderIds, CancellationToken ct);
}