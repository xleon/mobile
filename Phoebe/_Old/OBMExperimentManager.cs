using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class OBMExperimentManager
    {
        #if __ANDROID__
        public const int ExperimentNumber = 93;
        #else
        public const int ExperimentNumber = 83;
        #endif

        public const string StartButtonActionKey = "startButton";
        public const string ClickActionValue = "click";

        public static async void Send (string actionKey, string actionValue)
        {
            var experimentAction = new ExperimentAction {
                ExperimentId = ExperimentNumber,
                ActionKey = actionKey,
                ActionValue = actionValue
            };
            await experimentAction.Send();
        }

        public static bool IncludedInExperiment ()
        {
            var userData = ServiceContainer.Resolve<AuthManager>().User;
            return userData.ExperimentIncluded && userData.ExperimentNumber == ExperimentNumber;
        }

        public static bool InExperimentGroups (int experimentNumber)
        {
            var userData = ServiceContainer.Resolve<AuthManager>().User;
            return userData.ExperimentNumber == experimentNumber;
        }

    }
}

