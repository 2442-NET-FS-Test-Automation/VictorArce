//Here we implemente the actual store info using serilog

using Library.Library.Domain;
using Serilog;

namespace Library.Repo;

public class InMemoryLibraryRepository : ILibraryRepository
{
    //Because we dont have an outside store of info (like sql)
    //we are going to rely on a list

    private readonly List<LibraryItem> _items = new List<LibraryItem>();
    
    public void Add(LibraryItem item)
    {
        _items.Add(item);
        //We just added a new item - thats a significant event so we are going to log it
        //String interpolation is not used when calling Serilog because it have its own template for strings
        Log.Information("Added {Title} id: {Id}", item.Title, item.Id);
    }

    public LibraryItem Get(int id)
    {
        //This is the same as doing a foreach looping trought every element but its the built in method
        LibraryItem? item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null)
        {
            Log.Warning("Lookup failed for id {Id}", id);
            throw new KeyNotFoundException($"Library item with id {id} not found");
        }

        return item;
    }

    public List<LibraryItem> GetAll()
    {
        //Return a copy of the list for security reasons 
        return _items.ToList();
        
    }

    public bool Remove(int id)
    {
        foreach (LibraryItem item in _items)
        {
            if (item.Id == id)
            {
                _items.Remove(item);
                Log.Information("Removed item with id {Id}", id);
                return true;
            }
        }
        Log.Information("Removal filed for item with id {Id}", id);
        return false;
    }
}