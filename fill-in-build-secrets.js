
// config
var buildFileName = "Phoebe/Build.cs";
var secrets = {
	// "string to replace" : "secret env variable"
	"{GMC_SENDER_ID}" : "GCM_SENDER_ID",
	"{RAYGUN_API_KEY}" : "RAYGUN_API_KEY",
};


console.log("Replacing " + Object.keys(secrets).length + " secrets");

var fs = require("fs");

var data = fs.readFileSync(buildFileName, "utf8");


for(var key in secrets)
{
	var secret = getSecret(secrets[key]);

	data = data.replace(key, secret);
}

fs.writeFileSync(buildFileName, data);



function getSecret(name)
{
	var value = process.env[name];
	if (value !== undefined)
	{
		return value;
	}
	throw("Could not find secret variable: " + name);
}