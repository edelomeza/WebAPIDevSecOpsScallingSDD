using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPIDevSecOpsScallingSDD.Context;
using Xunit;
using ClienteModel = WebAPIDevSecOpsScallingSDD.Models.CliCliente;

namespace UnitTest.DbContext
{
    public class DbContextTests
    {
        [Fact]
        public void DbContextShouldInitializeWithSqlServer()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=Dummy;User Id=sa;Password=dummy;TrustServerCertificate=True")
                .Options;

            using var context = new AppDbContext(options);

            Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        }

        [Fact]
        public void DbContextShouldInitializeAndAssignRowVersionWithInMemory()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("DbContextTests")
                .Options;

            using var context = new AppDbContext(options);
            var cliente = new ClienteModel
            {
                strNombreCliente = "Test",
                strDireccionCliente = "Dir",
                strCorreoElectronico = "test@example.com",
                strNumeroTelefono = "1234567890",
                strCreadoPorUsuario = "tester",
            };

            context.CliClientes.Add(cliente);
            context.SaveChanges();

            Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
            Assert.NotNull(cliente.RowVersion);
            Assert.Equal(new byte[] { 1 }, cliente.RowVersion);
            Assert.NotNull(context.CliClientes.AsNoTracking().Single().RowVersion);
        }
    }
}
