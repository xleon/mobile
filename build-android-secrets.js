
var replacer = require("./node/secret-replace.js");

replacer.replaceSecrets("Phoebe/Build.cs", {
	// "string to replace" : "secret env variable"
	"{GMC_SENDER_ID}" : "GCM_SENDER_ID",
	"{XAMARIN_INSIGHTS_API_KEY_ANDROID}" : "XAMARIN_INSIGHTS_API_KEY_ANDROID",
	"{GOOGLE_ANALYTICS_ID}" : "GOOGLE_ANALYTICS_ID",
});