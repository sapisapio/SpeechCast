// Local MQTT integration test and optional read-only live connection check. GPLv2.
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MQTTnet;
using MQTTnet.Server;
using SpeechCastMe;

class FastIntegrationTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }
    static bool Wait(Func<bool> condition, int milliseconds = 10000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.ElapsedMilliseconds < milliseconds) Thread.Sleep(20);
        return condition();
    }
    static string Payload(string thread, int number)
    {
        return "{\"bbsid\":\"sapio\",\"threadkey\":\"" + thread + "\",\"no\":\"" + number + "\",\"body\":\"名無し<>sage<>2026/10/19(月) 00:00:00<>ローカル受信テスト<>\"}";
    }
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--live")
        {
            int connected = 0; string lastStatus = "";
            using (var client = new JpnknFastClient("sapio"))
            {
                client.ConnectionChanged += (ok, status) => { lastStatus = status; if (ok) Interlocked.Increment(ref connected); };
                client.Start();
                Check(Wait(() => Volatile.Read(ref connected) > 0, 25000), "live jpnkn Fast subscription acknowledged (read only): " + lastStatus);
            }
            return;
        }
        var listener = new TcpListener(IPAddress.Loopback,0);
        listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var options = new MqttServerOptionsBuilder().WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
            .WithDefaultEndpointPort(port).Build();
        using (var server = new MqttFactory().CreateMqttServer())
        {
            server.StartAsync(options).GetAwaiter().GetResult();
            server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/sapio").WithPayload(Payload("100",1)).WithRetainFlag().Build()).GetAwaiter().GetResult();
            int connections = 0;
            var messages = new ConcurrentQueue<JpnknFastMessage>();
            using (var client = new JpnknFastClient("sapio","127.0.0.1",port))
            {
                client.ConnectionChanged += (ok,status) => { if (ok) Interlocked.Increment(ref connections); };
                client.ReplyReceived += message => messages.Enqueue(message);
                client.Start();
                Check(Wait(() => Volatile.Read(ref connections) == 1), "client connects and subscribes to board topic");
                Thread.Sleep(150);
                Check(messages.Count == 0, "retained history is not replayed as new comments");
                server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/sapio").WithPayload(Payload("100",50)).Build()).GetAwaiter().GetResult();
                server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/sapio").WithPayload(Payload("200",50)).Build()).GetAwaiter().GetResult();
                Check(Wait(() => messages.Count == 2), "same board subscription receives new replies from two threads");
                JpnknFastMessage first, second; messages.TryDequeue(out first); messages.TryDequeue(out second);
                Check(first.Key != second.Key && first.Number == second.Number, "original thread identity distinguishes equal response numbers");
                server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/sapio").WithPayload("{broken").Build()).GetAwaiter().GetResult();
                server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/other").WithPayload(Payload("100",51)).Build()).GetAwaiter().GetResult();
                Thread.Sleep(150);
                Check(messages.Count == 0, "invalid payload and other boards do not enter the feed");
                server.StopAsync().GetAwaiter().GetResult();
                server.StartAsync(options).GetAwaiter().GetResult();
                Check(Wait(() => Volatile.Read(ref connections) >= 2,15000), "client reconnects and resubscribes after broker interruption");
                server.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("bbs/sapio").WithPayload(Payload("300",1)).Build()).GetAwaiter().GetResult();
                Check(Wait(() => messages.Count == 1), "new comments arrive after reconnection");
            }
            server.StopAsync().GetAwaiter().GetResult();
        }
    }
}
