using System;
using System.IO;

namespace ContractTest
{
    internal static class FixturePaths
    {
        internal static readonly string[] Files =
        {
            "ping.json",
            "segusuario.json",
            "cliente.json",
            "producto.json",
            "estado-venta.json",
            "clientes-paged.json",
            "clientes-autocomplete.json",
            "login-response.json",
            "refresh-response.json",
            "pedido.json",
            "pago.json",
            "venta.json",
            "dashboard.json",
            "error-404.json",
        };

        internal static string Directory
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ContractTest.csproj")))
                {
                    dir = dir.Parent;
                }

                if (dir == null)
                {
                    throw new InvalidOperationException("ContractTest.csproj no encontrado.");
                }

                var fixtures = Path.Combine(dir.FullName, "Fixtures");
                if (!System.IO.Directory.Exists(fixtures))
                {
                    System.IO.Directory.CreateDirectory(fixtures);
                }

                return fixtures;
            }
        }
    }
}
