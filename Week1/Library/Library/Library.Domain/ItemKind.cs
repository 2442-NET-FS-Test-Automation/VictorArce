namespace Library.Library.Domain;
//This is going to be a enum
//Its a custom value type where we basically enumerate posible values ahead of time

public enum ItemKind
{
    //Possible values for instance of this kind of enum
    //This enum can only be the included values in this
    //case Book, ReferenceBook and Magazine
    Book,
    ReferenceBook,
    Magazine
}