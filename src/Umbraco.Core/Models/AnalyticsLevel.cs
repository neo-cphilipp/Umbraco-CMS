using System.Runtime.Serialization;

namespace Umbraco.Cms.Core.Models
{
    [DataContract]
    public enum AnalyticsLevel
    {
        Minimal,
        Basic,
        Detailed,
    }
}
