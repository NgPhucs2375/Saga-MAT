dotnet ef migrations remove --context ApplicationDbContext --project Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Persistence --startup-project Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server
dotnet ef migrations remove --context IdentityContext --project Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Identity --startup-project Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server
dotnet ef migrations add InitialCreate --context ApplicationDbContext --project Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Persistence --startup-project Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server
dotnet ef migrations add InitialCreate --context IdentityContext --project Onion.CleanArchitecture\Onion.CleanArchitecture.Infrastructure.Identity --startup-project Onion.CleanArchitecture\Onion.CleanArchitecture.WebApp.Server


dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context IdentityContext

dotnet ef dbcontext list
dotnet ef migrations add InitialCreate --context ApplicationDbContext
dotnet ef migrations add InitialCreate --context IdentityContext

docker build -f Onion.CleanArchitecture/Onion.CleanArchitecture.WebApp.Server/Dockerfile -t onion.clean:v.0.1 .

docker network create -d bridge ecommerce
docker network connect ecommerce sqlserver

git config core.ignorecase true
git config --global core.ignorecase true

p, SuperAdmin, roleclaims, list
p, SuperAdmin, roleclaims, create
p, SuperAdmin, roleclaims, show
p, SuperAdmin, roleclaims, edit
p, SuperAdmin, roleclaims, delete

docker run -e POSTGRES_PASSWORD=2375 -p 5432:5432 -d --name=postgres postgres:16

git rm --cached Onion.CleanArchitecture/Onion.CleanArchitecture.WebApp.Client/Onion.CleanArchitecture.WebApp.Client.esproj
git rm --cached ./Onion.CleanArchitecture/Onion.CleanArchitecture.WebApp.Client/Onion.CleanArchitecture.WebApp.Client.esproj
