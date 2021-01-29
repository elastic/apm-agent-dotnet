FROM mcr.microsoft.com/dotnet/sdk:5.0.102-ca-patch-buster-slim

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:${DOTNET_ROOT}/tools:${DOTNET_ROOT}

# Download .Net SDK installer script
RUN curl -s -O -L https://dotnet.microsoft.com/download/dotnet-core/scripts/v1/dotnet-install.sh

# Install .Net SDKs
RUN /bin/bash ./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version "3.1.100"
RUN /bin/bash ./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version "2.1.505"

# Install docker
RUN apt update \
 && apt-get -qq install -y apt-transport-https ca-certificates curl \
    gnupg2 software-properties-common --no-install-recommends \
 && curl -fsSL https://download.docker.com/linux/debian/gpg | apt-key add - \
 && add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/debian $(lsb_release -cs) stable" \
 && apt -qq update \
 && apt-get -qq install -y docker-ce docker-ce-cli containerd.io \
    --no-install-recommends \
 && rm -rf /var/lib/apt/lists/*