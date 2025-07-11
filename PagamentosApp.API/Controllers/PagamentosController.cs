using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using PagamentosApp.API.Contexts;
using PagamentosApp.API.Models;

namespace PagamentosApp.API.Controllers
{
    /// <summary>
    /// Controlador para serviços de pagamentos.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PagamentosController(DataContext context, ILogger<PagamentosController> logger) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(PagamentoResponse), 201)]
        public IActionResult Post([FromBody] PagamentoRequest request)
        {
            IDbContextTransaction? transaction = null;

            try
            {
                transaction = context.Database.BeginTransaction();

                var pagamento = new Pagamento()
                {
                    Id = Guid.NewGuid(),
                    DataPagamento = request.DataPagamento,
                    FormaPagamento = request.FormaPagamento,
                    NomePagador = request.NomePagador,
                    Status = "Aguardando processamento",
                    Observacoes = "Pagamento capturado com sucesso."
                };

                context.Pagamentos.Add(pagamento);
                context.SaveChanges();

                var outboxMessage = new OutboxMessage()
                {
                    Id = Guid.NewGuid(),
                    DataHoraCriacao = DateTime.Now,
                    DataHoraTransmissao = null,
                    Evento = "pagamento_criado",
                    Mensagem = JsonConvert.SerializeObject(pagamento),
                    Transmitido = false
                };

                context.OutboxMessages.Add(outboxMessage);
                context.SaveChanges();

                transaction.Commit();

                logger.LogInformation($"Pagamento cadastrado com sucesso ({JsonConvert.SerializeObject(pagamento)}).");

                return StatusCode(201, new PagamentoResponse(
                    pagamento.Id,
                    pagamento.NomePagador,
                    0m,
                    pagamento.DataPagamento,
                    pagamento.FormaPagamento,
                    pagamento.Status,
                    pagamento.Observacoes
                    ));
            }
            catch (Exception e)
            {
                transaction?.Rollback();

                logger.LogError($"Falha ao cadastrar pagamento ({JsonConvert.SerializeObject(request)}).");

                return StatusCode(422, new
                {
                    message = "Não foi possível processar o pagamento: " + e.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO para definir os dados da requisição de pagamento.
    /// </summary>
    public record PagamentoRequest(
        string NomePagador, //nome do pagador
        decimal Valor, //valor a ser pago
        DateTime DataPagamento, //data do pagamento
        string FormaPagamento //forma de pagamento
        );

    /// <summary>
    /// DTO para definir os dados da resposta de pagamento.
    /// </summary>
    public record PagamentoResponse(
        Guid Id, //identificador do pagamento
        string NomePagador, //nome do pagador
        decimal Valor, //valor a ser pago
        DateTime DataPagamento, //data do pagamento
        string FormaPagamento, //forma de pagamento
        string Status, //status do pagamento
        string Observacoes //observações do pagamento
        );
}



