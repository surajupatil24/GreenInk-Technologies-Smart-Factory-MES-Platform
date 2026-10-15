FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/GreenInk.Api.csproj apps/api/
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then SSL_CERT_FILE=/run/secrets/proxy_ca dotnet restore apps/api/GreenInk.Api.csproj; else dotnet restore apps/api/GreenInk.Api.csproj; fi
COPY apps/api/ apps/api/
RUN dotnet publish apps/api/GreenInk.Api.csproj -c Release -o /out --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "GreenInk.Api.dll"]
