using System;
using System.Collections.Generic;
using Blun.MQ.Controllers;
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
            var serviceCollection = new Mock<IServiceCollection>();

            // act
            var provider = new ControllerProvider(serviceCollection.Object);

            // assert
            Assert.AreEqual(8, provider.Controllers.Count);
        }

        [TestMethod]
        public void ProviderGetControllerTypeOK()
        {
            // arrange
            var serviceCollection = new Mock<IServiceCollection>();
            var provider = new ControllerProvider(serviceCollection.Object);

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
            var serviceCollection = new Mock<IServiceCollection>();
            var provider = new ControllerProvider(serviceCollection.Object);
            var key = Guid.NewGuid().ToString();

            // act
            var type = provider.GetControllerType(key);

            // assert
        }
    }
}
