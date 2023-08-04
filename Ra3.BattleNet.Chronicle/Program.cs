// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

Console.WriteLine("Hello, World!");

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger("Chat");

var config = JsonSerializer.Deserialize<ChatConfig>(File.ReadAllText("config.json"))
    ?? throw new NullReferenceException("Cannot deserialize config");

var sessions = new ConcurrentDictionary<RoomIdentifier, ConcurrentQueue<ChatItem>>();

var server = new TcpListener(IPAddress.Any, config.Port);
server.Start();
while (true)
{
    try
    {
        var client = await server.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleConnection(client));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in main loop");
    }
}

async Task HandleConnection(TcpClient tcpClient)
{
    try
    {
        using var client = tcpClient; // 这样的写法便于自动释放资源
        using var stream = client.GetStream();
        var roomIdentifier = await JsonSerializer.DeserializeAsync<RoomIdentifier>(stream);
        if (roomIdentifier == null)
        {
            logger.LogError("Received null room identifier");
            return;
        }
        var chatQueue = sessions.GetOrAdd(roomIdentifier, new ConcurrentQueue<ChatItem>());

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in connection loop");
    }
}

record ChatConfig(int Port, string MongoDbConnectionString);
record RoomIdentifier(string MatchId, int Timestamp, int Sd);
record ChatItem(string Type, string Value, int LogicalFrame, bool IsConfirmed = false, int SequenceNumber = -1);
