using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Core.Services
{
    public interface IMetricsConsentService
    {
        AnalyticsLevel GetConsentLevel();

        void SetConsentLevel(AnalyticsLevel analyticsLevel);
    }
}
