namespace Library.Library.Domain;

//Structs are small bundles of data with no identity
//they look kind of like classes but they are VALUE types
//meaning - 2 structs of the same type with the same data are identical
//Comparing them with mmethod .equals() it gets true

public readonly struct ShelfLocation
{
    public int Aisle { get; }
    public int Shelf { get; }
    
    public ShelfLocation(int aile, int shelf)
    {
        Aisle = aile;
        Shelf = shelf;
    }

    public override string ToString()
    {
        return $"Aisle: {Aisle}, Shelf: {Shelf}"; 
    }
}