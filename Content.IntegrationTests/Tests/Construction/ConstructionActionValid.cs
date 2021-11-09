using System.Text;
using System.Threading.Tasks;
using Content.Server.Construction.Completions;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Construction
{
    [TestFixture]
    public class ConstructionActionValid : ContentIntegrationTest
    {
        private bool IsValid(IGraphAction action, IPrototypeManager protoMan, out string prototype)
        {
            switch (action)
            {
                case SpawnPrototype spawn:
                    prototype = spawn.Prototype;
                    return protoMan.TryIndex<EntityPrototype>(spawn.Prototype, out _);
                case SpawnPrototypeAtContainer spawn:
                    prototype = spawn.Prototype;
                    return protoMan.TryIndex<EntityPrototype>(spawn.Prototype, out _);
                case ConditionalAction conditional:
                    var valid = IsValid(conditional.Action, protoMan, out var protoA) & IsValid(conditional.Else, protoMan, out var protoB);

                    if (!string.IsNullOrEmpty(protoA) && string.IsNullOrEmpty(protoB))
                    {
                        prototype = protoA;
                    }

                    else if (string.IsNullOrEmpty(protoA) && !string.IsNullOrEmpty(protoB))
                    {
                        prototype = protoB;
                    }

                    else
                    {
                        prototype = $"{protoA}, {protoB}";
                    }

                    return valid;
                default:
                    prototype = string.Empty;
                    return true;
            }
        }

        [Test]
        public async Task ConstructionGraphSpawnPrototypeValid()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var node in graph.Nodes.Values)
                {
                    foreach (var action in node.Actions)
                    {
                        if (IsValid(action, protoMan, out var prototype)) continue;

                        valid = false;
                        message.Append($"Invalid entity prototype \"{prototype}\" on graph action in node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }

                    foreach (var edge in node.Edges)
                    {
                        foreach (var action in edge.Completed)
                        {
                            if (IsValid(action, protoMan, out var prototype)) continue;

                            valid = false;
                            message.Append($"Invalid entity prototype \"{prototype}\" on graph action in edge \"{edge.Target}\" of node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                        }
                    }
                }
            }

            Assert.That(valid, Is.True, $"One or more SpawnPrototype actions specified invalid entity prototypes!\n{message}");
        }

        [Test]
        public async Task ConstructionGraphNodeEntityPrototypeValid()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var node in graph.Nodes.Values)
                {
                    if (string.IsNullOrEmpty(node.Entity) || protoMan.TryIndex(node.Entity, out EntityPrototype _)) continue;

                    valid = false;
                    message.Append($"Invalid entity prototype \"{node.Entity}\" on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                }
            }

            Assert.That(valid, Is.True, $"One or more nodes specified invalid entity prototypes!\n{message}");
        }

        [Test]
        public async Task ConstructionGraphEdgeValid()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var protoMan = server.ResolveDependency<IPrototypeManager>();

            var valid = true;
            var message = new StringBuilder();

            foreach (var graph in protoMan.EnumeratePrototypes<ConstructionGraphPrototype>())
            {
                foreach (var node in graph.Nodes.Values)
                {
                    foreach (var edge in node.Edges)
                    {
                        if (graph.Nodes.ContainsKey(edge.Target)) continue;

                        valid = false;
                        message.Append($"Invalid target \"{edge.Target}\" in edge on node \"{node.Name}\" of graph \"{graph.ID}\"\n");
                    }
                }
            }

            Assert.That(valid, Is.True, $"One or more edges specified invalid node targets!\n{message}");
        }
    }
}
