using System;
using System.Management.Automation;
using System.Reflection;

namespace PESpy.PowerShell.Adapters
{
    public abstract class PropertyInfoAdapter : PSPropertyAdapter
    {
        public override PSAdaptedProperty GetProperty(object baseObject, string propertyName)
        {
            if (IsBannedProperty(propertyName))
                return null;

            var type = baseObject.GetType();

            //PowerShell will automatically set the BaseObject value of the PSAdaptedProperty
            var propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo == null)
            {
                //Try again, ignoring case

                propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                if (propertyInfo == null)
                    return null;
            }

            return new PSAdaptedProperty(propertyName, propertyInfo);
        }

        public override string GetPropertyTypeName(PSAdaptedProperty adaptedProperty)
        {
            //The the name of a property and hit tab
            return ((PropertyInfo) adaptedProperty.Tag).PropertyType.FullName;
        }

        public override object GetPropertyValue(PSAdaptedProperty adaptedProperty)
        {
            return ((PropertyInfo) adaptedProperty.Tag).GetValue(adaptedProperty.BaseObject);
        }

        public override bool IsGettable(PSAdaptedProperty adaptedProperty) => true;

        public override bool IsSettable(PSAdaptedProperty adaptedProperty)
        {
            //Called when you the the name of a property and hit tab; _not_ called when you just randomly assign
            //a value to a property
            return ((PropertyInfo) adaptedProperty.Tag).GetSetMethod() != null;
        }

        public override void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value)
        {
            var propertyInfo = (PropertyInfo) adaptedProperty.Tag;

            var setMethod = propertyInfo.GetSetMethod();

            if (setMethod == null)
            {
                //Based on what CoreAdapter.cs does
                throw new SetValueException($"'{adaptedProperty.Name}' is a ReadOnly property.");
            }

            var baseObject = GetBaseObject(adaptedProperty.BaseObject);

            setMethod.Invoke(baseObject, new[] { value });
        }

        protected virtual object GetBaseObject(object baseObject) => baseObject;

        protected virtual bool IsBannedProperty(string propertyName) => false;
    }
}
