namespace PESpy
{
    /// <summary>
    /// Represents the RT_* enumeration which describes well-known resource identifiers that may occur at the root level of a resource
    /// hierarchy. Note that there is no requirement that an assembly follow normal ID conventions.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// Hardware-dependent cursor resource.<para/>
        /// RT_CURSOR
        /// </summary>
        Cursor = 1,

        /// <summary>
        /// Bitmap resource.<para/>
        /// RT_BITMAP
        /// </summary>
        Bitmap = 2,

        /// <summary>
        /// Hardware-dependent icon resource.<para/>
        /// RT_ICON
        /// </summary>
        Icon = 3,

        /// <summary>
        /// Menu resource.<para/>
        /// RT_MENU
        /// </summary>
        Menu = 4,

        /// <summary>
        /// Dialog box.<para/>
        /// RT_DIALOG
        /// </summary>
        Dialog = 5,

        /// <summary>
        /// String-table entry.<para/>
        /// RT_STRING
        /// </summary>
        String = 6,

        /// <summary>
        /// Font directory resource.<para/>
        /// RT_FONTDIR
        /// </summary>
        FontDir = 7,

        /// <summary>
        /// Font resource.<para/>
        /// RT_FONT
        /// </summary>
        Font = 8,

        /// <summary>
        /// Accelerator table.<para/>
        /// RT_ACCELERATOR
        /// </summary>
        Accelerator = 9,

        /// <summary>
        /// Application-defined resource (raw data).<para/>
        /// RT_RCDATA
        /// </summary>
        RCData = 10,

        /// <summary>
        /// Message-table entry.<para/>
        /// RT_MESSAGETABLE
        /// </summary>
        MessageTable = 11,

        /// <summary>
        /// Hardware-independent cursor resource.<para/>
        /// RT_GROUP_CURSOR
        /// </summary>
        GroupCursor = 12,

        /// <summary>
        /// Hardware-independent icon resource.<para/>
        /// RT_GROUP_ICON
        /// </summary>
        GroupIcon = 14,

        /// <summary>
        /// Version resource.<para/>
        /// RT_VERSION
        /// </summary>
        Version = 16,

        /// <summary>
        /// Allows a resource editing tool to associate a string with an .rc file. Typically, the string is the name of the header file that provides symbolic names.
        /// The resource compiler parses the string but otherwise ignores the value.<para/>
        /// RT_DLGINCLUDE
        /// </summary>
        DlgInclude = 17,

        /// <summary>
        /// Plug and Play resource.<para/>
        /// RT_PLUGPLAY
        /// </summary>
        PlugPlay = 19,

        /// <summary>
        /// VXD.<para/>
        /// RT_VXD
        /// </summary>
        Vxd = 20,

        /// <summary>
        /// Animated cursor.<para/>
        /// RT_ANICURSOR
        /// </summary>
        AniCursor = 21,

        /// <summary>
        /// Animated icon.<para/>
        /// RT_ANIICON
        /// </summary>
        AniIcon = 22,

        /// <summary>
        /// HTML resource.<para/>
        /// RT_HTML
        /// </summary>
        Html = 23,

        /// <summary>
        /// Side-by-Side Assembly Manifest.<para/>
        /// RT_MANIFEST
        /// </summary>
        Manifest = 24
    }
}
