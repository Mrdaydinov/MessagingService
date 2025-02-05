using MessagingService.Models;
using Npgsql;

namespace MessagingService.DAL
{
    public class MessageRepository : IMessageRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<MessageRepository> _logger;

        public MessageRepository(IConfiguration configuration, ILogger<MessageRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task AddMessageAsync(Message message)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "INSERT INTO messages (content, created_at, sequence_number) VALUES (@content, @created_at, @sequence_number)";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("content", message.Content);
            command.Parameters.AddWithValue("created_at", message.CreatedAt);
            command.Parameters.AddWithValue("sequence_number", message.SequenceNumber);

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Добавлено сообщение: {@Message}", message);
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(DateTime from, DateTime to)
        {
            var messages = new List<Message>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT id, content, created_at, sequence_number FROM messages WHERE created_at BETWEEN @from AND @to ORDER BY created_at";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    Id = reader.GetInt32(0),
                    Content = reader.GetString(1),
                    CreatedAt = reader.GetDateTime(2),
                    SequenceNumber = reader.GetInt32(3)
                });
            }
            _logger.LogInformation("Получено {Count} сообщений", messages.Count);
            return messages;
        }
    }
}
