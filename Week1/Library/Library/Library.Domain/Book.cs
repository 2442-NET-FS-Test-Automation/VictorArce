namespace Library.Library.Domain;

public class Book : LibrayItem, ILendable
{
    public int CopiesAvailable { get; private set; }
    
    //Child class constructor
    //First we implement the constructor with every parameter needed and then we add
    //that part with the ":" to send the values to our father class
    public Book(string title, string author, int copiesAvailable) : base(title, author)
    {
        CopiesAvailable = copiesAvailable;
    }
    
    //We need to fulfill the requiremnts of implementation so
    //we are gonna build our Describe method

    public override string Describe()
    {
        return $"{Id} - {Title} by {Author} has {CopiesAvailable} copies available";
    }
    
    public bool CheckOut()
    {
        if (CopiesAvailable > 0)
        {
            CopiesAvailable--;
            return true;
        }
        return false;
    }

    public void Return() => CopiesAvailable++;
}