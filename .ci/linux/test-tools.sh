#!/usr/bin/env bash
set -euxo pipefail

dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj

# install tools
dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
for i in $(find . -name '*.csproj')
do
	if [[ $i == *"AspNetFullFrameworkSampleApp.csproj"* ]]; then
		continue
	fi
	if [[ $i == *"Elastic.Apm.AspNetFullFramework.csproj"* ]]; then
		continue
	fi
	dotnet add "$i" package XunitXml.TestLogger --version 2.0.0
	dotnet add "$i" package coverlet.msbuild --version 2.5.1
done
