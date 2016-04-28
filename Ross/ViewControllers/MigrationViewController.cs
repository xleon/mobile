
using System;
using System.Threading;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Ross.Theme;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class MigrationViewController : UIViewController
    {
        private readonly int oldVersion;
        private readonly int newVersion;
        private readonly Action onSuccess;

        private UILabel titleLabel;
        private UIProgressView progressBar;

        public MigrationViewController(int oldVersion, int newVersion, Action onSuccess)
        {
            this.oldVersion = oldVersion;
            this.newVersion = newVersion;
            this.onSuccess = onSuccess;
        }

        public override void ViewDidLoad()
        {
            this.View.BackgroundColor = UIColor.White;

            this.View.Add(this.titleLabel = new UILabel
            {
                Text = "MigratingLocalData".Tr()
            } .Apply(Style.Migration.Text));

            this.View.Add(this.progressBar = new UIProgressView
            {
                Progress = 0,
            } .Apply(Style.Migration.ProgressBar));

            View.AddConstraints(
                this.titleLabel.AtTopOf(this.View, 150),
                this.titleLabel.AtLeftOf(this.View, 25),
                this.titleLabel.AtRightOf(this.View, 25),

                this.progressBar.AtTopOf(this.View, 200),
                this.progressBar.AtLeftOf(this.View, 25),
                this.progressBar.AtRightOf(this.View, 25),
                this.progressBar.Height().EqualTo(4)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

        }

        public override void ViewDidAppear(bool animated)
        {
            this.migrateAsync();
        }

        private async void migrateAsync()
        {
            // TODO: Replace with real `migrate` method
            var success = await Task.Run(this.migrateFake);

            if (success)
            {
                this.onSuccess();
            }
            else
            {
                // TODO: show error message
            }
        }

        private bool migrate()
        {
            return DatabaseHelper.Migrate(
                       ServiceContainer.Resolve<IPlatformUtils>().SQLiteInfo,
                       DatabaseHelper.GetDatabaseDirectory(),
                       this.oldVersion, this.newVersion,
                       this.setProgress
                   );
        }

        private bool migrateFake()
        {
            for (int i = 1; i <= 5; i++)
            {
                Thread.Sleep(1000);
                this.setProgress(i / 5f);
            }
            return true;
        }

        private void setProgress(float percentage)
        {
            this.InvokeOnMainThread(() =>
            {
                this.progressBar.SetProgress(percentage, percentage >= this.progressBar.Progress);
            });
        }
    }
}

