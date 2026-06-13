using Library.Exceptions;

namespace Library.Library.Domain;


//this class is static so it can only contain static members
//Cannot be instantiated
//Cannot be inherited from
public static class LibraryItemFactory
{
    //Our class is responsible for creating LibraryItems of any type
    //We will use enum hhere to make sure users ONLY attempt to create valid types

    public static LibraryItem create(
        ItemKind kind,
        string name,
        string author,
        int copies = 1,
        string section = "General")
    {
        //This method is going to use a switch to call the correct constructor
        switch (kind)
        {
            case ItemKind.Book:
                return new Book(name, author, copies);
            case ItemKind.ReferenceBook:
                return new ReferenceBook(name, author, section);
            case ItemKind.Magazine:
                return new Magazine(name, author, copies, "NA");
            default: //Why do this need to be implemented anyways?
                throw new LibraryException($"Unknown kind: {kind}");
        }
    }
}