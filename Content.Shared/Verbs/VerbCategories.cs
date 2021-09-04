
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;

namespace Content.Shared.Verbs
{
    /// <summary>
    /// Contains combined name and icon information for a verb category.
    /// </summary>
    [Serializable, NetSerializable]
    public class VerbCategoryData
    {
        public readonly string Text;

        public readonly SpriteSpecifier? Icon;

        /// <summary>
        ///     If true, and this verb is the lone member of a verb category, it displays this verb in the context menu,
        ///     instead the category with the category name prepended to the verb text.
        /// </summary>
        /// <remarks>
        ///     For example, the ID console has two id slots. So you may have two verbs in the "Eject" category, with
        ///     individual verbs with text "Privileged ID" and "Target ID". If this option is set to true, and only the target ID
        ///     is present, this verb category will instead become a single verb with the text "Eject Target ID".
        ///     The verb icon will default to the verb category icon, if it isn't null;
        /// </remarks>
        public readonly bool Contractible;

        /// <summary>
        ///     If true, this verb category is shown in the context menu as a grid of icons without any text.
        /// </summary>
        /// <remarks>
        ///     For example, the 'Rotate' category simply shows two icons for rotating left and right.
        /// </remarks>
        public readonly bool IconsOnly;

        public VerbCategoryData(string text, string? icon, bool contractible = false, bool iconsOnly = false)
        {
            Text = Loc.GetString(text);
            Contractible = contractible;
            Icon = icon == null ? null : new SpriteSpecifier.Texture(new ResourcePath(icon));
            IconsOnly = iconsOnly;
        }
    }

    /// <summary>
    ///     Standard verb categories used across multiple systems.
    /// </summary>
    public struct VerbCategories
    {
        public static readonly VerbCategoryData Debug =
            new("verb-categories-debug", "/Textures/Interface/VerbIcons/debug.svg.192dpi.png");
        public static readonly VerbCategoryData Eject =
            new("verb-categories-eject", "/Textures/Interface/VerbIcons/eject.svg.192dpi.png", true);
        public static readonly VerbCategoryData Insert =
            new("verb-categories-insert", "/Textures/Interface/VerbIcons/insert.svg.192dpi.png", true);
        public static readonly VerbCategoryData Buckle =
            new("verb-categories-buckle", "/Textures/Interface/VerbIcons/buckle.svg.192dpi.png", true);
        public static readonly VerbCategoryData Unbuckle =
            new("verb-categories-unbuckle", "/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png", true);
        public static readonly VerbCategoryData Open =
            new("verb-categories-open", "/Textures/Interface/VerbIcons/open.svg.192dpi.png", true);
        public static readonly VerbCategoryData Close =
            new("verb-categories-close", "/Textures/Interface/VerbIcons/close.svg.192dpi.png", true);
        public static readonly VerbCategoryData Rotate =
            new("verb-categories-rotate", null, iconsOnly: true);
    }
}
