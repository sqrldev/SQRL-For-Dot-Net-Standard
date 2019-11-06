using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace UITests
{
    [TestClass]
    public class LoginTests
    {

        private FirefoxDriver _driver;

        [TestInitialize]
        public void SetUp()
        {
            _driver = new FirefoxDriver(new FirefoxOptions()
            {
                PageLoadStrategy = PageLoadStrategy.Normal
            });
        }

        [TestCleanup]
        public void Shutdown()
        {
            _driver.Quit();
        }

    }
}
