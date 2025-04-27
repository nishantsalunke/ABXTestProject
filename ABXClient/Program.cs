using ABXClient.Services;

class Program
{
    static void Main(string[] args)
    {
        var client = new ABXClientService("127.0.0.1", 3000);
        client.RunClient();
    }
}