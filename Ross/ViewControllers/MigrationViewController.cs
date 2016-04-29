using System.Threading;
using System.Threading.Tasks;
using Cirrious.FluentLayouts.Touch;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
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

        private UILabel titleLabel;
        private UIProgressView progressBar;

        public MigrationViewController(int oldVersion, int newVersion)
        {
            this.oldVersion = oldVersion;
            this.newVersion = newVersion;
        }

        public override void ViewDidLoad()
        {
            View.BackgroundColor = UIColor.White;

            View.Add(titleLabel = new UILabel
            {
                Text = "MigratingLocalData".Tr()
            } .Apply(Style.Migration.Text));

            View.Add(progressBar = new UIProgressView
            {
                Progress = 0,
            } .Apply(Style.Migration.ProgressBar));

            View.AddConstraints(
                titleLabel.AtTopOf(View, 150),
                titleLabel.AtLeftOf(View, 25),
                titleLabel.AtRightOf(View, 25),

                progressBar.AtTopOf(View, 200),
                progressBar.AtLeftOf(View, 25),
                progressBar.AtRightOf(View, 25),
                progressBar.Height().EqualTo(4)
            );

            View.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

        }

        public override void ViewDidAppear(bool animated)
        {
            Task.Run(() =>
            {
                triggerMigration();
            });

            titleLabel.Text = "Starting migration!";
        }

        private void triggerMigration()
        {
            var migrationResult = DatabaseHelper.Migrate(
                                      ServiceContainer.Resolve<IPlatformUtils>().SQLiteInfo,
                                      DatabaseHelper.GetDatabaseDirectory(),
                                      oldVersion, newVersion,
                                      setProgress
                                  );

            if (migrationResult)
            {
                RxChain.Send(new DataMsg.InitStateAfterMigration());
            }
            else
            {
                // TODO: show error message
                titleLabel.Text = "Oh no, something went wrong!";
            }
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
            });
        }
    }
}

