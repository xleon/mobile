
// config
var buildFileName = "Phoebe/Build.cs";
var secrets = {
	// "string to replace" : "secret env variable"
	"{GMC_SENDER_ID}" : "GCM_SENDER_ID",
	"{RAYGUN_API_KEY}" : "RAYGUN_API_KEY",
};


var replacer = require("./node/secret-replace.js");

replacer.replaceSecrets(buildFileName, secrets);