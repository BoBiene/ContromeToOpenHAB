#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ContromeToOpenHAB/ContromeToOpenHAB.csproj", "ContromeToOpenHAB/"]
RUN dotnet restore "ContromeToOpenHAB/ContromeToOpenHAB.csproj"
COPY . .
WORKDIR "/src/ContromeToOpenHAB"
RUN dotnet build "ContromeToOpenHAB.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContromeToOpenHAB.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContromeToOpenHAB.dll"]