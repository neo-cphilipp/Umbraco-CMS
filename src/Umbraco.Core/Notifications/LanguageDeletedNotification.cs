// Copyright (c) Umbraco.
// See LICENSE for more details.

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Core.Notifications
{
    public class LanguageDeletedNotification : DeletedNotification<ILanguage>
    {
        public LanguageDeletedNotification(ILanguage target, EventMessages messages) : base(target, messages)
        {
        }
    }
}
