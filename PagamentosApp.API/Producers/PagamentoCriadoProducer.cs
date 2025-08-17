
using Microsoft.EntityFrameworkCore;
using PagamentosApp.API.Contexts;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;

namespace PagamentosApp.API.Producers
{
    /// <summary>
    /// Classe para ler a tabela de saída (Outbox Message)
    /// e enviar os eventos do tipo 'pagamento_criado'
    /// que não tenham sido transmitidos para a fila.
    /// </summary>
    public class PagamentoCriadoProducer(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //enquanto não receber uma solicitação de cancelamento do processo
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    //Criando objetos por injeção de dependência
                    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<PagamentoCriadoProducer>>();
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    //Consultando os registros da tabela de outbox
                    var eventos = await context.OutboxMessages
                                        .Where(om => om.Evento.Equals("pagamento_criado")
                                                 && !om.Transmitido)
                                        .OrderBy(om => om.DataHoraCriacao)
                                        .Take(10) //10 primeiros registros
                                        .ToListAsync();

                    //percorrendo os registros obtidos
                    foreach (var item in eventos)
                    {
                        try
                        {
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
                            using var connection = await factory.CreateConnectionAsync();
                            using var channel = await connection.CreateChannelAsync();

                            //criando / acessando a fila
                            await channel.QueueDeclareAsync(
                                queue: configuration.GetSection("RabbitMQ:Queue").Value, //nome da fila
                                durable: true, //fila que mesmo se o servidor for reiniciado nã terá os dados apagados
                                exclusive: false, //fila que pode ser acessada por outras aplicações
                                autoDelete: false, //mensagens não são removidas automaticamente
                                arguments: null
                                );

                            //serializar os dados que serão gravados na fila para bytes
                            var payload = Encoding.UTF8.GetBytes(item.Mensagem);

                            //gravando a mensagem na fila
                            await channel.BasicPublishAsync(
                                exchange: "",
                                routingKey: configuration.GetSection("RabbitMQ:Queue").Value,
                                body: payload
                                );

                            //Atualizando o status do evento como transmitido
                            item.DataHoraTransmissao = DateTime.Now;
                            item.Transmitido = true;

                            context.Update(item); //salvando as atualizações
                            context.SaveChanges(); //confirmando..

                            logger.LogInformation($"Pagamento criado foi enviado para a fila com sucesso ({item.Mensagem}).");
                        }
                        catch (Exception e)
                        {
                            logger.LogInformation($"Falha ao enviar pagamento criado para a fila ({item.Mensagem}), Erro: {e.Message}.");
                        }
                    }
                }

                //Criando um deplay de 15 segundos..
                await Task.Delay(15000, stoppingToken);
            }
        }
    }
}