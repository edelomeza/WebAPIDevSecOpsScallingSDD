using System;
using System.IO;
using System.Text.Json;

namespace ContractTest
{
    public class JsonContractTests
    {
        [Fact]
        public void AllExpectedFixturesExist()
        {
            foreach (var name in FixturePaths.Files)
            {
                Assert.True(File.Exists(Path.Combine(FixturePaths.Directory, name)), "Falta fixture: " + name);
            }
        }

        [Fact]
        public void AllKeysFollowNamingConvention()
        {
            foreach (var name in FixturePaths.Files)
            {
                string body = File.ReadAllText(Path.Combine(FixturePaths.Directory, name));
                using var doc = JsonDocument.Parse(body);
                AssertPascalCase(doc.RootElement, name + "$");
            }
        }

        [Fact]
        public void NoSecretsLeaked()
        {
            string[] forbidden = { "strpwd", "2fasecreto", "password" };
            foreach (var name in FixturePaths.Files)
            {
                string body = File.ReadAllText(Path.Combine(FixturePaths.Directory, name));
                foreach (var word in forbidden)
                {
                    Assert.DoesNotContain(word, body, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // Convención real del repo (AGENTS.md §1): prefijos legacy en minúsculas
        // (str/int/dec/dte/bln, p. ej. strNombreCliente) + resto PascalCase
        // (RowVersion, TotalCount); `id` solo o seguido de sufijo PascalCase
        // (idCliCliente, idProProducto).
        private static bool IsConventional(string name)
        {
            if (name == "id" || (name.StartsWith("id", StringComparison.Ordinal) && name.Length > 2 && char.IsUpper(name[2])))
            {
                return true;
            }

            if (char.IsUpper(name[0]))
            {
                return true;
            }

            string[] prefixes = { "str", "int", "dec", "dte", "bln" };
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal) && name.Length > prefix.Length && (char.IsUpper(name[prefix.Length]) || char.IsDigit(name[prefix.Length])))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertPascalCase(JsonElement element, string path)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    Assert.False(string.IsNullOrEmpty(property.Name));
                    Assert.True(IsConventional(property.Name), "Clave fuera de convención en " + path + ": " + property.Name);
                    AssertPascalCase(property.Value, path + "." + property.Name);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    AssertPascalCase(item, path + "[" + index + "]");
                    index++;
                }
            }
        }
    }
}
