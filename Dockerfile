FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Evostel.ManagedServices.Web.csproj", "."]
RUN dotnet restore "Evostel.ManagedServices.Web.csproj"
COPY . .
RUN dotnet publish "Evostel.ManagedServices.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Evostel.ManagedServices.Web.dll"]
