using Content.Server.Chat.Managers;
using Content.Server.Roles;
using Content.Shared.Roles;

namespace Content.Server.Traitor
{
    public sealed class RevoHeadRole : Role
    {
        public AntagPrototype Prototype { get; }

        public RevoHeadRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = antagPrototype.Name;
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public override bool Antagonist { get; }

        public void GreetTraitor(string[] codewords)
        {
            if (Mind.TryGetSession(out var session))
            {
                var chatMgr = IoCManager.Resolve<IChatManager>();
                chatMgr.DispatchServerMessage(session, Loc.GetString("revo-head-role-greeting"));
            }
        }
    }
}
