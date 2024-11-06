using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Chemistry.EntitySystems;
using Content.Client.Guidebook.Richtext;
using Content.Client.Message;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.Controls;

/// <summary>
///     Control for embedding a reagent into a guidebook.
/// </summary>
[UsedImplicitly, GenerateTypedNameReferences]
public sealed partial class GuideReagentEmbed : BoxContainer, IDocumentTag, ISearchableControl
{
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ChemistryGuideDataSystem _chemistryGuideData;

    public GuideReagentEmbed()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _chemistryGuideData = _systemManager.GetEntitySystem<ChemistryGuideDataSystem>();
        MouseFilter = MouseFilterMode.Stop;
    }

    public GuideReagentEmbed(string reagent) : this()
    {
        GenerateControl(_prototype.Index<ReagentPrototype>(reagent));
    }

    public GuideReagentEmbed(ReagentPrototype reagent) : this()
    {
        GenerateControl(reagent);
    }

    public bool CheckMatchesSearch(string query)
    {
        return this.ChildrenContainText(query);
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Reagent", out var id))
        {
            Logger.Error("Reagent embed tag is missing reagent prototype argument");
            return false;
        }

        if (!_prototype.TryIndex<ReagentPrototype>(id, out var reagent))
        {
            Logger.Error($"Specified reagent prototype \"{id}\" is not a valid reagent prototype");
            return false;
        }

        GenerateControl(reagent);

        control = this;
        return true;
    }

    private void GenerateControl(ReagentPrototype reagent)
    {
        NameBackground.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = reagent.SubstanceColor
        };

        var r = reagent.SubstanceColor.R;
        var g = reagent.SubstanceColor.G;
        var b = reagent.SubstanceColor.B;

        var textColor = 0.2126f * r + 0.7152f * g + 0.0722f * b > 0.5
            ? Color.Black
            : Color.White;

        ReagentName.SetMarkup(Loc.GetString("guidebook-reagent-name",
            ("color", textColor), ("name", reagent.LocalizedName)));

        #region Recipe
        var reactions = _prototype.EnumeratePrototypes<ReactionPrototype>()
            .Where(p => !p.Source && p.Products.ContainsKey(reagent.ID))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Products.Count)
            .ToList();

        if (reactions.Any())
        {
            foreach (var reactionPrototype in reactions)
            {
                RecipesDescriptionContainer.AddChild(new GuideReagentReaction(reactionPrototype, _prototype, _systemManager));
            }
        }
        else
        {
            RecipesContainer.Visible = false;
        }
        #endregion

        #region Effects
        if (_chemistryGuideData.ReagentGuideRegistry.TryGetValue(reagent.ID, out var guideEntryRegistry) &&
            guideEntryRegistry.GuideEntries != null &&
            guideEntryRegistry.GuideEntries.Values.Any(pair => pair.EffectDescriptions.Any()))
        {
            EffectsDescriptionContainer.Children.Clear();
            foreach (var (group, effect) in guideEntryRegistry.GuideEntries)
            {
                if (!effect.EffectDescriptions.Any())
                    continue;

                var groupLabel = new RichTextLabel();
                groupLabel.SetMarkup(Loc.GetString("guidebook-reagent-effects-metabolism-group-rate",
                    ("group", _prototype.Index<MetabolismGroupPrototype>(group).LocalizedName), ("rate", effect.MetabolismRate)));
                var descriptionLabel = new RichTextLabel
                {
                    Margin = new Thickness(25, 0, 10, 0)
                };

                var descMsg = new FormattedMessage();
                var descriptionsCount = effect.EffectDescriptions.Length;
                var i = 0;
                foreach (var effectString in effect.EffectDescriptions)
                {
                    descMsg.AddMarkupOrThrow(effectString);
                    i++;
                    if (i < descriptionsCount)
                        descMsg.PushNewline();
                }
                descriptionLabel.SetMessage(descMsg);

                EffectsDescriptionContainer.AddChild(groupLabel);
                EffectsDescriptionContainer.AddChild(descriptionLabel);
            }
        }
        else
        {
            EffectsContainer.Visible = false;
        }
        #endregion

        #region PlantMetabolisms
        if (_chemistryGuideData.ReagentGuideRegistry.TryGetValue(reagent.ID, out var guideEntryRegistryPlant) &&
            guideEntryRegistryPlant.PlantMetabolisms != null &&
            guideEntryRegistryPlant.PlantMetabolisms.Count > 0)
        {
            PlantMetabolismsDescriptionContainer.Children.Clear();
            var metabolismLabel = new RichTextLabel();
            metabolismLabel.SetMarkup(Loc.GetString("guidebook-reagent-plant-metabolisms-rate"));
            var descriptionLabel = new RichTextLabel
            {
                Margin = new Thickness(25, 0, 10, 0)
            };
            var descMsg = new FormattedMessage();
            var descriptionsCount = guideEntryRegistryPlant.PlantMetabolisms.Count;
            var i = 0;
            foreach (var effectString in guideEntryRegistryPlant.PlantMetabolisms)
            {
                descMsg.AddMarkupOrThrow(effectString);
                i++;
                if (i < descriptionsCount)
                    descMsg.PushNewline();
            }
            descriptionLabel.SetMessage(descMsg);

            PlantMetabolismsDescriptionContainer.AddChild(metabolismLabel);
            PlantMetabolismsDescriptionContainer.AddChild(descriptionLabel);
        }
        else
        {
            PlantMetabolismsContainer.Visible = false;
        }
        #endregion

        GenerateSources(reagent);

        FormattedMessage description = new();
        if (reagent.Contraband != "None")
        {
            var severity = _prototype.Index(reagent.Contraband);
            description.AddMarkupOrThrow(Loc.GetString(severity.ExamineText));
            description.PushNewline();
        }
        description.AddText(reagent.LocalizedDescription);
        description.PushNewline();
        description.AddMarkupOrThrow(Loc.GetString("guidebook-reagent-physical-description",
            ("description", reagent.LocalizedPhysicalDescription)));
        ReagentDescription.SetMessage(description);
    }

    private void GenerateSources(ReagentPrototype reagent)
    {
        var sources = _chemistryGuideData.GetReagentSources(reagent.ID);
        if (sources.Count == 0)
        {
            SourcesContainer.Visible = false;
            return;
        }
        SourcesContainer.Visible = true;

        var orderedSources = sources
            .OrderBy(o => o.OutputCount)
            .ThenBy(o => o.IdentifierString);
        foreach (var source in orderedSources)
        {
            if (source is ReagentEntitySourceData entitySourceData)
            {
                SourcesDescriptionContainer.AddChild(new GuideReagentReaction(
                    entitySourceData.SourceEntProto,
                    entitySourceData.Solution,
                    entitySourceData.MixingType,
                    _prototype,
                    _systemManager));
            }
            else if (source is ReagentReactionSourceData reactionSourceData)
            {
                SourcesDescriptionContainer.AddChild(new GuideReagentReaction(
                    reactionSourceData.ReactionPrototype,
                    _prototype,
                    _systemManager));
            }
            else if (source is ReagentGasSourceData gasSourceData)
            {
                SourcesDescriptionContainer.AddChild(new GuideReagentReaction(
                    gasSourceData.GasPrototype,
                    gasSourceData.MixingType,
                    _prototype,
                    _systemManager));
            }
        }
    }
}
