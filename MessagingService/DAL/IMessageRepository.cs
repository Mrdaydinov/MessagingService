using MessagingService.Models;

namespace MessagingService.DAL
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(Message message);
        Task<IEnumerable<Message>> GetMessagesAsync(DateTime from, DateTime to);
    }
}
