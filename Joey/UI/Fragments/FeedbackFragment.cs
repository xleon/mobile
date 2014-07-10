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
        public ImageButton FeedbackPositiveButton { get; private set;}
        public ImageButton FeedbackNeutralButton { get; private set;}
        public ImageButton FeedbackNegativeButton { get; private set;}
        public Button SubmitFeedbackButton { get; private set; }
        public EditText FeedbackMessageEditText { get; private set; }
        public int FeedbackRating { get; private set; }
        public String FeedbackMessage { get; private set; }
        private const int RatingNotSet = 0;
        private const int RatingPositive = 1;
        private const int RatingNeutral = 2;
        private const int RatingNegative = 3;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);

            FeedbackPositiveButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackPositiveButton);
            FeedbackNeutralButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNeutralButton);
            FeedbackNegativeButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNegativeButton);

            FeedbackPositiveButton.Click  += (sender, e) => SetRating (RatingPositive);
            FeedbackNeutralButton.Click  += (sender, e) => SetRating (RatingNeutral);
            FeedbackNegativeButton.Click  += (sender, e) => SetRating (RatingNegative);

            FeedbackMessageEditText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText).SetFont (Font.Roboto);
            FeedbackMessageEditText.AfterTextChanged += OnEdit;

            SubmitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton).SetFont (Font.Roboto);
            SubmitFeedbackButton.Click += OnSendClick;
            if (savedInstanceState != null) {
                SetRating (savedInstanceState.GetInt ("rating"));
            } else {
                SetRating (RatingNotSet);
            }
            ValidateForm ();
            return view;
        }

        public override void OnResume ()
        {
            SetRating (FeedbackRating);
            ValidateForm ();
            base.OnResume ();
        }

        private async void OnSendClick (object sender, EventArgs e) 
        {
            SubmitFeedbackButton.Enabled = false;
            FeedbackMessageEditText.Enabled = false;
            FeedbackPositiveButton.Enabled = false;
            FeedbackNeutralButton.Enabled = false;
            FeedbackNegativeButton.Enabled = false;

            SubmitFeedbackButton.SetText (Resource.String.SendFeedbackButtonActiveText);
            bool send = await SendFeedbackData (FeedbackMessage, FeedbackRating);
            if (send == true) {
                if (FeedbackRating == RatingPositive) {
                    var args = new Bundle ();
                    args.PutString ("feedbackMessage", FeedbackMessage);
                    new AskPublishToAppStore (args).Show (FragmentManager);
                } else {
                    new ThankForFeedbackDialog ().Show (FragmentManager);
                }
                ResetFeedbackForm ();
            } else {
                Context ctx = ServiceContainer.Resolve<Context> ();
                Toast toast = Toast.MakeText (ctx, Resource.String.FeedbackSendFailedText, ToastLength.Long);
                toast.Show ();
                EnableForm ();
            }
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            if (FeedbackRating != RatingNotSet) {
                outState.PutInt ("rating", FeedbackRating);
            }
        }

        private void ValidateForm ()
        {
            FeedbackMessage = FeedbackMessageEditText.Text;
            bool enabled = false;
            if (FeedbackMessage.Length == 0 || FeedbackRating == RatingNotSet) {
                enabled = false;
            } else {
                enabled = true;
            }
            SubmitFeedbackButton.Enabled = enabled;
        }

        private bool prevSendResult;
        private async Task<bool> SendFeedbackData ( string feedback, int rating ) {
            await Task.Delay (TimeSpan.FromSeconds(1));
            prevSendResult = !prevSendResult;
            return prevSendResult;
        }

        private void SetRating (int rating)
        {
            FeedbackRating = rating;
            ResetRatingButtonImages ();
            if (FeedbackRating  == RatingPositive) {
                FeedbackPositiveButton.SetImageResource (Resource.Drawable.IcFeedbackPositiveActive);
            } else if (FeedbackRating == RatingNeutral) {
                FeedbackNeutralButton.SetImageResource (Resource.Drawable.IcFeedbackNeutralActive);
            } else if (FeedbackRating == RatingNegative) {
                FeedbackNegativeButton.SetImageResource (Resource.Drawable.IcFeedbackNegativeActive);
            }
            ValidateForm ();
        }

        private void ResetRatingButtonImages ()
        {
            FeedbackPositiveButton.SetImageResource (Resource.Drawable.IcFeedbackPositive);
            FeedbackNeutralButton.SetImageResource (Resource.Drawable.IcFeedbackNeutral);
            FeedbackNegativeButton.SetImageResource (Resource.Drawable.IcFeedbackNegative);
        }

        private void OnEdit (object sender, EventArgs e)
        {
            ValidateForm ();
        }

        private void ResetFeedbackForm ()
        {
            SetRating (RatingNotSet);
            FeedbackMessageEditText.Text = "";
            EnableForm ();
        }

        private void EnableForm (){
            SubmitFeedbackButton.SetText (Resource.String.SendFeedbackButtonText);
            SubmitFeedbackButton.Enabled = true;
            FeedbackMessageEditText.Enabled = true;
            FeedbackPositiveButton.Enabled = true;
            FeedbackNeutralButton.Enabled = true;
            FeedbackNegativeButton.Enabled = true;
        }
    }

    public class ThankForFeedbackDialog : BaseDialogFragment {


        public ThankForFeedbackDialog ()
        {
        }

        public ThankForFeedbackDialog (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public void Show (FragmentManager fragmentManager)
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

        private readonly Bundle args;
        public AskPublishToAppStore ()
        {
        }

        public AskPublishToAppStore (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public AskPublishToAppStore (Bundle arguments)
        {
            args = arguments;
        }

        public void Show (FragmentManager fragmentManager)
        {
            new AskPublishToAppStore (args).Show (fragmentManager, "askpublishtoappstore_dialog");
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
            Context ctx = ServiceContainer.Resolve<Context> ();
            ClipboardManager clipboard = (ClipboardManager) ctx.GetSystemService (Context.ClipboardService);
            ClipData clip = ClipData.NewPlainText (Resource.String.AppName.ToString(), args.GetString ("feedbackMessage"));
            clipboard.PrimaryClip = clip;

            Toast toast = Toast.MakeText (ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();

            StartActivity (new Intent (
                Intent.ActionView,
                Android.Net.Uri.Parse (Toggl.Phoebe.Build.GooglePlayUrl)
            ));
        }
    }
}

