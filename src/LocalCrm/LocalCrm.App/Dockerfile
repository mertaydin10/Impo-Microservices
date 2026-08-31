FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5209

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/LocalCrm/LocalCrm.App/LocalCrm.App.csproj", "src/LocalCrm/LocalCrm.App/"]
COPY ["src/LocalCrm/LocalCrm.Shared/LocalCrm.Shared.csproj", "src/LocalCrm/LocalCrm.Shared/"]
RUN dotnet restore "src/LocalCrm/LocalCrm.App/LocalCrm.App.csproj"
COPY . .
WORKDIR "/src/src/LocalCrm/LocalCrm.App"
RUN dotnet build "LocalCrm.App.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LocalCrm.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocalCrm.App.dll"]
