using System;

namespace Toggl.Phoebe.Analytics
{
    public class ExperimentChangedMessage : Message
    {
        public ExperimentChangedMessage(ExperimentManager sender) : base(sender)
        {
        }

        public ExperimentManager ExperimentManager
        {
            get { return (ExperimentManager)Sender; }
        }

        public Experiment CurrentExperiment
        {
            get { return ExperimentManager.CurrentExperiment; }
        }
    }
}
