namespace PagamentosApp.API.Models
{
    /// <summary>
    /// Entidade para modelar os dados de saída que serão
    /// enviados / transmitidos pelo PRODUCER para a mensageria
    /// </summary>
    public class OutboxMessage
    {
        public required Guid Id { get; set; } = Guid.NewGuid();
        public required DateTime DataHoraCriacao { get; set; } = DateTime.Now;
        public required DateTime? DataHoraTransmissao { get; set; } = null;
        public required string Evento { get; set; }
        public required string Mensagem { get; set; }
        public required bool Transmitido { get; set; } = false;
    }
}

