using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using XPlatUtils;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Fragment = Android.Support.V4.App.Fragment;


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
        public int FeedbackRating { get; private set; }
        public String FeedbackMessage { get; private set; }
        private const int RatingNotSet = 0;
        private const int RatingPositive = 1;
        private const int RatingNeutral = 2;
        private const int RatingNegative = 3;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);
            ctx = ServiceContainer.Resolve<Context> ();

            FeedbackPositiveButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackPositiveButton);
            FeedbackNeutralButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNeutralButton);
            FeedbackNegativeButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNegativeButton);

            FeedbackPositiveButton.Click  += (sender, e) => SetRating(RatingPositive);
            FeedbackNeutralButton.Click  += (sender, e) => SetRating(RatingNeutral);
            FeedbackNegativeButton.Click  += (sender, e) => SetRating(RatingNegative);

            FeedbackMessageEditText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText).SetFont (Font.Roboto);
            FeedbackMessageEditText.AfterTextChanged += OnEdit;

            SubmitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton).SetFont (Font.Roboto);
            SubmitFeedbackButton.Click += OnSendClick;
            if(savedInstanceState != null)
                SetRating(savedInstanceState.GetInt("rating"));
            else
                SetRating (RatingNotSet);
            ValidateForm ();
            return view;
        }

        public override void OnResume()
        {
            SetRating(FeedbackRating);
            ValidateForm ();
            base.OnResume ();
        }

        private async void OnSendClick (object sender, EventArgs e) 
        {
            SubmitFeedbackButton.Enabled = false;
            SubmitFeedbackButton.SetText(Resource.String.SendFeedbackButtonActiveText);
            bool send = await SendFeedbackData (FeedbackMessage, FeedbackRating);
            if (send == true) {
                if (FeedbackRating == RatingPositive)
                    AskPublishToAppStore ();
                else
                    ThankForFeedback();
            }
        }

        public override void OnSaveInstanceState (Bundle outState)
        {
            base.OnSaveInstanceState (outState);
            if (FeedbackRating != null) {
                outState.PutInt ("rating", FeedbackRating);
            }
        }

        private void ValidateForm()
        {
            FeedbackMessage = FeedbackMessageEditText.Text;
            bool enabled = false;
            if (FeedbackMessage.Length == 0 || FeedbackRating == RatingNotSet)
                enabled = false;
            else 
                enabled = true;
            SubmitFeedbackButton.Enabled = enabled;
        }

        private bool prevSendResult;
        private async Task<bool> SendFeedbackData ( string feedback, int rating ) {
            await Task.Delay(TimeSpan.FromSeconds(1));
            prevSendResult = true;
            return prevSendResult;
        }

        private void SetRating (int rating)
        {
            FeedbackRating = rating;
            ResetRatingButtonImages ();
            if (FeedbackRating  == RatingPositive) {
                FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositiveActive);
            } else if (FeedbackRating == RatingNeutral) {
                FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutralActive);
            } else if (FeedbackRating == RatingNegative) {
                FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegativeActive);
            }
            ValidateForm ();
        }

        private void ResetRatingButtonImages()
        {
            FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositive);
            FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutral);
            FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegative);
        }

        private void AskPublishToAppStore()
        {
            new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackAskPublishTitle)
                .SetMessage (Resource.String.FeedbackAskPublishMessage)
                .SetCancelable (true)
                .SetNegativeButton(Resource.String.FeedbackAskPublishCancel, (IDialogInterfaceOnClickListener)null )
                .SetPositiveButton (Resource.String.FeedbackAskPublishOK, OnCopyOkClicked)
                .Show ();
        }

        private void ThankForFeedback()
        {
            new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackThankYouTitle)
                .SetMessage (Resource.String.FeedbackThankYouMessage)
                .SetCancelable (true)
                .SetPositiveButton (Resource.String.FeedbackThankYouOK, (IDialogInterfaceOnClickListener)null)
                .Show ();
            ResetFeedbackForm ();
        }

        private void OnCopyOkClicked (object sender, DialogClickEventArgs e)
        {
            Android.Content.ClipboardManager clipboard = (Android.Content.ClipboardManager) ctx.GetSystemService(Context.ClipboardService); 
            Android.Content.ClipData clip = Android.Content.ClipData.NewPlainText(Resource.String.AppName.ToString(), FeedbackMessage);
            clipboard.PrimaryClip = clip;

            Toast toast = Toast.MakeText(ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();

            StartActivity (new Intent (
                Intent.ActionView,
                Android.Net.Uri.Parse (Toggl.Phoebe.Build.GooglePlayUrl)
            ));
            ResetFeedbackForm ();
        }

        private void OnEdit(object sender, EventArgs e)
        {
            ValidateForm ();
        }

        private void ResetFeedbackForm(){
            SetRating (RatingNotSet);
            FeedbackMessageEditText.Text = "";
            SubmitFeedbackButton.SetText(Resource.String.SendFeedbackButtonText);
        }

    }
}

