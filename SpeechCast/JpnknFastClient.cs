// Read-only MQTT subscription to the public jpnkn Fast interface. GPLv2.
// Endpoint documentation: https://doc.bbs.jpnkn.com/dev.html
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Protocol;

namespace SpeechCastMe
{
    public sealed class JpnknFastClient : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly string board, host;
        private readonly int port;
        private Task run;
        private int disposed;
        public event Action<JpnknFastMessage> ReplyReceived;
        public event Action<bool, string> ConnectionChanged;

        public JpnknFastClient(string board, string host = "bbs.jpnkn.com", int port = 1883)
        {
            JpnknFastTarget target;
            if (!JpnknFastTarget.TryParse(board, true, out target) || target.Board != board) throw new ArgumentException("Invalid board ID");
            this.board = board; this.host = host; this.port = port;
        }

        public void Start()
        {
            if (run != null || Volatile.Read(ref disposed) != 0) return;
            run = Task.Run(RunAsync);
        }

        private async Task RunAsync()
        {
            int failures = 0;
            var token = cancellation.Token;
            while (!token.IsCancellationRequested)
            {
                string reason = board;
                using (var connection = new MqttFactory().CreateMqttClient())
                {
                    var disconnected = new TaskCompletionSource<bool>();
                    connection.UseDisconnectedHandler(e => { disconnected.TrySetResult(true); });
                    connection.UseApplicationMessageReceivedHandler(e => {
                        if (token.IsCancellationRequested || e.ApplicationMessage.Retain) return;
                        var payload = e.ApplicationMessage.Payload;
                        if (payload == null || payload.Length > 1024 * 1024) return;
                        JpnknFastMessage message;
                        if (JpnknFastMessage.TryParse(e.ApplicationMessage.Topic, Encoding.UTF8.GetString(payload), out message) && ReplyReceived != null)
                            ReplyReceived(message);
                    });
                    try
                    {
                        bool alternate = host == "bbs.jpnkn.com" && failures % 2 == 1;
                        var options = new MqttClientOptionsBuilder().WithClientId("speechcastsp-" + Guid.NewGuid().ToString("N"))
                            .WithTcpServer(alternate ? "a.mq.jpnkn.com" : host, alternate ? 1884 : port)
                            .WithCredentials("genkai", "7144").WithCleanSession()
                            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30)).WithCommunicationTimeout(TimeSpan.FromSeconds(8)).Build();
                        await connection.ConnectAsync(options, token).ConfigureAwait(false);
                        var subscription = await connection.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(new MqttTopicFilterBuilder().WithTopic("bbs/" + board)
                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce).Build()).Build(), token).ConfigureAwait(false);
                        foreach (var item in subscription.Items)
                            if ((int)item.ResultCode >= 128) throw new InvalidOperationException("Fast購読が拒否されました。");
                        token.ThrowIfCancellationRequested();
                        if (ConnectionChanged != null) ConnectionChanged(true, "Fast 接続中：" + board);
                        failures = 0;
                        using (token.Register(() => disconnected.TrySetCanceled())) await disconnected.Task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex) { reason = board + "（" + ex.Message + "）"; }
                }
                if (token.IsCancellationRequested) break;
                failures++;
                if (ConnectionChanged != null) ConnectionChanged(false, "Fast 再接続待ち：" + reason);
                try { await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 3 * failures)), token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            cancellation.Cancel();
            if (run == null) cancellation.Dispose();
            else run.ContinueWith(t => cancellation.Dispose(), TaskScheduler.Default);
            // Connect/read operations observe cancellation; do not block the UI
            // waiting for a socket or a connection timeout during shutdown.
        }
    }
}
