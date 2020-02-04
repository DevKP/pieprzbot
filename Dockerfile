FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /
COPY PerchikSharp/PerchikSharp.csproj .
COPY . .

RUN dotnet publish -c Release -o /bot

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /bot
COPY /PerchikSharp/Resources/ ./
COPY /PerchikSharp/Configs/ ./Configs/
COPY --from=build /bot .
ENTRYPOINT ["dotnet", "PerchikSharp.dll"]