FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5185
EXPOSE 5186

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Spectator/Spectator.Api/Spectator.Api.csproj", "src/Spectator/Spectator.Api/"]
RUN dotnet restore "src/Spectator/Spectator.Api/Spectator.Api.csproj"
COPY . .
WORKDIR "/src/src/Spectator/Spectator.Api"
RUN dotnet build "Spectator.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Spectator.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Spectator.Api.dll"]
