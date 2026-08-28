FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5057

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/LocalCrm/LocalCrm.Api/LocalCrm.Api.csproj", "src/LocalCrm/LocalCrm.Api/"]
COPY ["src/LocalCrm/LocalCrm.Shared/LocalCrm.Shared.csproj", "src/LocalCrm/LocalCrm.Shared/"]
RUN dotnet restore "src/LocalCrm/LocalCrm.Api/LocalCrm.Api.csproj"
COPY . .
WORKDIR "/src/src/LocalCrm/LocalCrm.Api"
RUN dotnet build "LocalCrm.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LocalCrm.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocalCrm.Api.dll"]
