using System;

namespace Toggl.Phoebe.Models
{
    public interface IModelStore
    {
        T Get<T> (long id)
            where T : Model;

        Model Get (Type type, long id);

        void ModelChanged (Model model, string property);

        void Commit ();
    }
}
