#!/bin/bash

for dsym in $2/*.app.dSYM/Contents/Resources/DWARF/*
do
	echo "Uploading: $dsym"
	curl -F dsym=@$dsym -F projectRoot=$1 https://upload.bugsnag.com/
done
