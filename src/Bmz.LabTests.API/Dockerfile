FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Bmz.LabTests.API/Bmz.LabTests.API.csproj Bmz.LabTests.API/
COPY src/Bmz.LabTests.Application/Bmz.LabTests.Application.csproj Bmz.LabTests.Application/
COPY src/Bmz.LabTests.Domain/Bmz.LabTests.Domain.csproj Bmz.LabTests.Domain/
COPY src/Bmz.LabTests.Infrastructure/Bmz.LabTests.Infrastructure.csproj Bmz.LabTests.Infrastructure/

RUN dotnet restore Bmz.LabTests.API/Bmz.LabTests.API.csproj

COPY src/ ./

WORKDIR /src/Bmz.LabTests.API
RUN dotnet build -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS publish
WORKDIR /app

COPY --from=build /app/build .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Bmz.LabTests.API.dll"]