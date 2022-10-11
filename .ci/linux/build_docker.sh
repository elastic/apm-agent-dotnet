BUILD_PROPS="src/Directory.Build.props"
ZIP_FILE_BASE_PATH=${BASE_DIR}/build/output/

DOTNET_AGENT_VERSION=$(grep "VersionPrefix" ${BUILD_PROPS} | sed 's#<.*>\(.*\)<.*>#\1#' | tr -d " ")
  if [ -z "${DOTNET_AGENT_VERSION}" ] ; then
    echo 'ERROR: DOTNET_AGENT_VERSION could not be calculated.' && exit 1
  fi

echo 'agent version: ' ${DOTNET_AGENT_VERSION}

AGENT_ZIP_FILE=${ZIP_FILE_BASE_PATH}elastic_apm_profiler_${DOTNET_AGENT_VERSION}-linux-x64.zip

echo 'agent path: ' ${AGENT_ZIP_FILE}


docker build . \
  --build-arg AGENT_ZIP_FILE=${AGENT_ZIP_FILE}
