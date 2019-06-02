using System;
using System.Collections.Generic;
using System.Text;
using Blun.MQ.Controllers;
using Blun.MQ.Hosting;
using Blun.MQ.Messages;
using Blun.MQ.Test.Demo;
using Microsoft.Extensions.DependencyInjection;
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
            var scope = new Mock<IServiceScope>();
            var logger = new Mock<ILogger<DemoController>>() { DefaultValue = DefaultValue.Mock, };
            var demoController = new DemoController(logger.Object);
            serviceProvider.Setup(x => x.GetService(typeof(DemoController))).Returns(demoController);
            
            var controllerFactory = new ControllerFactory();

            // act
            var controller = controllerFactory.CreateController(scope.Object, typeof(DemoController), new MQContext());

            // assert
            Assert.AreEqual(typeof(DemoController), controller.GetType());
        }
    }
}
