using System;
using System.Reactive.Linq;
using PropertyChanged;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Reactive;

namespace Toggl.Phoebe._ViewModels
{
    [ImplementPropertyChanged]
    public class SettingsVM : IDisposable
    {
        private readonly IDisposable subscription;

        public SettingsVM(AppState state)
        {
            this.subscription = StoreManager.Singleton
                .Observe(x => x.State)
                .StartWith(state)
                .Subscribe(this.stateUpdated);
        }

        #region exposed UI properties

        public bool ShowNotification { get; private set; }
        public bool IdleNotification { get; private set; }
        public bool ChooseProjectForNew { get; private set; }
        public bool UseDefaultTag { get; private set; }
        public bool GroupedTimeEntries { get; private set; }

        #endregion

        #region public methods

        public void SetShowNotification(bool value)
        {
            setSetting(nameof(SettingsState.ShowNotification), value);
        }
        public void SetIdleNotification(bool value)
        {
            setSetting(nameof(SettingsState.IdleNotification), value);
        }
        public void SetChooseProjectForNew(bool value)
        {
            setSetting(nameof(SettingsState.ChooseProjectForNew), value);
        }
        public void SetUseDefaultTag(bool value)
        {
            setSetting(nameof(SettingsState.UseDefaultTag), value);
        }
        public void SetGroupedTimeEntries(bool value)
        {
            setSetting(nameof(SettingsState.GroupedEntries), value);
        }

        #endregion

        private void stateUpdated(AppState state)
        {
            var settings = state.Settings;

            this.ShowNotification = settings.ShowNotification;
            this.IdleNotification = settings.IdleNotification;
            this.ChooseProjectForNew = settings.ChooseProjectForNew;
            this.UseDefaultTag = settings.UseDefaultTag;
            this.GroupedTimeEntries = settings.GroupedEntries;
        }

        private static void setSetting(string propertyName, object value)
        {
            RxChain.Send(new DataMsg.UpdateSetting(propertyName, value));
        }

        public void Dispose()
        {
            this.subscription.Dispose();
        }
    }
}

