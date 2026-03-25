# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CarManagement.Solution/CarManagement.API/CarManagement.API.csproj", "CarManagement.Solution/CarManagement.API/"]
COPY ["CarManagement.Solution/CarManagement.Common/CarManagement.Common.csproj", "CarManagement.Solution/CarManagement.Common/"]
COPY ["CarManagement.Solution/CarManagement.DataAccess/CarManagement.DataAccess.csproj", "CarManagement.Solution/CarManagement.DataAccess/"]
COPY ["CarManagement.Solution/CarManagement.Models/CarManagement.Models.csproj", "CarManagement.Solution/CarManagement.Models/"]
COPY ["CarManagement.Solution/CarManagement.Repository/CarManagement.Repository.csproj", "CarManagement.Solution/CarManagement.Repository/"]
COPY ["CarManagement.Solution/CarManagement.Service/CarManagement.Service.csproj", "CarManagement.Solution/CarManagement.Service/"]

RUN dotnet restore "CarManagement.Solution/CarManagement.API/CarManagement.API.csproj"

COPY CarManagement.Solution/ CarManagement.Solution/

WORKDIR /src/CarManagement.Solution/CarManagement.API
RUN dotnet publish "CarManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV Database__FilePath=/app/Database/CarManagement.db

RUN mkdir -p /app/Database

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CarManagement.API.dll"]
