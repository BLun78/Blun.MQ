using System;
using System.Collections.Generic;
using Blun.MQ.Controllers;
using Blun.MQ.Test.Demo;
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
            var controllerFactory = new ControllerFactory();

            // act
            var provider = new ControllerProvider(controllerFactory, null);

            // assert
            Assert.AreEqual(8, provider.Controllers.Count);
        }

        [TestMethod]
        public void ProviderGetControllerTypeOk()
        {
            // arrange
            var serviceProvider = new Mock<IServiceProvider>();
            var controllerFactory = new ControllerFactory();
            var provider = new ControllerProvider(controllerFactory, null);

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
            var controllerFactory = new ControllerFactory();
            var provider = new ControllerProvider(controllerFactory, null);
            var key = Guid.NewGuid().ToString();

            // act
            var type = provider.GetControllerType(key);

            // assert
        }
    }
}