# Stage 1: extract the profiler zip
# wolfi-base ships with unzip; no additional packages needed
FROM cgr.dev/chainguard/wolfi-base AS extractor

ARG AGENT_ZIP_FILE
COPY ${AGENT_ZIP_FILE} /tmp/apm-dotnet-agent.zip
RUN mkdir -p /usr/agent && \
    unzip /tmp/apm-dotnet-agent.zip -d /usr/agent/apm-dotnet-agent && \
    rm /tmp/apm-dotnet-agent.zip && \
    chmod -R go+r /usr/agent/apm-dotnet-agent

# Stage 2: minimal runtime image containing only the agent files
# Wolfi provides nonroot user at uid/gid 65532
FROM cgr.dev/chainguard/wolfi-base
COPY --chown=65532:65532 --from=extractor /usr/agent/apm-dotnet-agent /usr/agent/apm-dotnet-agent
USER 65532:65532
