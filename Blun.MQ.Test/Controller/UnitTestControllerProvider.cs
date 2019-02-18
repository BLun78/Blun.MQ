using System;
using System.Collections.Generic;
using Blun.MQ.Hosting;
using Blun.MQ.Test.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Blun.MQ.Test.Controller
{
    [TestClass]
    public class UnitTestControllerProvider
    {
        [TestMethod]
        public void ProviderLoadController()
        {
            // arrange
            var serviceProvider = new Mock<IServiceProvider>();
            var controllerFactory = new ControllerFactory(serviceProvider.Object);

            // act
            var provider = new ControllerProvider(controllerFactory);

            // assert
            Assert.AreEqual(8, provider.Controllers.Count);
        }

        [TestMethod]
        public void ProviderGetControllerTypeOK()
        {
            // arrange
            var serviceProvider = new Mock<IServiceProvider>();
            var controllerFactory = new ControllerFactory(serviceProvider.Object);
            var provider = new ControllerProvider(controllerFactory);

            // act
            var type = provider.GetControllerType("Demo.HelloWorld");

            // assert
            Assert.AreEqual(typeof(DemoController), type);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void ProviderGetControllerTypeExeption()
        {
            // arrange
            var serviceProvider = new Mock<IServiceProvider>();
            var controllerFactory = new ControllerFactory(serviceProvider.Object);
            var provider = new ControllerProvider(controllerFactory);
            var key = Guid.NewGuid().ToString();

            // act
            var type = provider.GetControllerType(key);

            // assert
        }
    }
}
