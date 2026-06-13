namespace Library.Library.Domain;
//Just for demo this is going to be a custom generic class
//Yeah the ones with the <T> in the class declaration

public class Shelf<T> //Spooky stuff
{
    private readonly T[] _slots;
    private int _used;

    public Shelf(int slots)
    {
        _slots = new T[slots];
    }
    
    //Exposing just the needed things
    public int Capacity => _slots.Length;
    public int count => _used;
    
    //Method to add items to the shelf
    public bool TryAdd(T item)
    {
        if (_used == _slots.Length)
        {
            Console.WriteLine("Shelf already full");
            return false;
        }
        //We add in the next empty slot and increase thee counter to keep track 
        //of used slots 
        _slots[_used+1] = item;
        _used++;
        Console.WriteLine("Item added successfully");
        return true;
    }

    //Search for an item trought index 
    public T Get(int index)
    {
        return _slots[index];
    }
}