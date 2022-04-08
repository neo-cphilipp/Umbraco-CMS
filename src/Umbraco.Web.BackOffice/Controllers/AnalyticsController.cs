using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Cms.Web.BackOffice.Controllers
{
    public class AnalyticsController : UmbracoAuthorizedJsonController
    {
        private readonly IMetricsConsentService _metricsConsentService;
        public AnalyticsController(IMetricsConsentService metricsConsentService)
        {
            _metricsConsentService = metricsConsentService;
        }

        public AnalyticsLevel GetConsentLevel()
        {
            return _metricsConsentService.GetConsentLevel();
        }

        [HttpPost]
        public IActionResult SetConsentLevel([FromBody]TelemetryResource telemetryResource)
        {
            if (telemetryResource.TelemetryLevel == "Minimal")
            {
                _metricsConsentService.SetConsentLevel(AnalyticsLevel.Minimal);
                return Ok();
            }

            if (telemetryResource.TelemetryLevel == "Basic")
            {
                _metricsConsentService.SetConsentLevel(AnalyticsLevel.Basic);
                return Ok();
            }

            if (telemetryResource.TelemetryLevel == "Detailed")
            {
                _metricsConsentService.SetConsentLevel(AnalyticsLevel.Detailed);
                return Ok();
            }

            return BadRequest();
        }

        public IEnumerable<AnalyticsLevel> GetAllLevels() => new[] { AnalyticsLevel.Minimal, AnalyticsLevel.Basic, AnalyticsLevel.Detailed };
    }
}
