# tạo services
# Console service (Submit/Accept/Complete)
dotnet new console -f net10.0 -o OrderSubmitService    -n OrderSubmitService
dotnet new console -f net10.0 -o OrderAcceptService    -n OrderAcceptService
dotnet new console -f net10.0 -o OrderCompleteService  -n OrderCompleteService

dotnet new web -f net10.0 -o NotificationService -n NotificationService

# Thêm vô solution
dotnet sln Onion.CleanArchitecture.sln add OrderSubmitService/OrderSubmitService.csproj
dotnet sln Onion.CleanArchitecture.sln add OrderAcceptService/OrderAcceptService.csproj
dotnet sln Onion.CleanArchitecture.sln add OrderCompleteService/OrderCompleteService.csproj
dotnet sln Onion.CleanArchitecture.sln add NotificationService/NotificationService.csproj

# add reference mẫu 
dotnet add OrderSubmitService\OrderSubmitService.csproj reference Onion.CleanArchitecture\Onion.CleanArchitecture.Application\Onion.CleanArchitecture.Application.csproj 

Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Persistence\Onion.CleanArchitecture.Infrastructure.Persistence.csproj 

Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Shared\Onion.CleanArchitecture.Infrastructure.Shared.csproj

# add transport Sql
dotnet add package MassTransit.SqlTransport.PostgreSQL --version 8.5.7

# Onion Architecture In ASP.NET Core With CQRS

https://craftbakery.dev/make-your-own-custom-netcore-template/

# Install template

dotnet new -i Onion.CleanArchitecture.Template\

# Uninstall template

dotnet new -u Onion.CleanArchitecture.Template\

# Create a project using the template

dotnet new onion-clean -n Ecommerce -au "ToanLe" -d "The ecommerce project for business" -y 2024

# Run docker sql server as command below

docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=sql@pa22w0rd" -p 1433:1433 -d --name=sqlserver mcr.microsoft.com/mssql/server:2022-preview-ubuntu-22.04

# Create policy.csv file in Onion.CleanArchitecture/Onion.CleanArchitecture.WebApp.Server/wwwroot/policy.csv

Add default row as below
p, SuperAdmin, users, list
p, SuperAdmin, users, create
p, SuperAdmin, roleclaims, list
p, SuperAdmin, roleclaims, create
p, SuperAdmin, roleclaims, show
p, SuperAdmin, roleclaims, edit
p, SuperAdmin, roleclaims, delete

# Run dotnet run in Onion.CleanArchitecture.WebApp.Server

# Access https://localhost:5173/ on browser

# Login with username:superadmin@gmail.com and password:123Pa$$word!
