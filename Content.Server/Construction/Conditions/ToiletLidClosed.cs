using System.Threading.Tasks;
using Content.Server.Toilet;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ToiletLidClosed : IGraphCondition
    {
        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ToiletComponent? toilet)) return false;
            return !toilet.IsLidOpen;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inExamineRange)
        {
            if (!entity.TryGetComponent(out ToiletComponent? toilet)) return false;
            if (!toilet.IsLidOpen) return false;

            message.AddMarkup(Loc.GetString("construction-condition-toilet-lid-closed") + "\n");
            return true;
        }
    }
}
