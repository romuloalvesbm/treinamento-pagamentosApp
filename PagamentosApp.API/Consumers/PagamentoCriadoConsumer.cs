using PagamentosApp.API.Producers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PagamentosApp.API.Consumers
{
    /// <summary>
    /// Classe para ler as mensagens contidas na fila
    /// </summary>
    public class PagamentoCriadoConsumer(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<PagamentoCriadoProducer>>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                //Conexão com o RabbitMQ
                var factory = new ConnectionFactory
                {
                    HostName = configuration.GetSection("RabbitMQ:Host").Value,
                    Port = int.Parse(configuration.GetSection("RabbitMQ:Port").Value),
                    UserName = configuration.GetSection("RabbitMQ:User").Value,
                    Password = configuration.GetSection("RabbitMQ:Pass").Value,
                    VirtualHost = configuration.GetSection("RabbitMQ:VHost").Value
                };

                //conectando e criando / acessando a fila
                var connection = await factory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                //criando / acessando a fila
                await channel.QueueDeclareAsync(
                    queue: configuration.GetSection("RabbitMQ:Queue").Value, //nome da fila
                    durable: true, //fila que mesmo se o servidor for reiniciado nã terá os dados apagados
                    exclusive: false, //fila que pode ser acessada por outras aplicações
                    autoDelete: false, //mensagens não são removidas automaticamente
                    arguments: null
                    );

                //criando o consumer para fazer a leitura da fila
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (sender, args) =>
                {
                    var payload = Encoding.UTF8.GetString(args.Body.ToArray());
                    await channel.BasicAckAsync(args.DeliveryTag, false);

                    logger.LogInformation($"Pagamento criado foi lido da fila com sucesso ({payload}).");
                };

                //executando o consumer
                await channel.BasicConsumeAsync(
                    queue: configuration.GetSection("RabbitMQ:Queue").Value,
                    autoAck: false,
                    consumer: consumer
                );
            }
        }
    }
}


