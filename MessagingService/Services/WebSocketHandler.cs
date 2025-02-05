using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace MessagingService.Services
{
    public class WebSocketHandler
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly ILogger<WebSocketHandler> _logger;

        public WebSocketHandler(ILogger<WebSocketHandler> logger)
        {
            _logger = logger;
        }


        public async Task HandleAsync(HttpContext context, WebSocket webSocket)
        {
            string socketId = Guid.NewGuid().ToString();
            _sockets.TryAdd(socketId, webSocket);
            _logger.LogInformation("WebSocket подключен. ID: {SocketId}", socketId);

            try
            {
                await ReceiveLoopAsync(socketId, webSocket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в обработке WebSocket-соединения. ID: {SocketId}", socketId);
            }
            finally
            {
                _sockets.TryRemove(socketId, out _);
                _logger.LogInformation("WebSocket отключён. ID: {SocketId}", socketId);
            }
        }

        private async Task ReceiveLoopAsync(string socketId, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Клиент инициировал закрытие WebSocket. ID: {SocketId}", socketId);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие соединения", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в потоке WebSocket. ID: {SocketId}", socketId);
            }
        }


        public async Task BroadcastMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var encodedMessage = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task>();

            foreach (var pair in _sockets)
            {
                var socket = pair.Value;
                if (socket.State == WebSocketState.Open)
                {
                    _logger.LogInformation("Отправка сообщения WebSocket-клиенту. ID: {SocketId}", pair.Key);
                    tasks.Add(socket.SendAsync(
                        new ArraySegment<byte>(encodedMessage, 0, encodedMessage.Length),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None));
                }
                else
                {
                    _logger.LogWarning("WebSocket-клиент неактивен, удаляем. ID: {SocketId}", pair.Key);
                    _sockets.TryRemove(pair.Key, out _);
                }
            }

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation("Рассылка сообщения: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при рассылке сообщения по WebSocket.");
            }
        }
    }
}
