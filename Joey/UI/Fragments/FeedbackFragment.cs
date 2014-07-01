
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

            FeedbackMessageEditText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText);

            SubmitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton);
            SubmitFeedbackButton.Click += OnSendClick;
            FeedbackMessageEditText.AfterTextChanged += OnEdit;
            SetRating (RatingNotSet);
            ValidateForm ();
            return view;
        }

        public void OnEdit(object sender, EventArgs e)
        {
            ValidateForm ();
        }
        public override void OnResume()
        {
            SetRating(FeedbackRating);
            ValidateForm ();
            base.OnResume ();
        }

        private async void OnSendClick (object sender, EventArgs e) 
        {
            ValidateForm ();
                bool send = await SendFeedbackData (FeedbackMessage, FeedbackRating);
                if (send == true) {
                    if (FeedbackRating == RatingPositive)
                        AskPublishToAppStore ();
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
        private async Task<bool> SendFeedbackData ( string feedback, int Rating ) {
            await Task.Delay(TimeSpan.FromSeconds(5));
            prevSendResult = !prevSendResult;

            Toast toast = Toast.MakeText(ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();

            return prevSendResult;
        }

        private void SetRating (int Rating)
        {
            FeedbackRating = Rating;
            ResetRatingButtonImages ();
            if (FeedbackRating  == RatingPositive) {
                FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositiveActive);
            } else if (FeedbackRating == RatingNeutral) {
                FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutralActive);
            } else if (FeedbackRating == RatingNegative) {
                FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegativeActive);
            }
        }

        private void ResetRatingButtonImages()
        {
            FeedbackPositiveButton.SetImageResource(Resource.Drawable.IcFeedbackPositive);
            FeedbackNeutralButton.SetImageResource(Resource.Drawable.IcFeedbackNeutral);
            FeedbackNegativeButton.SetImageResource(Resource.Drawable.IcFeedbackNegative);
        }

        private void FormNotValidAlert(int AlertMessage)
        {
            new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.FeedbackFormNotValidTitle)
                .SetMessage (AlertMessage)
                .SetCancelable (false)
                .SetPositiveButton (Resource.String.FeedbackAlertDialogOk, (IDialogInterfaceOnClickListener)null)
                .Show ();
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

        private void OnCopyOkClicked (object sender, DialogClickEventArgs e)
        {
            Android.Content.ClipboardManager clipboard = (Android.Content.ClipboardManager) ctx.GetSystemService(Context.ClipboardService); 
            Android.Content.ClipData clip = Android.Content.ClipData.NewPlainText(Resource.String.AppName.ToString(), FeedbackMessage);
            clipboard.PrimaryClip = clip;

            Toast toast = Toast.MakeText(ctx, Resource.String.FeedbackCopiedToClipboardToast, ToastLength.Short);
            toast.Show ();
        }
    }
}

