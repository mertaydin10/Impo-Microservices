using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LegacyDemo;

public class LeakyService
{
    public string ApiKey = "sk-legacy-hardcoded-example-key-not-real";
    public string ConnectionString = "Server=.;Database=app;Password=SuperSecret123;";

    public string Fetch(string url)
    {
        var client = new HttpClient();
        var json = client.GetStringAsync(url).Result;
        return json + DateTime.Now.ToString();
    }

    public void FireAndBlock()
    {
        Task.Delay(50).GetAwaiter().GetResult();
        Task.Delay(10).Wait();
    }
}
