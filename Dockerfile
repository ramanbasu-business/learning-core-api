FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/LearningUserService/LearningUserService.csproj src/LearningUserService/
RUN dotnet restore src/LearningUserService/LearningUserService.csproj

COPY src/LearningUserService/ src/LearningUserService/
RUN dotnet publish src/LearningUserService/LearningUserService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5002
ENTRYPOINT ["dotnet", "LearningUserService.dll"]
