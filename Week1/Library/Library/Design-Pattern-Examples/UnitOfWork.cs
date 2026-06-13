using Library.Repo;
using Serilog;

namespace Library.Design_Pattern_Examples;

public class UnitOfWork : IUnitOfWork
{
    public ILibraryRepository items { get; }

    private readonly List<string> _staged = new List<string>();
    
    //We are technically using Dependency Injection here.
    //We never initiate the ILibraryRepositoory object, we ask for an existing one.

    public UnitOfWork(ILibraryRepository items)
    {
        this.items = items;
    }

    public void Stage(string change)
    {
        _staged.Add(change); //staging a change
    }

    public int commit()
    {
        //Shallow commit implementation
        //We will just log how many things are staging at commit time?
        
        int count = _staged.Count;
        
        Log.Information("LibraryUnitOfwork commited {Count} staged change(s)", count);
        
        //Once the process end a clear is needed
        _staged.Clear();
        return count;
    }
}