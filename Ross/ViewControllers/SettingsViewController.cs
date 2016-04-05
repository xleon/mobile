using System;
using System.Collections.Generic;
using Cirrious.FluentLayouts.Touch;
using UIKit;
using Toggl.Phoebe.Analytics;
using XPlatUtils;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.Reactive;
using GalaSoft.MvvmLight.Helpers;

namespace Toggl.Ross.ViewControllers
{
    public class SettingsViewController : UIViewController
    {
        private SettingsVM viewModel { get; set; }

        private LabelSwitchView askProjectView { get; set; }
        private LabelSwitchView mobileTagView { get; set; }
        private bool isResuming;

        private Binding<bool, bool> askProjectBinding;
        private Binding<bool, bool> mobileTagBinding;

        public SettingsViewController ()
        {
            Title = "SettingsTitle".Tr ();
            EdgesForExtendedLayout = UIRectEdge.None;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
            }
            base.Dispose (disposing);
        }

        public override void LoadView ()
        {
            View = new UIView ().Apply (Style.Screen);

            Add (new SeparatorView ().Apply (Style.Settings.Separator));
            Add (askProjectView = new LabelSwitchView ().Apply (Style.Settings.RowBackground));
            askProjectView.Label.Apply (Style.Settings.SettingLabel);
            askProjectView.Label.Text = "SettingsAskProject".Tr ();
            askProjectView.Switch.ValueChanged += OnAskProjectViewValueChanged;

            Add (new SeparatorView ().Apply (Style.Settings.Separator));
            Add (new UILabel () { Text = "SettingsAskProjectDesc".Tr () } .Apply (Style.Settings.DescriptionLabel));

            Add (new SeparatorView ().Apply (Style.Settings.Separator));
            Add (mobileTagView = new LabelSwitchView ().Apply (Style.Settings.RowBackground));
            mobileTagView.Label.Apply (Style.Settings.SettingLabel);
            mobileTagView.Label.Text = "SettingsMobileTag".Tr ();
            mobileTagView.Switch.ValueChanged += OnMobileTagViewValueChanged;

            Add (new SeparatorView ().Apply (Style.Settings.Separator));
            Add (new UILabel () { Text = "SettingsMobileTagDesc".Tr () } .Apply (Style.Settings.DescriptionLabel));

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
            View.AddConstraints (MakeConstraints (View));

            Rebind ();
        }

        private void BindAskProjectView (LabelSwitchView v)
        {
            //v.Switch.On = SettingsStore.ChooseProjectForNew;
        }

        private void BindMobileTagView (LabelSwitchView v)
        {
            //v.Switch.On = SettingsStore.UseDefaultTag;
        }

        private void Rebind ()
        {
            askProjectView.Apply (BindAskProjectView);
            mobileTagView.Apply (BindMobileTagView);
        }

        private void OnAskProjectViewValueChanged (object sender, EventArgs e)
        {
            viewModel.SetChooseProjectForNew (askProjectView.Switch.On);
        }

        private void OnMobileTagViewValueChanged (object sender, EventArgs e)
        {
            Console.WriteLine ($"send message to switch to: {mobileTagView.Switch.On}");
            viewModel.SetUseDefaultTag (mobileTagView.Switch.On);
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            viewModel = new SettingsVM (StoreManager.Singleton.AppState);

            // these bindings crash
            //this.askProjectBinding = this.SetBinding(() => viewModel.ChooseProjectForNew, () => askProjectView.Switch.On);
            //this.mobileTagBinding  = this.SetBinding(() => viewModel.UseDefaultTag, () => mobileTagView.Switch.On);

            // this binding SEEMS to work, except that the settings state does not appear to be updated
            this.mobileTagBinding = this.SetBinding (() => viewModel.UseDefaultTag)
            .WhenSourceChanges (() => {
                mobileTagView.Switch.On = viewModel.UseDefaultTag;
                Console.WriteLine ($"switch to: {viewModel.UseDefaultTag}, is now: {mobileTagView.Switch.On}");
            });

            //if (isResuming) {
            //    Rebind ();
            //}

            isResuming = true;
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);



        }

        public override void ViewWillDisappear (bool animated)
        {
            //this.askProjectBinding.Detach();
            this.mobileTagBinding.Detach();
            viewModel.Dispose();

            base.ViewWillDisappear (animated);

            /*
            if (subscriptionSettingChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionSettingChanged);
                subscriptionSettingChanged = null;
            }
            */
        }

        private static IEnumerable<FluentLayout> MakeConstraints (UIView container)
        {
            UIView prev = null;

            foreach (var view in container.Subviews) {
                var topMargin = 0f;
                var horizMargin = 0f;

                if (view is UILabel) {
                    topMargin = 7f;
                    horizMargin = 15f;
                } else if (view is SeparatorView && ! (prev is LabelSwitchView)) {
                    topMargin = 20f;
                    horizMargin = 0f;
                }

                if (prev == null) {
                    yield return view.AtTopOf (container, topMargin);
                } else {
                    yield return view.Below (prev, topMargin);
                }

                yield return view.AtLeftOf (container, horizMargin);
                yield return view.AtRightOf (container, horizMargin);

                if (view is LabelSwitchView) {
                    yield return view.Height ().EqualTo (42f);
                } else if (view is SeparatorView) {
                    yield return view.Height ().EqualTo (1f);
                }

                prev = view;
            }
        }

        private class SeparatorView : UIView
        {
            public SeparatorView ()
            {
                TranslatesAutoresizingMaskIntoConstraints = false;
            }
        }
    }
}