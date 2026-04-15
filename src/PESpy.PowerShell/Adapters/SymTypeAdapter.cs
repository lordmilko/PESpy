using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;
using PESpy.PDB;

namespace PESpy.PowerShell.Adapters
{
    public class SymTypeAdapter : PropertyInfoAdapter
    {
        public override Collection<PSAdaptedProperty> GetProperties(object baseObject)
        {
            var symType = (SymType) baseObject;

            //If the value is null, don't return any properties; that would cause us to AV
            if (symType == default)
                return new Collection<PSAdaptedProperty>();

            var converted = ObjectSymTypeDispatcher.Instance.Dispatch(symType);
            var properties = converted.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var result = new Collection<PSAdaptedProperty>();

            //PowerShell will automatically set the BaseObject value of the PSAdaptedProperty
            foreach (var property in properties)
                result.Add(new PSAdaptedProperty(property.Name, property));

            return result;
        }

        public override PSAdaptedProperty GetProperty(object baseObject, string propertyName)
        {
            var symType = (SymType) baseObject;

            //If the value is null, don't return any properties; that would cause us to AV
            if (symType == default)
                return null;

            var converted = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

            return base.GetProperty(converted, propertyName);
        }

        public override Collection<string> GetTypeNameHierarchy(object baseObject)
        {
            var types = base.GetTypeNameHierarchy(baseObject);

            var symType = (SymType) baseObject;

            if (symType != default)
            {
                var converted = ObjectSymTypeDispatcher.Instance.Dispatch(symType);
                types.Add(converted.GetType().FullName);
            }

            return types;
        }

        public override object GetPropertyValue(PSAdaptedProperty adaptedProperty)
        {
            //ThirdPartyAdapter sets the underlying baseObject field
            var symType = (SymType) adaptedProperty.BaseObject;

            var converted = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

            var propertyInfo = (PropertyInfo) adaptedProperty.Tag;

            var value = propertyInfo.GetValue(converted);

            var propertyType = propertyInfo.PropertyType;

            //If the value is a null SymType, we can't be emitting it to the pipeline, because
            //PowerShell will then try and access the members of it, which will crash
            if (propertyType == typeof(SymType))
            {
                if ((SymType) value == default)
                    return null;
            }

            //Don't stringify our IString types here; we need a special format for those so we handle them correctly
            //no matter where they come from

            return value;
        }

        protected override object GetBaseObject(object baseObject)
        {
            var symType = (SymType) baseObject;

            var converted = ObjectSymTypeDispatcher.Instance.Dispatch(symType);

            return converted;
        }
    }
}
