using System;
using Toggl.Phoebe.Net;
using MonoTouch.UIKit;
using MonoTouch.MessageUI;

namespace Toggl.Ross.Views
{
    public static class AuthErrorAlert
    {
        public static void Show (UIViewController controller, string email, AuthResult res, Mode mode, bool googleAuth=false)
        {
            switch (res) {
            case AuthResult.InvalidCredentials:
                if (mode == Mode.Login && !googleAuth) {
                    new UIAlertView (
                        "AuthErrorLoginTitle".Tr (),
                        "AuthErrorLoginMessage".Tr (),
                        null, "AuthErrorOk".Tr ()).Show ();
                } else if (mode == Mode.Login && googleAuth) {
                    new UIAlertView (
                        "AuthErrorGoogleLoginTitle".Tr (),
                        "AuthErrorGoogleLoginMessage".Tr (),
                        null, "AuthErrorOk".Tr ()).Show ();
                } else if (mode == Mode.Signup) {
                    new UIAlertView (
                        "AuthErrorSignupTitle".Tr (),
                        "AuthErrorSignupMessage".Tr (),
                        null, "AuthErrorOk".Tr ()).Show ();
                }
                break;
            case AuthResult.NoDefaultWorkspace:
                if (MFMailComposeViewController.CanSendMail) {
                    var dia = new UIAlertView (
                        "AuthErrorNoWorkspaceTitle".Tr (),
                        "AuthErrorNoWorkspaceMessage".Tr (),
                        null, "AuthErrorNoWorkspaceCancel".Tr (),
                        "AuthErrorNoWorkspaceOk".Tr ());
                    dia.Clicked += (sender, e) => {
                        if (e.ButtonIndex == 1) {
                            var mail = new MFMailComposeViewController ();
                            mail.SetToRecipients (new[] { "AuthErrorNoWorkspaceEmail".Tr () });
                            mail.SetSubject ("AuthErrorNoWorkspaceSubject".Tr ());
                            mail.SetMessageBody (String.Format ("AuthErrorNoWorkspaceBody".Tr (), email), false);
                            mail.Finished += delegate {
                                controller.DismissViewController(true, null);
                            };

                            controller.PresentViewController (mail, true, null);
                        }
                    };
                    dia.Show ();
                } else {
                    new UIAlertView (
                        "AuthErrorNoWorkspaceTitle".Tr (),
                        "AuthErrorNoWorkspaceMessage".Tr (),
                        null, "AuthErrorOk".Tr ()).Show();
                }
                break;
            case AuthResult.NetworkError:
                new UIAlertView (
                    "AuthErrorNetworkTitle".Tr (),
                    "AuthErrorNetworkMessage".Tr (),
                    null, "AuthErrorOk".Tr ()).Show ();
                break;
            default:
                new UIAlertView (
                    "AuthErrorSystemTitle".Tr (),
                    "AuthErrorSystemMessage".Tr (),
                    null, "AuthErrorOk".Tr ()).Show ();
                break;
            }
        }

        public enum Mode {
            Login,
            Signup
        }
    }
}
