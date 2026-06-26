using System.Text.Json;
using Serilog;

namespace Library.Library.Domain;

public class OpenLibraryClient
{
    //There must be only ONE HTTPClient for the whole process
    //Otherwise we will have multiple connections and that will cause sockets to leak
    //and cause a SocketException
    
    private static readonly HttpClient _client = new();
    
    //We are going to write a Async methdo
    //An async method is any method that calls Aysnc code
    //Like .FindAsync(); or "await"
    //That inmediatly catalog our methods as Async

    public async Task<LibraryItem> FetchByIsbnAync(string isbn)
    {
        //We are going to use a string to hold the target url
        string url = $"https://openlibrary.org/search.json?q=isbn:{isbn}&fields=titel,author_name&limit=1";

        try
        {
            //Since we are tring to comunicate with a remote source we need to wrap this on a try catch
            //Whenever we call an async method we need to await the result
            string jsonResponse = await _client.GetStringAsync(url);


            //We need to build our own parser to parse the json response because
            //Someone didnt make it
            return Parse(jsonResponse);
        }
        catch (HttpRequestException ex)
        {
            Log.Warning("Error fetch failed for {isb}  : {Message}", isbn, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning("Error fetch failed {Message}",  ex.Message);
            return null;
        }
        
        
    }

    public static LibraryItem? Parse(string json)
    {
        Dictionary<string, JsonElement> resp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        if (resp == null || !resp.TryGetValue("docs", out JsonElement docs) || docs.GetArrayLength() == 0)
        {
            return null;
        }
        JsonElement FoundBook = docs[0];
        
        string title = FoundBook.GetProperty("title").GetString() ?? "Untitled";
        
        string author = "Unknown";

        if (FoundBook.TryGetProperty("author_name", out JsonElement authors))
        {
            author = authors[0].GetString() ?? "Unknown";
        }
        
        return LibraryItemFactory.create(ItemKind.Book, title, author);
        
        return new Book(resp["title"].GetString(), resp["author_name"].GetString(), 0);
        //return JsonSerializer.Deserialize<LibraryItem>(json);
    }
}