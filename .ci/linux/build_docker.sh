BUILD_PROPS="src/Directory.Build.props"
ZIP_FILE_BASE_PATH="/build/output/"

readonly NAMESPACE="observability"

DOTNET_AGENT_VERSION=$(dotnet minver -t=v -p=canary.0 -v=e)
if [ -z "${DOTNET_AGENT_VERSION}" ] ; then
  echo 'ERROR: DOTNET_AGENT_VERSION could not be calculated.' && exit 1
fi

echo 'agent version: ' ${DOTNET_AGENT_VERSION}

AGENT_ZIP_FILE=${ZIP_FILE_BASE_PATH}elastic_apm_profiler_${DOTNET_AGENT_VERSION}-linux-x64.zip

echo 'agent path: ' ${AGENT_ZIP_FILE}

docker build . -t docker.elastic.co/$NAMESPACE/apm-agent-dotnet:$DOTNET_AGENT_VERSION \
  --build-arg AGENT_ZIP_FILE=${AGENT_ZIP_FILE}
