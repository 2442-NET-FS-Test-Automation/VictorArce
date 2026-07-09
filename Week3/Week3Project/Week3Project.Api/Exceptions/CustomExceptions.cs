namespace Week3Project.Api.Exceptions;

public class CustomExceptions : Exception
{
    //String to catch the SKU in the exception
    public string sku { get; }

    //Yeah i didnt want to practically use the same exception for the DB but i didnt find a good point
    //to use a different one
    
    public CustomExceptions(string sku) : base($"Error SKU: {sku} not found")
    {
        this.sku = sku;
    }
}