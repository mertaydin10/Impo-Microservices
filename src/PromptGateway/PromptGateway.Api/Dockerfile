FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5047

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/PromptGateway/PromptGateway.Api/PromptGateway.Api.csproj", "src/PromptGateway/PromptGateway.Api/"]
RUN dotnet restore "src/PromptGateway/PromptGateway.Api/PromptGateway.Api.csproj"
COPY . .
WORKDIR "/src/src/PromptGateway/PromptGateway.Api"
RUN dotnet build "PromptGateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PromptGateway.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PromptGateway.Api.dll"]
