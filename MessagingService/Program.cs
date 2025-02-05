using MessagingService.DAL;
using MessagingService.Services;

namespace MessagingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{AppDomain.CurrentDomain.BaseDirectory}MessagingService.xml";
                c.IncludeXmlComments(xmlFile);
            });

            builder.Services.AddScoped<IMessageRepository, MessageRepository>();
            builder.Services.AddSingleton<WebSocketHandler>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            };
            app.UseWebSockets(webSocketOptions);

            app.UseStaticFiles();

            app.UseRouting();

            app.Use(async (context, next) =>
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Входящий запрос: {Method} {Path}", context.Request.Method, context.Request.Path);

                await next();

                logger.LogInformation("Исходящий ответ: {StatusCode}", context.Response.StatusCode);
            });

            app.MapControllers();
            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
                    await webSocketHandler.HandleAsync(context, webSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.UseAuthorization();

            app.Run();
        }
    }
}
