FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Usemam.Ledger.API/Usemam.Ledger.API.fsproj
RUN dotnet publish src/Usemam.Ledger.API/Usemam.Ledger.API.fsproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Usemam.Ledger.API.dll"]
