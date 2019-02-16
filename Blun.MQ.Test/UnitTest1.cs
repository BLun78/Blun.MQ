using Blun.MQ.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blun.MQ.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // arrange
            var provider = new ControllerProvider();

            // act
            provider.LoadControllers();

            // assert
            Assert.AreEqual(8,provider.Controllers.Count);
        }
    }
}
