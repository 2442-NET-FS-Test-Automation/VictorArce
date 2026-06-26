namespace Library.Data.Entities;

public enum status
{
    //In my application if an order is yet to be processed it is pending
    //Fullfilled means the sale completed bbackorder happens when someone
    //places a buy request we dont have stock for
    
    Pending,
    Fulfilled,
    Backordered
}