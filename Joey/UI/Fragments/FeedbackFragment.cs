
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Fragment = Android.Support.V4.App.Fragment;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class FeedbackFragment : Fragment
    {
        private Context ctx;
        public ImageButton FeedbackPositiveButton { get; private set;}
        public ImageButton FeedbackNeutralButton { get; private set;}
        public ImageButton FeedbackNegativeButton { get; private set;}
        public Button SubmitFeedbackButton { get; private set; }
        public EditText FeedbackMessageEditText { get; private set; }
        public int FeedbackMood { get; private set; }
        public String FeedbackMessage { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);
            ctx = ServiceContainer.Resolve<Context> ();

            FeedbackPositiveButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackPositiveButton);
            FeedbackNeutralButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNeutralButton);
            FeedbackNegativeButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNegativeButton);

            FeedbackPositiveButton.Click  += (sender, e) => SetMood(1);
            FeedbackNeutralButton.Click  += (sender, e) => SetMood(2);
            FeedbackNegativeButton.Click  += (sender, e) => SetMood(3);

            FeedbackMessageEditText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText);

            SubmitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton);
            SubmitFeedbackButton.Click += OnSendClick;

            return view;

        }

        public override void OnResume()
        {
            SetMood(FeedbackMood);
            base.OnResume ();
        }

        void OnSendClick (object sender, EventArgs e) 
        {
            //Collect feedback message text, and users mood (alert when either is missing)
            //If user submits positive message, ask if they want to insert it to app store (google play store??) and if so then copy to clipboard.
            //When succesfully sent, reset the form and navigate away, also display toast that it succeeded.
            FeedbackMessage = FeedbackMessageEditText.Text;


            if (FeedbackMessage.Length == 0)
                FormNotValidAlert (1);
            else if (FeedbackMood == 0)
                FormNotValidAlert (2);
            else if (FeedbackMessage.Length > 0 && FeedbackMood > 0) { //valid
                SendFeedbackData (FeedbackMessage, FeedbackMood);
                AskCopyToClipboard ();
                if (FeedbackMood == 1) { //in case of positive feedback

                }
            }
        }

        private bool prevSendResult;
        private async Task<bool> SendFeedbackData ( string feedback, int mood ) {
            await Task.Delay(TimeSpan.FromSeconds(5));
            prevSendResult = !prevSendResult;
            return prevSendResult;
        }

        void SetMood (int mood)
        {
            FeedbackMood = mood;
            ResetMoodButtonImages ();
            if (mood  == 1) {
                FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositiveActive);
            } else if (mood == 2) {
                FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutralActive);
            } else if (mood == 3) {
                FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegativeActive);
            }
        }

        void ResetMoodButtonImages()
        {
            FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositive);
            FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutral);
            FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegative);
        }

        void FormNotValidAlert(int type)
        {
            int AlertMessage;
            if (type == 1)
                AlertMessage = Resource.String.FeedbackAlertNoText;
            else 
                AlertMessage = Resource.String.FeedbackAlertNoMood;

            new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackFormNotValidTitle)
                .SetMessage (AlertMessage)
                .SetCancelable (false)
                .SetPositiveButton (Resource.String.FeedbackAlertDialogOk, OnOkClicked)
                .Show ();
        }

        void AskCopyToClipboard()
        {
            new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackCopyToClipboardTitle)
                .SetMessage (Resource.String.FeedbackCopyToClipboardMessage)
                .SetCancelable (true)
                .SetNegativeButton(Resource.String.FeedbackCopyToClipboardCancel, OnCopyCancelClicked)
                .SetPositiveButton (Resource.String.FeedbackCopyToClipboardOK, OnCopyOkClicked)
                .Show ();
        }

        private void OnOkClicked (object sender, DialogClickEventArgs e)
        {
        }

        private void OnCopyOkClicked (object sender, DialogClickEventArgs e)
        {
            Android.Content.ClipboardManager clipboard = (Android.Content.ClipboardManager) ctx.GetSystemService(Context.ClipboardService); 
            Android.Content.ClipData clip = Android.Content.ClipData.NewPlainText("Toggl", FeedbackMessage);
            clipboard.PrimaryClip = clip;

            Toast toast = Toast.MakeText(ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();
        }

        private void OnCopyCancelClicked (object sender, DialogClickEventArgs e)
        {

        }
    }
}

