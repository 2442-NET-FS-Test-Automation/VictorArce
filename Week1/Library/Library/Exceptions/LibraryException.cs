namespace Library.Exceptions;

//Just adding the exception class we are making a custom exception class 
public class LibraryException : Exception
{
    //Base constructor
    public LibraryException(string message) : base(message)
    {
        
    }
    
    
}