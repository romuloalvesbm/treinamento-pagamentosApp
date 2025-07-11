using Microsoft.EntityFrameworkCore;
using PagamentosApp.API.Models;

namespace PagamentosApp.API.Contexts
{
    /// <summary>
    /// Classe para configuração do EntityFramework
    /// </summary>
    public class DataContext : DbContext
    {
        /// <summary>
        /// Construtor para injeção de dependência
        /// </summary>
        /// <param name="options">Parâmetros para conexão do EntityFramework</param>
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}

