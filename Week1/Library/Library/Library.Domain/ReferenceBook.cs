namespace Library.Library.Domain;

public class  ReferenceBook : LibraryItem
{
    public string Section{ get;}

    public ReferenceBook(string title, string author, string section) : base(title, author)
    {
        Section = section;
    }

    public override string Describe()
    {
        return $"{Id}: {Title} by {Author} -- Reference only, {Section} section.";
    }
    
    //Overriding ShelfLabel
    //This is a TRUE override
    public override string ShelfLabel()
    {
        return $"REF - {Id}: {Title} by {Section}";
    }
}