using System.Linq;
using System.Reflection;
using Xunit;

namespace UnitTest.Common
{
    public class BuildSmokeTests
    {
        [Fact]
        public void MainAssemblyLoadsWithoutReflectionTypeLoadException()
        {
            var asm = Assembly.Load("WebAPIDevSecOpsScallingSDD");
            Assert.NotNull(asm);
        }

        [Fact]
        public void MainAssemblyContainsExpectedTypes()
        {
            var asm = Assembly.Load("WebAPIDevSecOpsScallingSDD");
            var types = asm.GetTypes();
            Assert.Contains(types, t => t.Name == "Program");
        }
    }
}
