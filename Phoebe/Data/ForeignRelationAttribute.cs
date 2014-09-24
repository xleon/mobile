using System;

namespace Toggl.Phoebe.Data
{
    [AttributeUsage (AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ForeignRelationAttribute : Attribute
    {
        readonly Type dataType;

        public Type DataType
        {
            get { return dataType; }
        }

        public ForeignRelationAttribute (Type dataType)
        {
            this.dataType = dataType;
        }
    }
}
