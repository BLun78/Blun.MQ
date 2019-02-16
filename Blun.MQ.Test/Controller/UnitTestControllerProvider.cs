using Blun.MQ.Controller;
using Blun.MQ.Test.Demo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blun.MQ.Test
{
    [TestClass]
    public class UnitTestControllerProvider
    {
        [TestMethod]
        public void ProviderLoadController()
        {
            // arrange
            
            // act
            var provider = new ControllerProvider();

            // assert
            Assert.AreEqual(8,provider.Controllers.Count);
        }

        [TestMethod]
        public void ProviderGetControllerType()
        {
            // arrange
            var provider = new ControllerProvider();

            // act
            var type = provider.GetControllerType("Demo.HelloWorld");

            // assert
            Assert.AreEqual(typeof(DemoController), type);
        }
    }
}
