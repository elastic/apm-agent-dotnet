FROM mcr.microsoft.com/dotnet/sdk:6.0
RUN mkdir /usr/agent
ARG AGENT_ZIP_FILE
COPY ${AGENT_ZIP_FILE} /usr/agent/apm-dotnet-agent.zip

RUN apt-get update
RUN apt-get install unzip

RUN unzip /usr/agent/apm-dotnet-agent.zip -d /usr/agent/apm-dotnet-agent