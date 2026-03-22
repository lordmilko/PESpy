namespace PESpy.Ecma335
{
    public static class MetadataExtensions
    {
        #region Custom Attributes

        //Gets the custom attribute handle with the specified namespace + name without allocating (having said that, MetadataStringComparer _does_ seem to actually just allocate, so I'm not so sure how useful it really is)
        internal static CustomAttributeRow GetCustomAttributeHandle(this CompressedModelHeap heap, CustomAttributeList customAttributes, string customAttributeNamespace, string customAttributeName)
        {
            foreach (var customAttribute in customAttributes)
            {
                if (IsEqualCustomAttributeName(heap, customAttribute, customAttributeNamespace, customAttributeName))
                    return customAttribute;
            }

            return default;
        }

        internal static bool IsEqualCustomAttributeName(CompressedModelHeap heap, in CustomAttributeRow customAttribute, string customAttributeNamespace, string customAttributeName)
        {
            if (!customAttribute.TryGetName(out var namespaceIndex, out var nameIndex))
                return false;

            return namespaceIndex.GetString() == customAttributeNamespace && nameIndex.GetString() == customAttributeName;
        }

        #endregion
    }
}
