ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder as deps

RUN mkdir -p /src/packages

FROM hub.nexdev.net/base/dotnet/core/sdk:6.0 AS builder

ENV NUGET_PACKAGES=/src/packages

COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

COPY . .

WORKDIR PaymentService/PaymentService

RUN dotnet restore -v n -r linux-x64 ./PaymentService.csproj

RUN dotnet publish -c Release -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:6.0-alpine-s1

WORKDIR /app

COPY --from=builder /app .

EXPOSE 5000

ENTRYPOINT ["dotnet", "PaymentService.dll"]
