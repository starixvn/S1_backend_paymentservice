FROM hub.lumigame.com/base/dotnet/core/sdk:6.0-alpine AS builder

COPY . .
WORKDIR PaymentService/PaymentService
RUN dotnet publish -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/aspnet:6.0-alpine
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "PaymentService.dll"]