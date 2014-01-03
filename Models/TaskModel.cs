using System;

namespace TogglDoodle.Models
{
    public class TaskModel : Model
    {
        public static long NextId {
            get { return Model.NextId<TaskModel> (); }
        }
    }
}
