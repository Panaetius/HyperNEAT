using HyperNeatLib.Factories;
using HyperNeatLib.Interfaces;

using Moq;

using NUnit.Framework;

namespace HyperNeatTest
{
    [TestFixture]
    public class HyperNeatTest
    {
        [Test]
        public void TestConnection()
        {
            var input = new Mock<INeuron>();
            var output = new Mock<INeuron>();

            input.Setup(t => t.Output).Returns(1);

            output.SetupSet(t => t.Input = 0.5);

            var connection = ConnectionFactory.CreateConnection(input.Object, output.Object, 0.5);

            Assert.NotNull(connection);

            connection.Calculate();

            output.VerifySet(o => o.Input = 0.5);
        }
    }
}
