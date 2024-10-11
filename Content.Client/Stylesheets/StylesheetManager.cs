using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Stylesheets.Redux;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Reflection;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IReflectionManager _reflection = default!;
        [Dependency] private readonly IResourceCache _resCache = default!; // TODO: REMOVE (obsolete; used to construct StyleNano/StyleSpace)

        public Stylesheet SheetNanotransen { get; private set; } = default!;
        public Stylesheet SheetSystem { get; private set; } = default!;

        [Obsolete("Update to use SheetNanotransen instead")]
        public Stylesheet SheetNano { get; private set; } = default!;
        [Obsolete("Update to use SheetSystem instead")]
        public Stylesheet SheetSpace { get; private set; } = default!;

        private Dictionary<string, Stylesheet> Stylesheets { get; set; } = default!;

        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            return Stylesheets.TryGetValue(name, out stylesheet);
        }

        public void Initialize()
        {
            var sawmill = _logManager.GetSawmill("style");
            sawmill.Debug("Initializing Stylesheets...");
            var sw = Stopwatch.StartNew();

            // add all sheetlets to the hashset
            var tys = _reflection.FindTypesWithAttribute<CommonSheetletAttribute>();
            UnusedSheetlets = [..tys];

            Stylesheets = new Dictionary<string, Stylesheet>();
            SheetNanotransen = Init("Nanotransen", new NanotrasenStylesheet(new BaseStylesheet.NoConfig(), this));
            SheetSystem = Init("Interface", new SystemStylesheet(new BaseStylesheet.NoConfig(), this));
            SheetNano = new StyleNano(_resCache).Stylesheet; // TODO: REMOVE (obsolete)
            SheetSpace = new StyleSpace(_resCache).Stylesheet; // TODO: REMOVE (obsolete)

            _userInterfaceManager.Stylesheet = SheetNanotransen;

            // warn about unused sheetlets
            if (UnusedSheetlets.Count > 0)
            {
                var sheetlets = UnusedSheetlets.AsEnumerable()
                    .Take(5)
                    .Select(t => t.FullName ?? "<could not get FullName>")
                    .ToArray();
                sawmill.Error($"There are unloaded sheetlets: {string.Join(", ", sheetlets)}");
            }

            sawmill.Debug($"Initialized {_styleRuleCount} style rules in {sw.Elapsed}");
        }

        public HashSet<Type> UnusedSheetlets { get; private set; } = [];

        private int _styleRuleCount;

        public Stylesheet Init(string name, BaseStylesheet baseSheet)
        {
            Stylesheets.Add(name, baseSheet.Stylesheet);
            _styleRuleCount += baseSheet.Stylesheet.Rules.Count;
            return baseSheet.Stylesheet;
        }
    }
}
