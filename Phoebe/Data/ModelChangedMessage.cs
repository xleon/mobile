using System;
using System.Linq.Expressions;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class ModelChangedMessage : Message
    {
        private readonly string propertyName;

        public ModelChangedMessage (Model model, string property) : base (model)
        {
            this.propertyName = property;
        }

        public Model Model {
            get { return (Model)Sender; }
        }

        public string PropertyName {
            get { return propertyName; }
        }
    }
}

