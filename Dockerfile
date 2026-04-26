FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SedesBarrioNorte.csproj", "./"]
RUN dotnet restore "./SedesBarrioNorte.csproj"
COPY . .
WORKDIR /src/.
RUN dotnet build "SedesBarrioNorte.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SedesBarrioNorte.csproj" -c Release -o /app/publish

FROM base AS final

# libgdi plus es para el fast-report
RUN apt update -y
RUN apt install -y software-properties-common
RUN add-apt-repository -y universe
RUN apt update -y
RUN apt install -y libgdiplus

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SedesBarrioNorte.dll"]
