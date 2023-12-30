FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SierraSam/SierraSam.csproj", "SierraSam/"]
RUN dotnet restore "SierraSam/SierraSam.csproj"
COPY . .
WORKDIR "/src/SierraSam"
RUN dotnet build "SierraSam.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SierraSam.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SierraSam.dll"]
