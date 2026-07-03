# Build React frontend
FROM node:20 AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json* ./
COPY frontend/tsconfig.json frontend/vite.config.ts ./
COPY frontend/ ./
RUN npm install
RUN npm run build

# Build .NET backend and copy built frontend into wwwroot
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
COPY --from=frontend-build /src/frontend/dist ./wwwroot
RUN dotnet publish ErpCosmetiqueApi.csproj -c Release -o /app/publish /p:SkipSpaBuild=true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 80
ENV ASPNETCORE_URLS=http://0.0.0.0:80
ENTRYPOINT ["dotnet", "ErpCosmetiqueApi.dll"]
