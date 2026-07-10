using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Week3Project.Data;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;

//I almost did a 1 to 1 implementation
public class BurstPlanner : IBurstPlanner
{ 
    // Tu método existente incorporado limpiamente
    public IReadOnlyList<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        PriorityQueue<int, int> pq = new PriorityQueue<int, int>();

        foreach (PurchaseOrder o in orders)
            pq.Enqueue(o.Id, o.Priority == OrderPriority.SpeedPlus ? 0 : 1);
            
        var orderedByPriority = new List<int>();
        while (pq.TryDequeue(out int id, out _))
        {
            orderedByPriority.Add(id);
        }

        return orderedByPriority;
    }
}