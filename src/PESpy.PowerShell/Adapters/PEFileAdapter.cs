using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;

namespace PESpy.PowerShell.Adapters
{
    public class PEFileAdapter : PropertyInfoAdapter
    {
        public override Collection<PSAdaptedProperty> GetProperties(object baseObject)
        {
            var peFile = (PEFile) baseObject;

            var results = new Collection<PSAdaptedProperty>();

            var properties = typeof(PEFile).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (IsBannedProperty(property.Name))
                    continue;

                results.Add(new PSAdaptedProperty(property.Name, property));
            }

            return results;
        }

        protected override bool IsBannedProperty(string propertyName)
        {
            //Exclude properties that depend on symbols
            switch (propertyName)
            {
                case nameof(PEFile.RTTICompleteObjectLocators):
                case nameof(PEFile.NativeAOTModules):
                case nameof(PEFile.Vftables):
                    return true;

                default:
                    return false;
            }
        }
    }
}
