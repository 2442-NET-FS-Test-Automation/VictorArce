namespace Library.Exceptions;

public class ItemNotAvailableException : LibraryException
{
    public ItemNotAvailableException(string message) : base($"{message} has no copies available to borrow.")
    {
        
    }
}