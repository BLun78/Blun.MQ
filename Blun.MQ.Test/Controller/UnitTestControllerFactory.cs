using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Blun.MQ.Hosting;
using Blun.MQ.Message;
using Blun.MQ.Test.Demo;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Blun.MQ.Test.Controller
{
    [TestClass]
    public class UnitTestControllerFactory
    {
        [TestMethod]
        public void ProviderLoadController()
        {
            // arrange
            var serviceProvider = new Mock<IServiceProvider>();
            var loggerFactory = new Mock<ILoggerFactory>() { DefaultValue = DefaultValue.Mock, };
            var demoController = new DemoController(loggerFactory.Object);
            serviceProvider.Setup(x => x.GetService(typeof(ILoggerFactory))).Returns(loggerFactory.Object);
            serviceProvider.Setup(x => x.GetService(typeof(DemoController))).Returns(demoController);
            var controllerFactory = new ControllerFactory(serviceProvider.Object);

            // act
            var controller = controllerFactory.GetController(typeof(DemoController), new MQContext());

            // assert
            Assert.AreEqual(typeof(DemoController), controller.GetType());
        }
    }
}
