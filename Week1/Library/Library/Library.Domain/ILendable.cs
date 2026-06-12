namespace Library.Library.Domain;

//Interfaces in C# - they are a contract for behaviors 
public interface ILendable
{
    //Just declaration of signature methods
    //No bodies nor access modifiers
    bool CheckOut();

    void Return();
}