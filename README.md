# VergilBot 
#### ![Technologies used](https://skillicons.dev/icons?i=dotnet,cs,discord,bots,ai,rider,postgres)
_Preview of a larger project_. (Does not contain latest updates)

_Feature tests Bot_

### Migration Commands
To create the tables of the application in the database, make sure you have configured the
connection string to your database in your appsettings.json. Then run the commands:
<br>
<br>
```
dotnet ef migrations add "initial" --startup-project Vergil.Api --project Vergil.Data
```
```
dotnet ef database update --project Vergil.Api
```

### appsettings.json format (Api)
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1; Port=5432; Database=yourdatabase; Uid=admin; Pwd=admin;"
  }
}
```

### appsettings.json format (Main)
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1; Port=5432; Database=yourdatabase; Uid=admin; Pwd=admin;"
  },
  "DISCORD_TOKEN": "your discord bot api token",
  "OPENAI_TOKEN": "your api token"
}
```
