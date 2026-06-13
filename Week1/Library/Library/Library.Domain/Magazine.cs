namespace Library.Library.Domain;

public class Magazine : LibraryItem, ILendable
{
    public int CirculationsCopies { get; private set; }
    public string Publisher {  get; private set; }

    public Magazine(string title, string author, int circulationsCopies, string publisher) 
        : base(title, author)
    {
        CirculationsCopies = circulationsCopies;
        Publisher = publisher;
    }

    public override string Describe()
    {
        return $"{Title} Magazine, published by {Publisher}";
    }

    //Providing implementation via new instead of override
    //Has implication for later
    //This is technically methd hiding
    //Depends on the reference type
    //LybraryItem magname = new Magazine(...) - call LibraryIItem ShelfLabbel
    //This is most likely not what you want
    //new vs override - very different behavior
    public new String ShelfLabel()
    {
        return $"Mag-{Id}: {Title}";
    }

    public bool CheckOut()
    {
        if (CirculationsCopies == 0)
        {
            return false;
        }
        CirculationsCopies--;
        return true;
    }

    public void Return() => CirculationsCopies++;
}