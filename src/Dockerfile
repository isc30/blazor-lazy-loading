# this docker file prepares a build environment for the project

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200 AS build-env

# Workaround for GitVersionTask bug in combination with .NET Core SDK 3.1.200
# (see, e.g., https://github.com/dotnet/sdk/issues/10878 and https://github.com/GitTools/GitVersion/issues/2063)
ENV MSBUILDSINGLELOADCONTEXT=1

VOLUME /repo
WORKDIR /repo
