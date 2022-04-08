using System.Runtime.Serialization;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Core.Install.Models
{
    [DataContract(Name = "analytics")]
    public class AnalyticsModel
    {
        [DataMember(Name = "analyticsLevel")]
        public AnalyticsLevel AnalyticsLevel { get; set; }
    }
}
