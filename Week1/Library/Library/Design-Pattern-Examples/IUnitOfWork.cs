using Library.Repo;

namespace Library.Design_Pattern_Examples;

public interface IUnitOfWork
{
    //This is a property
    ILibraryRepository items { get; }

    //Methods
    void Stage(string change);
    
    int commit();
}