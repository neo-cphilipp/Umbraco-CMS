using System;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Core.Services
{
    public class MetricsConsentService : IMetricsConsentService
    {
        internal const string Key = "UmbracoAnalyticsLevel";

        private readonly IKeyValueService _keyValueService;

        public MetricsConsentService(IKeyValueService keyValueService)
        {
            _keyValueService = keyValueService;
        }

        public AnalyticsLevel GetConsentLevel()
        {
            var analyticsLevelString = _keyValueService.GetValue(Key);

            if (analyticsLevelString is null || Enum.TryParse(analyticsLevelString, out AnalyticsLevel analyticsLevel) is false)
            {
                return AnalyticsLevel.Basic;
            }

            return analyticsLevel;
        }

        public void SetConsentLevel(AnalyticsLevel analyticsLevel)
        {
            _keyValueService.SetValue(Key, analyticsLevel.ToString());
        }
    }
}
