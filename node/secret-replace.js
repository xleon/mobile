
module.exports = {
	replaceSecrets : (filename, secrets) =>
	{
		console.log(`Replacing ${Object.keys(secrets).length} secrets in '${filename}'`);

		var fs = require("fs");

		var data = fs.readFileSync(filename, "utf8");

		data = replace(data, secrets);

		fs.writeFileSync(filename, data);
	}
};


function replace(data, secrets)
{
	for(var key in secrets)
	{
		var secret = getSecret(secrets[key]);

		data = data.replace(key, secret);
	}

	return data;
}

function getSecret(name)
{
	var value = process.env[name];
	if (value !== undefined)
	{
		return value;
	}
	throw `Could not find secret variable: ${name}`;
}