using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Toggl.Phoebe.Tests.Data
{
    [TestFixture]
    public class ModelValidationTest : Test
    {
        private class ValidatingModel : Model
        {
            private static string GetPropertyName<T> (Expression<Func<ValidatingModel, T>> expr)
            {
                return expr.ToPropertyName ();
            }

            private string email;
            public static readonly string PropertyEmail = GetPropertyName ((m) => m.Email);

            public string Email {
                get { return email; }
                set {
                    if (email == value)
                        return;

                    ChangePropertyAndNotify (PropertyEmail, delegate {
                        email = value;
                    });
                }
            }

            private bool isSecured;
            public static readonly string PropertyIsSecured = GetPropertyName ((m) => m.IsSecured);

            public bool IsSecured {
                get { return isSecured; }
                set {
                    if (isSecured == value)
                        return;

                    ChangePropertyAndNotify (PropertyIsSecured, delegate {
                        isSecured = value;
                    });
                }
            }

            private string password;
            public static readonly string PropertyPassword = GetPropertyName ((m) => m.Password);

            public string Password {
                get { return password; }
                set {
                    if (password == value)
                        return;

                    ChangePropertyAndNotify (PropertyPassword, delegate {
                        password = value;
                    });
                }
            }

            protected override void Validate (ValidationContext ctx)
            {
                base.Validate (ctx);

                if (ctx.HasChanged (PropertyEmail)) {
                    if (String.IsNullOrWhiteSpace (Email)) {
                        ctx.AddError (PropertyEmail, "Cannot be empty");
                    }
                    if (Email == null || !Email.Contains ("@")) {
                        ctx.AddError (PropertyEmail, "Must contain @");
                    }
                }

                if (ctx.HasChanged (PropertyIsSecured)
                    || ctx.HasChanged (PropertyPassword)) {
                    ctx.ClearErrors (PropertyPassword);
                    if (IsSecured) {
                        if (String.IsNullOrEmpty (Password) || Password != "letmein") {
                            ctx.AddError (PropertyPassword, "Invalid password");
                        }
                    }
                }
            }
        }

        [Test]
        public void TestDefault ()
        {
            var model = Model.Update (new ValidatingModel ());
            Assert.IsFalse (model.IsValid, "Model must be invalid.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyEmail, "Cannot be empty")),
                "Must have a single email error.");
        }

        [Test]
        public void TestAutomaticValidation ()
        {
            var model = Model.Update (new ValidatingModel () {
                Email = "here@dragons.org",
            });
            Assert.IsTrue (model.IsValid, "Must be valid for initial data.");
            Assert.IsEmpty (model.Errors, "Must not have any errors for initial data.");

            model.Password = "wrong";
            Assert.IsTrue (model.IsValid, "Must be valid for wrong password and not secured.");
            Assert.IsEmpty (model.Errors, "Must not have any errors for wrong password and not secured.");

            model.IsSecured = true;
            Assert.IsFalse (model.IsValid, "Must be invalid for wrong password.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyPassword, "Invalid password")),
                "Must have a single password error.");

            model.Password = null;
            model.Email = "   ";
            Assert.IsFalse (model.IsValid, "Model must be invalid for empty email.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyEmail, "Cannot be empty")),
                "Must have a single email empty error.");

            model.IsSecured = true;
            Assert.IsFalse (model.IsValid, "Model must be invalid for empty email and invalid password.");
            Assert.AreEqual (new Dictionary<string, string> () {
                { ValidatingModel.PropertyEmail, "Cannot be empty" },
                { ValidatingModel.PropertyPassword, "Invalid password" },
            }, model.Errors, "Must have an email and password error");

            model.Email = "code@example.com";
            model.Password = "letmein";
            Assert.IsTrue (model.IsValid, "Model must be valid for valid email and correct password");
            Assert.IsEmpty (model.Errors, "Errors must be empty for valid email and correct password");
        }

        [Test]
        public void TestErrorClearing ()
        {
            var model = Model.Update (new ValidatingModel ());
            Assert.IsFalse (model.IsValid, "Model must be invalid.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyEmail, "Cannot be empty")),
                "Must have a single email error.");

            model.Email = "here@dragons.org";
            Assert.IsTrue (model.IsValid, "Model must be valid for valid email");
            Assert.IsEmpty (model.Errors, "Errors must be empty for valid email");
        }

        [Test]
        public void TestCompositeErrorClearing ()
        {
            var model = Model.Update (new ValidatingModel () {
                Email = "here@dragons.org",
                IsSecured = true,
            });

            Assert.IsFalse (model.IsValid, "Model must be invalid.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyPassword, "Invalid password")),
                "Must have a single password error.");

            model.Password = "letmein";
            Assert.IsTrue (model.IsValid, "Model must be valid for valid email and correct password");
            Assert.IsEmpty (model.Errors, "Errors must be empty for valid email and correct password");
        }

        [Test]
        public void TestManualValidation ()
        {
            var model = new ValidatingModel ();
            model.Validate ();

            Assert.IsFalse (model.IsValid, "Model must be invalid.");
            Assert.That (model.Errors, Has.Exactly (1).EqualTo (
                new KeyValuePair<string, string> (ValidatingModel.PropertyEmail, "Cannot be empty")),
                "Must have a single email error.");

            model.Email = "code@example.com";
            model.Validate ();
            Assert.IsTrue (model.IsValid, "Model must be valid for valid email");
            Assert.IsEmpty (model.Errors, "Errors must be empty for valid email");
        }
    }
}
