using System;
using System.Threading;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using GalaSoft.MvvmLight.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Misc;
using Toggl.Phoebe.Reactive;
using Toggl.Ross.Theme;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class MigrationViewController : UIViewController
    {
        private readonly int oldVersion;
        private readonly int newVersion;

        private UILabel topLabel;
        private UILabel descLabel;
        private UILabel discardLabel;
        private UILabel discardDesc;
        private UILabel percente;
        private UIProgressView progressBar;
        private UIButton tryAgainBtn;
        private UIButton discardBtn;

        private UIImageView toggler1;
        private UIImageView toggler2;

        private bool userTriedAgain;

        public MigrationViewController(int oldVersion, int newVersion)
        {
            this.oldVersion = oldVersion;
            this.newVersion = newVersion;
            Title = "MigratingScreenTitle".Tr();
        }

        public override void ViewDidLoad()
        {
            View.BackgroundColor = UIColor.White;

            View.Add(topLabel = new UILabel
            {
                Text = "MigratingUpdateTitle".Tr()
            } .Apply(Style.Migration.TopLabel));

            View.Add(descLabel = new UILabel
            {
                Text = "MigratingUpdateDesc".Tr()
            } .Apply(Style.Migration.DescriptionLabel));

            View.Add(discardLabel = new UILabel
            {
                Text = "MigratingDiscardTitle".Tr()
            } .Apply(Style.Migration.DiscardTopLabel));

            View.Add(discardDesc = new UILabel
            {
                Text = "MigratingDiscardDesc".Tr()
            } .Apply(Style.Migration.DiscardDescriptionLabel));

            View.Add(percente = new UILabel
            {
                Text = "0 %"
            } .Apply(Style.Migration.DiscardDescriptionLabel));

            View.Add(progressBar = new UIProgressView
            {
                Progress = 0,
            } .Apply(Style.Migration.ProgressBar));

            View.Add(tryAgainBtn = UIButton.FromType(UIButtonType.RoundedRect)
                                   .Apply(Style.Migration.TryAgainBtn));
            tryAgainBtn.SetTitle("MigratingTryBtn".Tr(), UIControlState.Normal);

            View.Add(discardBtn = UIButton.FromType(UIButtonType.System)
                                  .Apply(Style.Migration.DiscardBtn));
            discardBtn.SetTitle("MigratingDiscardBtn".Tr(), UIControlState.Normal);

            View.Add(toggler1 = new UIImageView(UIImage.FromFile("toggler1")));
            View.Add(toggler2 = new UIImageView(UIImage.FromFile("toggler2")));

            View.AddConstraints(
                topLabel.AtTopOf(View, 150),
                topLabel.AtLeftOf(View, 25),
                topLabel.AtRightOf(View, 25),

                descLabel.Below(topLabel, 16),
                descLabel.AtLeftOf(View, 30),
                descLabel.AtRightOf(View, 30),

                toggler1.Below(descLabel, 12),
                toggler1.WithSameCenterX(View),

                toggler2.Below(descLabel, 12),
                toggler2.WithSameCenterX(View),

                percente.Below(toggler1, 7),
                percente.WithSameCenterX(View),

                progressBar.Below(percente, 24),
                progressBar.AtLeftOf(View, 30),
                progressBar.AtRightOf(View, 30),
                progressBar.Height().EqualTo(3),

                tryAgainBtn.Below(descLabel, 24),
                tryAgainBtn.WithSameCenterX(View),
                tryAgainBtn.Height().EqualTo(40),
                tryAgainBtn.Width().EqualTo(240),

                discardLabel.Below(descLabel, 24),
                discardLabel.WithSameCenterX(View),

                discardDesc.Below(discardLabel, 24),
                discardDesc.AtLeftOf(View, 50),
                discardDesc.AtRightOf(View, 50),

                discardBtn.Below(discardDesc, 10),
                discardBtn.WithSameCenterX(View)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();
        }

        public override void ViewDidAppear(bool animated)
        {
            MigrateDatabase();
            tryAgainBtn.TouchUpInside += (sender, e) =>
            {
                MigrateDatabase();
                userTriedAgain = true;
            };
            discardBtn.TouchUpInside += async(sender, e) =>
            {
                await ServiceContainer.Resolve<IDialogService>()
                .ShowMessage("MigratingDiscardDialogMsg".Tr(),
                             "MigratingDiscardDialogTitle".Tr(),
                             "MigratingDiscardConfirm".Tr(),
                             "MigratingDiscardCancel".Tr(),
                             confirm =>
                {
                    if (confirm)
                    {
                        // ATTENTION At this point, old DBs are deleted,
                        // the state is reseted and Intro screen is shown.
                        // All this operations could be converted in reducers
                        // and maybe moved to the state.
                        DatabaseHelper.ResetToDBVersion(SyncSqliteDataStore.DB_VERSION);
                        RxChain.Send(new DataMsg.ResetState());
                        NavigationController.SetViewControllers(new[] { new WelcomeViewController() }, true);
                    }
                });
            };
        }

        private void MigrateDatabase()
        {
            Task.Run(() =>
            {
                var migrationResult = DatabaseHelper.Migrate(
                                          ServiceContainer.Resolve<IPlatformUtils>().SQLiteInfo,
                                          DatabaseHelper.GetDatabaseDirectory(),
                                          oldVersion, newVersion,
                                          setProgress
                                      );
                if (migrationResult)
                {
                    InvokeOnMainThread(() => DisplaySuccessState());
                    RxChain.Send(new DataMsg.InitStateAfterMigration());
                }
                else
                {
                    InvokeOnMainThread(() =>
                    {
                        if (!userTriedAgain)
                            DisplayErrorState();
                        else
                            DisplayDiscardState();
                    });
                }
            });

            DisplayInitialState();
        }

        private void DisplayInitialState()
        {
            topLabel.Text = "MigratingUpdateTitle".Tr();
            descLabel.Text = "MigratingUpdateDesc".Tr();
            progressBar.Hidden = false;

            toggler2.Hidden = true;
            tryAgainBtn.Hidden = true;
            discardBtn.Hidden = true;
            discardDesc.Hidden = true;
        }

        private void DisplaySuccessState()
        {
            topLabel.Text = "MigratingSuccessTitle".Tr();
            descLabel.Text = "MigratingSuccessDesc".Tr();
            toggler2.Hidden = false;

            toggler1.Hidden = true;
            progressBar.Hidden = true;
            tryAgainBtn.Hidden = true;
            percente.Hidden = true;
        }

        private void DisplayErrorState()
        {
            topLabel.Text = "MigratingTryTitle".Tr();
            descLabel.Text = "MigratingTryDesc".Tr();
            tryAgainBtn.Hidden = false;

            toggler1.Hidden = true;
            toggler2.Hidden = true;
            percente.Hidden = true;
            progressBar.Hidden = true;
            discardBtn.Hidden = true;
            discardDesc.Hidden = true;
        }

        private void DisplayDiscardState()
        {
            topLabel.Text = "MigratingFeedbackTitle".Tr();
            descLabel.Text = "MigratingFeedbackDesc".Tr();
            discardLabel.Hidden = false;
            discardDesc.Hidden = false;
            discardBtn.Hidden = false;

            toggler1.Hidden = true;
            toggler2.Hidden = true;
            percente.Hidden = true;
            progressBar.Hidden = true;
            tryAgainBtn.Hidden = true;
        }

        // method for testing
        private bool migrateFake()
        {
            for (int i = 1; i <= 5; i++)
            {
                Thread.Sleep(1000);
                setProgress(i / 5f);
            }
            return true;
        }

        private void setProgress(float percentage)
        {
            InvokeOnMainThread(() =>
            {
                progressBar.SetProgress(percentage, percentage >= progressBar.Progress);
                percente.Text = Math.Truncate(percentage * 100) + " %";
            });
        }
    }
}

