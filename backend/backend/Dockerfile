FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY SqlTrainer.Api/SqlTrainer.Api.csproj SqlTrainer.Api/
RUN dotnet restore SqlTrainer.Api/SqlTrainer.Api.csproj

COPY . .
RUN dotnet publish SqlTrainer.Api/SqlTrainer.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SqlTrainer.Api.dll"]