namespace PESpy
{
    /// <summary>
    /// Represents the RT_* enumeration which describes well-known resource identifiers that may occur at the root level of a resource
    /// hierarchy. Note that there is no requirement that an assembly follow normal ID conventions.
    /// </summary>
    public enum RT
    {
        /// <summary>
        /// Hardware-dependent cursor resource.
        /// </summary>
        RT_CURSOR = 1,

        /// <summary>
        /// Bitmap resource.
        /// </summary>
        RT_BITMAP = 2,

        /// <summary>
        /// Hardware-dependent icon resource.
        /// </summary>
        RT_ICON = 3,

        /// <summary>
        /// Menu resource.
        /// </summary>
        RT_MENU = 4,

        /// <summary>
        /// Dialog box.
        /// </summary>
        RT_DIALOG = 5,

        /// <summary>
        /// String-table entry.
        /// </summary>
        RT_STRING = 6,

        /// <summary>
        /// Font directory resource.
        /// </summary>
        RT_FONTDIR = 7,

        /// <summary>
        /// Font resource.
        /// </summary>
        RT_FONT = 8,

        /// <summary>
        /// Accelerator table.
        /// </summary>
        RT_ACCELERATOR = 9,

        /// <summary>
        /// Application-defined resource (raw data).
        /// </summary>
        RT_RCDATA = 10,

        /// <summary>
        /// Message-table entry.
        /// </summary>
        RT_MESSAGETABLE = 11,

        /// <summary>
        /// Hardware-independent cursor resource.
        /// </summary>
        RT_GROUP_CURSOR = 12,

        /// <summary>
        /// Hardware-independent icon resource.
        /// </summary>
        RT_GROUP_ICON = 14,

        /// <summary>
        /// Version resource.
        /// </summary>
        RT_VERSION = 16,

        /// <summary>
        /// Allows a resource editing tool to associate a string with an .rc file. Typically, the string is the name of the header file that provides symbolic names.
        /// The resource compiler parses the string but otherwise ignores the value.
        /// </summary>
        RT_DLGINCLUDE = 17,

        /// <summary>
        /// Plug and Play resource.
        /// </summary>
        RT_PLUGPLAY = 19,

        /// <summary>
        /// VXD.
        /// </summary>
        RT_VXD = 20,

        /// <summary>
        /// Animated cursor.
        /// </summary>
        RT_ANICURSOR = 21,

        /// <summary>
        /// Animated icon.
        /// </summary>
        RT_ANIICON = 22,

        /// <summary>
        /// HTML resource.
        /// </summary>
        RT_HTML = 23,

        /// <summary>
        /// Side-by-Side Assembly Manifest.
        /// </summary>
        RT_MANIFEST = 24
    }
}
