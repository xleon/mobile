using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Toggl.Chandler
{
    public class TimerFragment : Fragment
    {
        private readonly SimpleTimeEntryData dataObject;
        private readonly Handler handler = new Handler ();
        private TextView DurationTextView;
        private TextView DescriptionTextView;
        private TextView ProjectTextView;
        private ImageButton ActionButton;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntryFragment, container, false);

            ActionButton = view.FindViewById<ImageButton> (Resource.Id.testButton);
            DurationTextView = view.FindViewById<TextView> (Resource.Id.DurationTextView);
            DescriptionTextView = view.FindViewById<TextView> (Resource.Id.DescriptionTextView);
            ProjectTextView = view.FindViewById<TextView> (Resource.Id.ProjectTextView);

            ActionButton.Click += OnActionButtonClicked;


//            Rebind();
            return view;
        }

        private void OnActionButtonClicked (object sender, EventArgs e)
        {
//            SendStartStopMessage ();
        }

//        private void SendStartStopMessage ()
//        {
//            Task.Run (() => {
//                var apiResult = WearableClass.NodeApi.GetConnectedNodes (googleApiClient) .Await ().JavaCast<INodeApiGetConnectedNodesResult> ();
//                var nodes = apiResult.Nodes;
//                foreach (var node in nodes) {
//                    WearableClass.MessageApi.SendMessage (googleApiClient, node.Id,
//                        Common.StartTimeEntryPath,
//                        new byte[0]);
//                }
//            });
//            SendNewData (googleApiClient);
//        }

        private void Rebind()
        {
            DescriptionTextView.Text = String.IsNullOrWhiteSpace (dataObject.Description) ? Resources.GetString (Resource.String.TimeEntryNoDescription) : dataObject.Description;
            ProjectTextView.Text = String.IsNullOrWhiteSpace (dataObject.Project) ? Resources.GetString (Resource.String.TimeEntryNoProject) : dataObject.Project;

            var dur = dataObject.GetDuration();
            DurationTextView.Text = TimeSpan.FromSeconds ((long)dur.TotalSeconds).ToString ();

            if (!dataObject.IsRunning) {
                return;
            }

            // Schedule next rebind:
            handler.RemoveCallbacks (Rebind);
            handler.PostDelayed (Rebind, 1000);
        }
    }
}

