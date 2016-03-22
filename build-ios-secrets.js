
var replacer = require("./node/secret-replace.js");

replacer.replaceSecrets("Phoebe/Build.cs", {
	// "string to replace" : "secret env variable"
	"{RAYGUN_API_KEY_IOS}" : "RAYGUN_API_KEY_IOS",
	"{TESTFAIRY_API_TOKEN}" : "TESTFAIRY_API_TOKEN",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
    "{REVERSED_CLIENT_ID}" : "REVERSED_CLIENT_ID",
});

replacer.replaceSecrets("Ross/GoogleService-Info.plist", {
	// "string to replace" : "secret env variable"
	"{CLIENT_ID}" : "CLIENT_ID",
	"{REVERSED_CLIENT_ID}" : "REVERSED_CLIENT_ID",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
});

replacer.replaceSecrets("Ross/Info.plist", {
	// "string to replace" : "secret env variable"
	"{CF_BUNDLE_URL_SCHEMES}" : "CF_BUNDLE_URL_SCHEMES",
});