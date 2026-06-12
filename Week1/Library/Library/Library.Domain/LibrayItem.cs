namespace Library.Library.Domain;

//Abstarct class that cannot be instantiated
//Still need a constructor for child classes but it cannot be called here
public abstract class LibrayItem
{
    public string? Title { get; private set; }
    public string? Author { get; private set; }
    private static int _nextId = 1; //Underscore its mandatory declaring static properties
    public int Id {get;} //No setter so it cannot be modified

    //Protected because its going to be inherted by other classes
    protected LibrayItem(string title, string author)
    {
        Id = _nextId++;
        Title = title;
        Author = author;
    }


    //Abstract method, only signature without a body
    public abstract string Describe();
    
    //Abstract classes can contain concrete 
    //potentially our child will implement override and use it for ToString method
    public override  string ToString() => Describe();
    
    //Concrete methods have a body and must be overriden
    public virtual string ShelfLabel()
    {
        return $"{Id}: {Title}";
    }
}