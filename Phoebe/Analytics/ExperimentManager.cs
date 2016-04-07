using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using XPlatUtils;

namespace Toggl.Phoebe.Analytics
{
    public class ExperimentManager
    {
        private readonly IList<Experiment> experiments;
        private Experiment currentExperiment;
        internal Random rand;

        public ExperimentManager(params Type[] experimentRegistries)
        {
            experiments = new List<Experiment> (FindExperiments(experimentRegistries));

            // TODO Rx review experiment code.
            // Restore currentExperiment
            /*
            var id = ServiceContainer.Resolve<ISettingsStore> ().ExperimentId;
            var id = StoreManager.Singleton.AppState.Settings.Ex
            if (id != null) {
                currentExperiment = experiments.FirstOrDefault (e => e.Id == id);
            }
            */
            currentExperiment = experiments.FirstOrDefault();
        }

        public Experiment CurrentExperiment
        {
            get { return currentExperiment; }
            private set
            {
                if (currentExperiment == value)
                {
                    return;
                }

                var id = value != null ? value.Id : null;
                // ServiceContainer.Resolve<ISettingsStore> ().ExperimentId = id;
                currentExperiment = value;

                if (value.SetUp != null)
                {
                    value.SetUp();
                }

                ServiceContainer.Resolve<MessageBus> ().Send(new ExperimentChangedMessage(this));
            }
        }

        internal List<Experiment> GetPossibleNextExperiments(bool isFreshInstall)
        {
            return experiments
                   .Where(e => !e.FreshInstallOnly || isFreshInstall)
                   .Where(IsValid)
                   .ToList();
        }

        internal int RandomNumber(int maxExclusive)
        {
            if (rand == null)
            {
                rand = new Random();
            }
            return rand.Next(maxExclusive);
        }

        public void NextExperiment(bool isFreshInstall)
        {
            // If the current experiment is still valid, do nothing.
            if (IsValid(CurrentExperiment))
            {
                return;
            }

            var validExperiments = GetPossibleNextExperiments(isFreshInstall);

            // Choose experiment or no experiment. Equal probability of any outcome.
            var idx = RandomNumber(validExperiments.Count + 1);
            if (idx < validExperiments.Count)
            {
                CurrentExperiment = validExperiments [idx];
            }
            else
            {
                CurrentExperiment = null;
            }
        }

        private static bool IsValid(Experiment e)
        {
            if (e == null || !e.Enabled)
            {
                return false;
            }

            var now = Time.UtcNow;
            if (e.StartTime != null && e.StartTime.ToUtc() > now)
            {
                return false;
            }
            if (e.EndTime != null && e.EndTime.ToUtc() < now)
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<Experiment> FindExperiments(Type[] registries)
        {
            return registries.SelectMany(
                       reg => reg.GetFields(BindingFlags.Static | BindingFlags.Public)
                       .Where(f => f.FieldType == typeof(Experiment))
                       .Select(f => (Experiment)f.GetValue(null))
                   )
                   .Where(e => e.Enabled);
        }
    }
}
