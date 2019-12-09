FROM mcr.microsoft.com/dotnet/core/sdk:3.1.100

ENV HOME=/tmp
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:${DOTNET_ROOT}/tools:${HOME}/bin:${DOTNET_ROOT}

# Download .Net SDK installer script
RUN curl -s -O -L https://dot.net/v1/dotnet-install.sh

# Install .Net SDKs
RUN /bin/bash ./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version 2.1.505

# Install dotnet-xunit-to-junit
RUN dotnet tool install --tool-path /usr/share/dotnet/tools dotnet-xunit-to-junit --version 0.3.1
