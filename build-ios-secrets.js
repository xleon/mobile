
var replacer = require("./node/secret-replace.js");

replacer.replaceSecrets("Phoebe/Build.cs", {
	// "string to replace" : "secret env variable"
	"{XAMARIN_INSIGHTS_API_KEY_IOS}" : "XAMARIN_INSIGHTS_API_KEY_IOS",
	"{TESTFAIRY_API_TOKEN}" : "TESTFAIRY_API_TOKEN",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
	"{GOOGLE_CLIENT_ID}" : "GOOGLE_CLIENT_ID",
});

replacer.replaceSecrets("Ross/GoogleService-Info.plist", {
	// "string to replace" : "secret env variable"
	"{GOOGLE_CLIENT_ID}" : "GOOGLE_CLIENT_ID",
	"{GOOGLE_REVERSED_CLIENT_ID}" : "GOOGLE_REVERSED_CLIENT_ID",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
    "{GOOGLE_APP_ID}": "GOOGLE_APP_ID",
    "{GOOGLE_PROJECT_ID}": "GOOGLE_PROJECT_ID",
});

replacer.replaceSecrets("Ross/Info.plist", {
	// "string to replace" : "secret env variable"
	"{GOOGLE_REVERSED_CLIENT_ID}" : "GOOGLE_REVERSED_CLIENT_ID",
});