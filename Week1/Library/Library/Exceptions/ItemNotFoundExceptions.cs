namespace Library.Exceptions;

public class ItemNotFoundExceptions : LibraryException
{
    //We can hold the offending Id that triggered exception
    public int Id { get; }
    public ItemNotFoundExceptions(string message, int id) : base($"No library item with id {id}")
    {
        Id = id;
    }
}