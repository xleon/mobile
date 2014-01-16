using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Joey
{
    [Activity (Label = "LoginActivity", MainLauncher = true, Theme = "@style/Theme.Login")]
    public class LoginActivity : Activity
    {
        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.Login);
            // Create your application here

            EditText passwordField = FindViewById<EditText> (Resource.Id.userPassword);
            CheckBox hidePassword = FindViewById<CheckBox> (Resource.Id.hidePassword);
            hidePassword.Click += (o, e) => {
                if (hidePassword.Checked)
                    passwordField.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationPassword;
                else
                    passwordField.InputType = Android.Text.InputTypes.ClassText | Android.Text.InputTypes.TextVariationVisiblePassword;
            };
        }
    }
}

