namespace PagamentosApp.API.Models
{
    /// <summary>
    /// Modelo de dados para a entidade Pedido
    /// </summary>
    public class Pagamento
    {
        public required Guid Id { get; set; } = Guid.NewGuid();
        public required string NomePagador { get; set; }
        public required DateTime DataPagamento { get; set; }
        public required string FormaPagamento { get; set; }
        public required string Status { get; set; }
        public required string Observacoes { get; set; }
    }
}

