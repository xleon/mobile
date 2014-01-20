using System;

namespace Toggl.Phoebe.Data
{
    /// <summary>
    /// Models committed message should be sent after the <see cref="M:IModelStore.Commit"/> has finished persisting
    /// all of the changes made to the models.
    /// </summary>
    public class ModelsCommittedMessage : Message
    {
        public ModelsCommittedMessage (IModelStore store) : base (store)
        {
        }
    }
}
