using Week3Project.Data.Entities;

namespace Week3Project.Api.Fulfillment;

public interface IBurstPlanner
{
    IReadOnlyList<int> OrderByPriority(IEnumerable<PurchaseOrder> orders);
}