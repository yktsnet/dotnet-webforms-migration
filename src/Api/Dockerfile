FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/Api/*.csproj src/Api/
RUN dotnet restore src/Api/AttendanceApi.csproj
COPY src/Api/ src/Api/
RUN dotnet publish src/Api/AttendanceApi.csproj \
    -c Release -r linux-x64 --self-contained true -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["./AttendanceApi"]
