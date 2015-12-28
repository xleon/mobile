var fs = require("fs");
var path = require("path");
var exec = require('child_process').exec;

fs.watch(".", function(ev, filename) {
    if (path.extname(filename) === ".mmd") {
        exec("mermaid " + filename + " -w 980", function(error, stdout, stderr) {
            console.log("Rendered: " + filename);
        });
    }
})
