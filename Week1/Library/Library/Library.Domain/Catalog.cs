namespace Library.Library.Domain;

public class Catalog
{
    //Backing our catalog theres going to be a list
    //List<T> ordered, grow dinamcally,accesible via indx
    //List<T> > array

    public  List<LibraryItem> _items = new();
    
    //Readonly just to restrict what the program can do with the proper list
    //Still it can be defined with private, public, protected properties

    public int Count => _items.Count;
    
    //Stack LIFO
    //We will model a return cart
    //Push put a item on the top of pile
    //Pop gets the last inserted item
    public readonly Stack<LibraryItem> _returncart = new();
    
    //Qeue FIFO
    //Method .queue() puts item in the back of the line
    //Method .dequeue() puts get item in the front of the line
    public readonly Queue<LibraryItem> _holdQueue = new();
    
    //Reading list
    //LinkedList, yeah that cursed stuff from c++ that has no index 
    //and has to be fully checked every time you need to find something
    public readonly LinkedList<LibraryItem> _readingList = new(); //Nightmare fuel
    
    

}