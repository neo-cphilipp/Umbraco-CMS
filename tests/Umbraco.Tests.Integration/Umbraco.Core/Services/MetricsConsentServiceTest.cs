using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Core.Services
{
    [TestFixture]
    [UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
    public class MetricsConsentServiceTest : UmbracoIntegrationTest
    {
        private IMetricsConsentService MetricsConsentService => GetRequiredService<IMetricsConsentService>();

        private IKeyValueService KeyValueService => GetRequiredService<IKeyValueService>();

        [Test]
        [TestCase(AnalyticsLevel.Minimal)]
        [TestCase(AnalyticsLevel.Basic)]
        [TestCase(AnalyticsLevel.Detailed)]
        public void Can_Store_Consent(AnalyticsLevel level)
        {
            MetricsConsentService.SetConsentLevel(level);

            var actual = MetricsConsentService.GetConsentLevel();
            Assert.IsNotNull(actual);
            Assert.AreEqual(level, actual);
        }

        [Test]
        public void Enum_Stored_as_string()
        {
            MetricsConsentService.SetConsentLevel(AnalyticsLevel.Detailed);

            var stringValue = KeyValueService.GetValue(Cms.Core.Services.MetricsConsentService.Key);

            Assert.AreEqual("Detailed", stringValue);
        }
    }
}
