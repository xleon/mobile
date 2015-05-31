using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Joey.UI.Adapters
{
    public sealed class SettingsAdapter : BaseAdapter
    {
        private const int HeaderViewType = 0;
        private const int CheckboxViewType = 1;
        private readonly List<IListItem> listItems;

        public SettingsAdapter ()
        {
            listItems = new List<IListItem> () {
                new HeaderListItem (Resource.String.SettingsGeneralHeader),
                    new CheckboxListItem (
                        Resource.String.SettingsGeneralShowNotificationTitle,
                        Resource.String.SettingsGeneralShowNotificationDesc,
                        SettingsStore.PropertyShowNotification,
                        s => s.ShowNotification,
                        (s, v) => s.ShowNotification = v
                    ),
                    new CheckboxListItem (
                        Resource.String.SettingsGeneralNotifTitle,
                        Resource.String.SettingsGeneralNotifDesc,
                        SettingsStore.PropertyIdleNotification,
                        s => s.IdleNotification,
                        (s, v) => s.IdleNotification = v
                    ),
                    new CheckboxListItem (
                        Resource.String.SettingsGeneralAskProjectTitle,
                        Resource.String.SettingsGeneralAskProjectDesc,
                        SettingsStore.PropertyChooseProjectForNew,
                        s => s.ChooseProjectForNew,
                        (s, v) => s.ChooseProjectForNew = v
                    ),
                    new CheckboxListItem (
                        Resource.String.SettingsGeneralMobileTagTitle,
                        Resource.String.SettingsGeneralMobileTagDesc,
                        SettingsStore.PropertyUseDefaultTag,
                        s => s.UseDefaultTag,
                        (s, v) => s.UseDefaultTag = v
                    ),
                    new CheckboxListItem (
                        Resource.String.SettingsGeneralGroupedEntriesTitle,
                        Resource.String.SettingsGeneralGroupedEntriesDesc,
                        SettingsStore.PropertyGroupedTimeEntries,
                        s => s.GroupedTimeEntries,
                        (s, v) => s.GroupedTimeEntries = v
                    ),

            };
        }

        public override int ViewTypeCount
        {
            get { return 2; }
        }

        public override Java.Lang.Object GetItem (int position)
        {
            return null;
        }

        public override long GetItemId (int position)
        {
            return position;
        }

        public override bool IsEnabled (int position)
        {
            return GetItemViewType (position) != HeaderViewType;
        }

        public override int GetItemViewType (int position)
        {
            return listItems [position].ViewType;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            var view = convertView;
            var listItem = listItems [position];
            var viewType = listItem.ViewType;

            if (viewType == HeaderViewType) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.SettingsListHeaderItem, parent, false);
                    view.Tag = new HeaderListItemHolder (view);
                }
                var holder = (HeaderListItemHolder)view.Tag;
                holder.Bind ((HeaderListItem)listItem);
            } else if (viewType == CheckboxViewType) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.SettingsListCheckboxItem, parent, false);
                    view.Tag = new CheckboxListItemHolder (view);
                }
                var holder = (CheckboxListItemHolder)view.Tag;
                holder.Bind ((CheckboxListItem)listItem);
            } else {
                throw new InvalidOperationException (String.Format ("Unknown view type {0}", viewType));
            }

            return view;
        }

        public override int Count
        {
            get { return listItems.Count; }
        }

        public void OnItemClicked (int position)
        {
            var item = listItems [position];

            var checkItem = item as CheckboxListItem;
            if (checkItem != null) {
                checkItem.Toggle ();
            }
        }

        private interface IListItem
        {
            int ViewType { get; }
        }

        private class HeaderListItem : IListItem
        {
            private readonly int headerResId;

            public HeaderListItem (int headerResId)
            {
                this.headerResId = headerResId;
            }

            public int ViewType
            {
                get { return HeaderViewType; }
            }

            public int HeaderResId
            {
                get { return headerResId; }
            }
        }

        private class CheckboxListItem : IListItem
        {
            private readonly int titleResId;
            private readonly int descriptionResId;
            private readonly string settingName;
            private readonly Func<SettingsStore, bool> valueGetter;
            private readonly Action<SettingsStore, bool> valueSetter;

            public CheckboxListItem (int titleResId, int descriptionResId, string settingName, Func<SettingsStore, bool> valueGetter, Action<SettingsStore, bool> valueSetter)
            {
                this.titleResId = titleResId;
                this.descriptionResId = descriptionResId;
                this.settingName = settingName;
                this.valueGetter = valueGetter;
                this.valueSetter = valueSetter;
            }

            public int ViewType
            {
                get { return CheckboxViewType; }
            }

            public int TitleResId
            {
                get { return titleResId; }
            }

            public int DescriptionResId
            {
                get { return descriptionResId; }
            }

            public string SettingName
            {
                get { return settingName; }
            }

            public bool IsChecked
            {
                get {
                    var store = ServiceContainer.Resolve<SettingsStore> ();
                    return valueGetter (store);
                }
            }

            public void Toggle ()
            {
                var store = ServiceContainer.Resolve<SettingsStore> ();
                valueSetter (store, !valueGetter (store));
            }
        }

        private class HeaderListItemHolder : BindableViewHolder<HeaderListItem>
        {
            public TextView HeaderTextView { get; private set; }

            public HeaderListItemHolder (View root) : base (root)
            {
                HeaderTextView = root.FindViewById<TextView> (Resource.Id.HeaderTextView).SetFont (Font.RobotoMedium);
            }

            protected override void Rebind ()
            {
                HeaderTextView.SetText (DataSource.HeaderResId);
            }
        }

        private abstract class SettingViewHolder<T> : BindableViewHolder<T>
        {
            private Subscription<SettingChangedMessage> subscriptionSettingChanged;

            public SettingViewHolder (View root) : base (root)
            {
            }

            protected override void Dispose (bool disposing)
            {
                if (disposing) {
                    Unsubscribe (ServiceContainer.Resolve<MessageBus> ());
                }

                base.Dispose (disposing);
            }

            protected override void OnRootAttachedToWindow (object sender, View.ViewAttachedToWindowEventArgs e)
            {
                base.OnRootAttachedToWindow (sender, e);
                Subscribe (ServiceContainer.Resolve<MessageBus> ());
            }

            protected override void OnRootDetachedFromWindow (object sender, View.ViewDetachedFromWindowEventArgs e)
            {
                Unsubscribe (ServiceContainer.Resolve<MessageBus> ());
                base.OnRootDetachedFromWindow (sender, e);
            }

            protected virtual void Subscribe (MessageBus bus)
            {
                if (subscriptionSettingChanged == null) {
                    subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (DispatchSettingChanged);
                }
            }

            protected virtual void Unsubscribe (MessageBus bus)
            {
                if (subscriptionSettingChanged != null) {
                    bus.Unsubscribe (subscriptionSettingChanged);
                    subscriptionSettingChanged = null;
                }
            }

            private void DispatchSettingChanged (SettingChangedMessage msg)
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                OnSettingChanged (msg);
            }

            protected abstract void OnSettingChanged (SettingChangedMessage msg);
        }

        private class CheckboxListItemHolder : SettingViewHolder<CheckboxListItem>
        {
            public TextView TitleTextView { get; private set; }

            public TextView DescriptionTextView { get; private set; }

            public CheckBox CheckBox { get; private set; }

            public CheckboxListItemHolder (View root) : base (root)
            {
                TitleTextView = root.FindViewById<TextView> (Resource.Id.TitleTextView).SetFont (Font.Roboto);
                DescriptionTextView = root.FindViewById<TextView> (Resource.Id.DescriptionTextView).SetFont (Font.RobotoLight);
                CheckBox = root.FindViewById<CheckBox> (Resource.Id.CheckBox);
            }

            protected override void Rebind ()
            {
                TitleTextView.SetText (DataSource.TitleResId);
                DescriptionTextView.SetText (DataSource.DescriptionResId);
                CheckBox.Checked = DataSource.IsChecked;
            }

            protected override void OnSettingChanged (SettingChangedMessage msg)
            {
                if (DataSource == null) {
                    return;
                }

                if (msg.Name == DataSource.SettingName) {
                    Rebind ();
                }
            }
        }
    }
}
