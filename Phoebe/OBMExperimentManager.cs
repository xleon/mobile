using Toggl.Phoebe.Net;
using XPlatUtils;
using System;

namespace Toggl.Phoebe
{
    public static class OBMExperimentManager
    {
        public const int HomeEmptyState = 75;

        public static async void Send (int experimentNumber, string actionKey, string actionValue)
        {
            if (InExperimentGroups (experimentNumber)) {
                var experimentAction = new ExperimentAction () {
                    ExperimentId = experimentNumber,
                    ActionKey = actionKey,
                    ActionValue = actionValue
                };
                await experimentAction.Send ();
            }
        }

        public static bool InludedInExperiment (int experimentNumber)
        {
            var userData = ServiceContainer.Resolve<AuthManager> ().User;
            return userData.ExperimentIncluded && userData.ExperimentNumber == experimentNumber;
        }

        public static bool InExperimentGroups (int experimentNumber)
        {
            var userData = ServiceContainer.Resolve<AuthManager> ().User;
            return userData.ExperimentNumber == experimentNumber;
        }

    }
}

