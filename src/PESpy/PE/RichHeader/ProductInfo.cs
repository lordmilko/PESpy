namespace PESpy
{
    public readonly struct ProductInfo
    {
        /// <summary>
        /// Gets the major version of the tool that this product item describes, e.g. <see cref="PRODID.prodidLinker511"/> describes LINK 5.11.
        /// </summary>
        public int ToolMajorVersion { get; }

        /// <summary>
        /// Gets the minor version of the tool that this product item describes, e.g. <see cref="PRODID.prodidLinker511"/> describes LINK 5.11.
        /// </summary>
        public int ToolMinorVersion { get; }

        /// <summary>
        /// Gets the build number of the tool that this product describes. This is value comes from <see cref="ProdItem.BuildId"/>.
        /// </summary>
        public int ToolBuild { get; }

        /// <summary>
        /// Gets a series of flags that describe the type of this product.
        /// </summary>
        public ProductKind Kind { get; }

        public string? ToolsetName { get; }

        public int? ToolsetBuild { get; }

        public string? ToolsetReleaseType { get; }

        public ProductKind LanguageKind => (ProductKind) ((int) Kind & ProductKindFlags.LanguageMask);

        public ProductKind ToolKind => (ProductKind) ((int) Kind & ProductKindFlags.ToolMask);

        public ProductKind OtherKind => (ProductKind) ((int) Kind & ProductKindFlags.OtherMask);

        //Tool or Other
        public ProductKind CategoryKind => (ProductKind) ((int) Kind & ~ProductKindFlags.LanguageMask);

        public string? ToolsetFullName
        {
            get
            {
                if (ToolsetName == null)
                    return null;

                using var builder = new ValueStringBuilder();

                builder.Append(ToolsetName);

                if (ToolsetBuild != null)
                {
                    builder.Append(".");
                    builder.Append(ToolsetBuild.Value);
                }
                else if (ToolsetReleaseType != null)
                {
                    builder.Append(' ');
                    builder.Append(ToolsetReleaseType);
                }

                if (Approx)
                    builder.Append($" (Approx: {ToolBuild - FoundToolBuild} under ({ToolBuild}-{FoundToolBuild})");

                return builder.ToString();
            }
        }

        public string ToolFullName
        {
            get
            {
                using var builder = new ValueStringBuilder();

                var category = CategoryKind;
                var tool = ToolKind;

                if (tool == ProductKind.None || category == tool)
                    builder.Append(category.ToString());
                else
                {
                    //e.g. C2 + LTCG
                    builder.Append(tool.ToString());
                    builder.Append(" (");

                    var categoryWithoutTool = category & ~tool;
                    builder.Append(categoryWithoutTool.ToString());

                    builder.Append(")");
                }

                if (ToolMajorVersion != 0 || ToolMinorVersion != 0 || ToolBuild != 0)
                {
                    builder.Append(' ');
                    builder.Append(ToolMajorVersion);
                    builder.Append('.');
                    builder.Append(ToolMinorVersion);
                    builder.Append(".");
                    builder.Append(ToolBuild);
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Gets whether this was an approximate match for the closest known build ID less than the build ID we were looking for.
        /// </summary>
        public bool Approx => ToolBuild != FoundToolBuild;

        /// <summary>
        /// Gets the build ID of the toolset that was matched against. If this value is different from <see cref="ToolBuild"/>, this indicates
        /// that this was a best effort approximate match.
        /// </summary>
        public int FoundToolBuild { get; }

        internal ProductInfo(
            int toolMajorVersion,
            int toolMinorVersion,
            int toolBuild,
            ProductKind kind,
            string toolsetName,
            int? toolsetBuild,
            string? toolsetReleaseType,
            int foundToolBuild)
        {
            ToolMajorVersion = toolMajorVersion;
            ToolMinorVersion = toolMinorVersion;
            ToolBuild = toolBuild;
            Kind = kind;
            ToolsetName = toolsetName;
            ToolsetBuild = toolsetBuild;
            ToolsetReleaseType = toolsetReleaseType;
            FoundToolBuild = foundToolBuild;
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(Kind.ToString());
            builder.Append(' ');

            if (ToolMajorVersion != 0 || ToolMinorVersion != 0 || ToolBuild != 0)
            {
                builder.Append(ToolMajorVersion);
                builder.Append('.');
                builder.Append(ToolMinorVersion);
                builder.Append('.');
                builder.Append(ToolBuild);
            }
            else
                builder.Append("(No Version)");

            var toolsetFullName = ToolsetFullName;

            if (toolsetFullName != null)
            {
                builder.Append(", ");

                builder.Append(toolsetFullName);
            }

            return builder.ToString();
        }
    }
}
