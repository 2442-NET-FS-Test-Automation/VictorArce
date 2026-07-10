using System.Collections.Generic;
using Week3Project.Data.Entities;

namespace Week3Project.Api.Seeder;

public interface ISeeder
{
    IReadOnlyList<int> Seed(int i, bool expedited);

    void Restock();

    void StockToTarget(int i);
}