using System.Threading.Tasks;
using Umbraco.Cms.Core.Install.Models;

namespace Umbraco.Cms.Infrastructure.Install.InstallSteps
{
    [InstallSetupStep(InstallationType.NewInstall, "AnalyticsConsent", 30, "")]
    public class AnalyticsConsentStep : InstallSetupStep<AnalyticsModel>
    {
        public override string View => "analyticsconsent";

        public override Task<InstallSetupResult> ExecuteAsync(AnalyticsModel model)
        {
            return null;
        }

        public override bool RequiresExecution(AnalyticsModel model)
        {
            return true;
        }
    }
}
