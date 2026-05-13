using System;

namespace PESpy.VB
{
    [Flags]
    public enum VBObjectType
    {
        /// <summary>
        /// A Visual Basic Designer for an Add-In
        /// </summary>
        Designer = 0x2,

        /// <summary>
        /// A Visual Basic Class
        /// </summary>
        ClassModule = 0x10,

        /// <summary>
        /// A Visual Basic Active X User Control (OCX)
        /// </summary>
        UserControl = 0x20,

        /// <summary>
        /// A Visual Basic User Document
        /// </summary>
        UserDocument = 0x80
    }
}
