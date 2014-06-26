
using System;
using System.Collections.Generic;
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

namespace Toggl.Joey.UI.Fragments
{
    public class FeedbackFragment : Fragment
    {

        public ImageButton FeedbackPositiveButton { get; private set;}
        public ImageButton FeedbackNeutralButton { get; private set;}
        public ImageButton FeedbackNegativeButton { get; private set;}
        public Button SubmitFeedbackButton { get; private set; }
        public EditText FeedbackMessageText { get; private set; }
        public int UserMood { get; private set; }
        public String UserMessage { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.FeedbackFragment, container, false);

            FeedbackPositiveButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackPositiveButton);
            FeedbackNeutralButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNeutralButton);
            FeedbackNegativeButton = view.FindViewById<ImageButton> (Resource.Id.FeedbackNegativeButton);

            FeedbackPositiveButton.Click  += (sender, e) => OnMoodButtonClick(1);
            FeedbackNeutralButton.Click  += (sender, e) => OnMoodButtonClick(2);
            FeedbackNegativeButton.Click  += (sender, e) => OnMoodButtonClick(3);

            FeedbackMessageText = view.FindViewById<EditText> (Resource.Id.FeedbackMessageText);

            SubmitFeedbackButton = view.FindViewById<Button> (Resource.Id.SendFeedbackButton);
            SubmitFeedbackButton.Click += OnSendClick;

            return view;
        }

        public override void OnResume()
        {
            OnMoodButtonClick(UserMood);
            base.OnResume ();
        }

        void OnSendClick (object sender, EventArgs e) 
        {
            //Collect feedback message text, and users mood (alert when either is missing)
            //If user submits positive message, ask if they want to insert it to app store (google play store??) and if so then copy to clipboard.
            //When succesfully sent, reset the form and navigate away, also display toast that it succeeded.
            
        }

        void OnMoodButtonClick (int mood)
        {
            UserMood = mood;
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
    }
}

