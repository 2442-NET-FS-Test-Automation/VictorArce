namespace Library.Library.Domain;

public class OldBook
{
    public string? Title { get; private set; }
    public string? Author { get; private set; }
    public int? CopiesAvailable { get; private set; }
    public string? Editorial { get; set; }
    public int? Year { get; set; }

    //Properties can be static too
    private static int _nextId = 1; //Underscore its mandatory declaring static properties
    public int Id {get;} //No stter so it cannot be modified

    public OldBook(string title, string author, int copiesAvailable, string editorial, int year)
    {
        Title = title;
        Author = author;
        CopiesAvailable = copiesAvailable;
        Editorial = editorial;
        Year = year;
        Id = _nextId++; //Assign the next ID and increment the static counter
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

    public void ReturnBook() => CopiesAvailable++;

    //Overriding a Tostring
    public override string ToString()
    {
        //Any of the following options is corrct to use but the fisrts one its building on the compilator
        //return base.ToString();
        return $"Book: {Title} by {Author}, {CopiesAvailable} copies available";
    }
}