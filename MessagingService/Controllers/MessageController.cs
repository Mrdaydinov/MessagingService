using MessagingService.DAL;
using MessagingService.Models;
using MessagingService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MessagingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageRepository _repository;
        private readonly ILogger<MessageController> _logger;
        private readonly WebSocketHandler _webSocketHandler;

        public MessageController(IMessageRepository repository,
                                 ILogger<MessageController> logger,
                                 WebSocketHandler webSocketHandler)
        {
            _repository = repository;
            _logger = logger;
            _webSocketHandler = webSocketHandler;
        }

        /// <summary>
        /// Отправляет сообщение в систему.
        /// </summary>
        /// <param name="input">Объект сообщения с контентом и порядковым номером.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Сообщение успешно отправлено.</response>
        /// <response code="400">Некорректные данные в запросе.</response>
        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] MessageInputModel input)
        {
            if (string.IsNullOrEmpty(input.Content) || input.Content.Length > 128)
            {
                return BadRequest("Длина сообщения должна быть от 1 до 128 символов");
            }

            var message = new Message
            {
                Content = input.Content,
                SequenceNumber = input.SequenceNumber,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddMessageAsync(message);
            _logger.LogInformation("Получено сообщение: {@Message}", message);

            var messageJson = JsonSerializer.Serialize(new
            {
                message.Id,
                message.Content,
                CreatedAt = message.CreatedAt.ToString("o"),
                message.SequenceNumber
            });
            await _webSocketHandler.BroadcastMessageAsync(messageJson);

            return Ok();
        }

        /// <summary>
        /// Получает список сообщений за указанный диапазон дат.
        /// </summary>
        /// <param name="from">Начальная дата (UTC).</param>
        /// <param name="to">Конечная дата (UTC).</param>
        /// <returns>Список сообщений.</returns>
        /// <response code="200">Успешный запрос.</response>
        /// <response code="400">Некорректные параметры запроса.</response>
        [HttpGet("range")]
        public async Task<IActionResult> GetMessages([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var messages = await _repository.GetMessagesAsync(from, to);
            return Ok(messages);
        }
    }
}