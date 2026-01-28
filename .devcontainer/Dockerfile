FROM mcr.microsoft.com/dotnet/sdk:10.0-noble

# Install utilities and EF Core tools in a single layer
RUN apt-get update && apt-get install -y --no-install-recommends \
    git \
    curl \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/* \
    && dotnet tool install --global dotnet-ef --version 10.0.1

ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /workspace
