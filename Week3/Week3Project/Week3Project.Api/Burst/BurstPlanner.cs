using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;


//I almost did a 1 to 1 implementation 
public class BurstPlanner
{
    
    // Method to plan fulfillment order
    public IReadOnlyList<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        PriorityQueue<int, int> pq = new PriorityQueue<int, int>();

        foreach (PurchaseOrder o in orders)
            pq.Enqueue(o.Id, o.Priority == OrderPriority.Expedited ? 0 : 1);
        var orderedByPriority = new List<int>();
        while (pq.TryDequeue(out int id, out _))
        {
            orderedByPriority.Add(id);
        }

        return orderedByPriority; // expedited ids should be first in the list

    } 

}