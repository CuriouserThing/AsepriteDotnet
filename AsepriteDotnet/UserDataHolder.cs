namespace Aseprite
{
    public abstract class UserDataHolder
    {
        private UserData userData;
        public bool HasUserData { get; private set; }

        public UserData UserData
        {
            get => userData;
            set
            {
                userData = value;
                HasUserData = true;
            }
        }
    }
}
