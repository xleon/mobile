using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Fragments
{
    public class FeedbackFragment : Fragment
    {
        private ImageButton feedbackPositiveButton { get; set;}
        private ImageButton feedbackNeutralButton { get; set;}
        private ImageButton feedbackNegativeButton { get; set;}
        private Button submitFeedbackButton { get; set; }
        private EditText feedbackMessageEditText { get; set; }
        private int feedbackRating { get; set; }
        private String feedbackMessage { get; set; }
        private static readonly int ratingNotSet = 0;
        private static readonly int ratingPositive = 1;
        private static readonly int ratingNeutral = 2;
        private static readonly int ratingNegative = 3;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);

            feedbackPositiveButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackPositiveButton);
            feedbackNeutralButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNeutralButton);
            feedbackNegativeButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNegativeButton);

            feedbackPositiveButton.Click  += (sender, e) => SetRating (ratingPositive);
            feedbackNeutralButton.Click  += (sender, e) => SetRating (ratingNeutral);
            feedbackNegativeButton.Click  += (sender, e) => SetRating (ratingNegative);

            feedbackMessageEditText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText).SetFont (Font.Roboto);
            feedbackMessageEditText.AfterTextChanged += OnEdit;

            submitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton).SetFont (Font.Roboto);
            submitFeedbackButton.Click += OnSendClick;
            if (savedInstanceState != null) {
                SetRating (savedInstanceState.GetInt ("rating"));
            } else {
                SetRating (ratingNotSet);
            }
            ValidateForm ();
            return view;
        }

        public override void OnResume ()
        {
            SetRating (feedbackRating);
            ValidateForm ();
            base.OnResume ();
        }

        private async void OnSendClick (object sender, EventArgs e) 
        {
            submitFeedbackButton.Enabled = false;
            feedbackMessageEditText.Enabled = false;
            feedbackPositiveButton.Enabled = false;
            feedbackNeutralButton.Enabled = false;
            feedbackNegativeButton.Enabled = false;

            submitFeedbackButton.SetText (Resource.String.SendFeedbackButtonActiveText);
            bool send = await SendFeedbackData (feedbackMessage, feedbackRating);
            if (send == true) {
                if (feedbackRating == ratingPositive) {
                    AskPublishToAppStore.Show (feedbackMessage, FragmentManager);
                } else {
                    ThankForFeedbackDialog.Show (FragmentManager);
                }
                ResetFeedbackForm ();
            } else {
                var ctx = ServiceContainer.Resolve<Context> ();
                var toast = Toast.MakeText (ctx, Resource.String.FeedbackSendFailedText, ToastLength.Long);
                toast.Show ();
                EnableForm ();
            }
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            if (feedbackRating != ratingNotSet) {
                outState.PutInt ("rating", feedbackRating);
            }
        }

        private void ValidateForm ()
        {
            feedbackMessage = feedbackMessageEditText.Text;
            bool enabled = false;
            if (feedbackMessage.Length == 0 || feedbackRating == ratingNotSet) {
                enabled = false;
            } else {
                enabled = true;
            }
            submitFeedbackButton.Enabled = enabled;
        }

        private bool prevSendResult;
        private async Task<bool> SendFeedbackData ( string feedback, int rating ) {
            await Task.Delay (TimeSpan.FromSeconds(1));
            prevSendResult = !prevSendResult;
            return prevSendResult;
        }

        private void SetRating (int rating)
        {
            feedbackRating = rating;
            ResetRatingButtonImages ();
            if (feedbackRating  == ratingPositive) {
                feedbackPositiveButton.SetImageResource (Resource.Drawable.IcFeedbackPositiveActive);
            } else if (feedbackRating == ratingNeutral) {
                feedbackNeutralButton.SetImageResource (Resource.Drawable.IcFeedbackNeutralActive);
            } else if (feedbackRating == ratingNegative) {
                feedbackNegativeButton.SetImageResource (Resource.Drawable.IcFeedbackNegativeActive);
            }
            ValidateForm ();
        }

        private void ResetRatingButtonImages ()
        {
            feedbackPositiveButton.SetImageResource (Resource.Drawable.IcFeedbackPositive);
            feedbackNeutralButton.SetImageResource (Resource.Drawable.IcFeedbackNeutral);
            feedbackNegativeButton.SetImageResource (Resource.Drawable.IcFeedbackNegative);
        }

        private void OnEdit (object sender, EventArgs e)
        {
            ValidateForm ();
        }

        private void ResetFeedbackForm ()
        {
            SetRating (ratingNotSet);
            feedbackMessageEditText.Text = "";
            EnableForm ();
        }

        private void EnableForm (){
            submitFeedbackButton.SetText (Resource.String.SendFeedbackButtonText);
            submitFeedbackButton.Enabled = true;
            feedbackMessageEditText.Enabled = true;
            feedbackPositiveButton.Enabled = true;
            feedbackNeutralButton.Enabled = true;
            feedbackNegativeButton.Enabled = true;
        }
    }

    public class ThankForFeedbackDialog : BaseDialogFragment {


        public ThankForFeedbackDialog ()
        {
        }

        public ThankForFeedbackDialog (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static void Show (FragmentManager fragmentManager)
        {
            new ThankForFeedbackDialog ().Show (fragmentManager, "thankforfeedback_dialog");
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            return new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackThankYouTitle)
                .SetMessage (Resource.String.FeedbackThankYouMessage)
                .SetCancelable (true)
                .SetPositiveButton (Resource.String.FeedbackThankYouOK, (IDialogInterfaceOnClickListener)null)
                .Create ();
        }
    }

    public class AskPublishToAppStore : BaseDialogFragment{

        private static readonly string UserMessageArgument = "com.toggl.timer.user_message";

        public AskPublishToAppStore ()
        {
        }

        public AskPublishToAppStore (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public AskPublishToAppStore (String userMessage)
        {
            var args = new Bundle ();
            args.PutString (UserMessageArgument, userMessage);
            Arguments = args;
        }

        public static void Show (String userMessage, FragmentManager fragmentManager)
        {
            new AskPublishToAppStore (userMessage).Show (fragmentManager, "askpublishtoappstore_dialog");
        }

        private String UserMessage {
            get {
                if (Arguments != null) {
                    return Arguments.GetString (UserMessageArgument);
                }
                return null;
            }
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            return new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackAskPublishTitle)
                .SetMessage (Resource.String.FeedbackAskPublishMessage)
                .SetCancelable (true)
                .SetNegativeButton (Resource.String.FeedbackAskPublishCancel, (IDialogInterfaceOnClickListener)null)
                .SetPositiveButton (Resource.String.FeedbackAskPublishOK, OnPositiveClick)
                .Create ();
        }

        private void OnPositiveClick (object sender, DialogClickEventArgs e)
        {
            var ctx = ServiceContainer.Resolve<Context> ();
            var clipboard = (ClipboardManager) ctx.GetSystemService (Context.ClipboardService);
            var clip = ClipData.NewPlainText (Resources.GetString(Resource.String.AppName), UserMessage);
            clipboard.PrimaryClip = clip;

            var toast = Toast.MakeText (ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();

            StartActivity (new Intent (
                Intent.ActionView,
                Android.Net.Uri.Parse (Toggl.Phoebe.Build.GooglePlayUrl)
            ));
        }
    }
}

