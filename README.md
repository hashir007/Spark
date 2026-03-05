# Spark

Spark is a .NET 8 based solution that encompasses a robust Web API, a background worker for message queue processing, and integration with various third-party services like PayPal, Authorize.Net, MongoDB, and RabbitMQ.

## Architecture & Projects

The solution consists of four main projects:

### 1. SparkApp (`Spark`)
The core ASP.NET Core Web API project. It exposes external API endpoints and relies on modern features such as:
- **API Versioning** using `Asp.Versioning.Http` & `Asp.Versioning.Mvc`
- **Authentication & Authorization** utilizing JWT Bearers
- **API Documentation** via `Swashbuckle.AspNetCore` (Swagger)

### 2. SparkService
A class library containing the core business logic, application services, and data access layers. Key features include:
- **Database** integration with MongoDB and Entity Framework Core
- **Email Services** using `MailKit`
- **Payments** utilizing the `PayPal` SDK

### 3. SparkMQService
A background worker (console application) built with `Microsoft.Extensions.Hosting` designed to handle message queues.
- Consumes messages using **RabbitMQ** via `RabbitMQ.Client.Core.DependencyInjection`
- Executes background tasks asynchronously, detached from the main web application workload.

### 4. AuthorizeNET
A specialized .NET 8 class library interacting with the Authorize.Net payment gateway APIs. It encapsulates payment processing functionalities to provide a clean interface for the rest of the application.

## Technologies Used
- **Framework**: .NET 8
- **Databases**: MongoDB, Entity Framework Core (SQL)
- **Message Broker**: RabbitMQ
- **Payment Gateways**: PayPal, Authorize.Net
- **Security**: JWT Authentication
- **Other**: MailKit (Email)

## Getting Started

1. Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
2. Clone the repository and navigate to the root directory `e:\Github\Spark`.
3. Configure your connection strings and external service credentials in `SparkService/appsettings.json` (which is linked to both `SparkApp` and `SparkMQService`).
4. Build the solution:
   ```bash
   dotnet build Spark.sln
   ```
5. Run the main Web API:
   ```bash
   dotnet run --project SparkApp/Spark.csproj
   ```
6. (Optional) Run the MQ background service in a separate terminal:
   ```bash
   dotnet run --project SparkMQService/SparkMQService.csproj
   ```