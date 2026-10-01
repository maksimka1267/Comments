FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# сначала только файлы проектов: слой с restore кэшируется, пока зависимости не менялись
COPY src/Comments.Domain/Comments.Domain.csproj src/Comments.Domain/
COPY src/Comments.Infrastructure/Comments.Infrastructure.csproj src/Comments.Infrastructure/
COPY src/Comments.Api/Comments.Api.csproj src/Comments.Api/
RUN dotnet restore src/Comments.Api/Comments.Api.csproj

COPY src/ src/
RUN dotnet publish src/Comments.Api/Comments.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Comments.Api.dll"]