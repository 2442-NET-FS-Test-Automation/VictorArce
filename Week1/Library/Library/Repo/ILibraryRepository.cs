using Library.Library.Domain;

namespace Library.Repo;

public interface ILibraryRepository
{
    //This interface is an abstraction of an actual repository class
    //Lets think of things we want to be able to do againts our library store of information
    
    //Create Read Update Delete (CRUD) its like the bare minimum here  
    
    //Create item
    void Add(LibraryItem item); //If we put the parent class we can insert any class that inherits from it
    
    //Read item
    LibraryItem Get(int id); //Return an item and if it doesn't find it throws an exception for it
    List<LibraryItem> GetAll();
    
    //Update item
    
    
    //Delete Item
    bool  Remove(int id); //Recieves id of object to remove
}