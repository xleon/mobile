using System;

namespace TogglDoodle.Models
{
    public class UserModel : Model
    {
        public static long NextId {
            get { return Model.NextId<UserModel> (); }
        }
    }
}
