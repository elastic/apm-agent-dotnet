# Builds a docker image containing all of the assemblies needed to
# auto instrument the APM agent using startup hooks.
#
# Assumes that the agent 

FROM busybox

RUN mkdir /usr/agent

# Assumes that the agent directory has been built.
# Run build.[bat|sh] agent-zip to build it
COPY . /usr/agent