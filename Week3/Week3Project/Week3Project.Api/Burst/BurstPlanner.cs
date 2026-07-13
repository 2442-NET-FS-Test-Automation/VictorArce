using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Week3Project.Data;
using Week3Project.Data.Entities;
using Week3Project.Data.Enum;

namespace Week3Project.Api.Fulfillment;


public class BurstPlanner : IBurstPlanner
{ 
    /// <summary>
    /// Evaluates a collection of purchase orders and sorts their unique database IDs 
    /// based strictly on their shipping priority level using an in-memory priority queue.
    /// </summary>
    /// <param name="orders">The stream of raw purchase order records pulled from the database or seeder.</param>
    /// <returns>A read-only ordered list of integer IDs ready to be processed by the fulfillment engine.</returns>
    public IReadOnlyList<int> OrderByPriority(IEnumerable<PurchaseOrder> orders)
    {
        // Instantiate a min-priority queue (lower numeric values take precedence over higher numbers)
        // Element: int (The unique PurchaseOrder Id)
        // Priority: int (The scheduling priority rating weight)
        PriorityQueue<int, int> pq = new PriorityQueue<int, int>();

        // Enqueue loop: Catalog each order ID into the binary heap bucket data structure
        foreach (PurchaseOrder o in orders)
        {
            // Note: In a standard .NET PriorityQueue, the lowest priority value is dequeued first.
            // Assigning '0' to SpeedPlus (High Priority) ensures it moves directly to the front of the execution lane,
            // while assigning '1' to normal orders places them comfortably behind the express traffic.
            pq.Enqueue(o.Id, o.Priority == OrderPriority.SpeedPlus ? 0 : 1);
        }
            
        // Dequeue loop: Construct our output tracking array matching the sorted timeline
        var orderedByPriority = new List<int>();
        
        // Continuously pop the top element out of the min-heap until the queue is fully cleared
        while (pq.TryDequeue(out int id, out _))
        {
            // The item is added to the list in absolute priority sequence
            orderedByPriority.Add(id);
        }

        // Return the prioritized list of execution IDs to the underlying caller
        return orderedByPriority;
    }
}