using Week3Project.Data.Entities;

namespace Week3Project.Api.Fulfillment;

/// <summary>
/// Defines the architectural contract for sorting incoming order batches.
/// Responsible for determining the exact sequence in which orders must pass through the fulfillment engine.
/// </summary>
public interface IBurstPlanner
{
    /// <summary>
    /// Analyzes a batch of raw purchase orders and applies business priority weights 
    /// to return a structured sequence of database IDs ready for safe processing.
    /// </summary>
    /// <param name="orders">The collection of purchase orders evaluating for prioritization metrics.</param>
    /// <returns>An immutable, ordered list of database IDs representing the optimal fulfillment path.</returns>
    IReadOnlyList<int> OrderByPriority(IEnumerable<PurchaseOrder> orders);
}