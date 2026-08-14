# Supplier Portal
 
Databasdriven webbapplikation för hantering och delning av leverantörsdata för MEDS Apotek.
 
*En publik demo finns tillgänglig på [supplierportal.fridajansson.com](https://supplierportal.fridajansson.com). Demo-data återställs automatiskt varje natt.*
 
**Demo-inloggning (MEDS-admin):**
- E-post: `demo-admin@example.com`
- Lösenord: `DemoPass123!`
**Demo-inloggning (leverantör):**
- E-post: `demo-supplier@example.com`
- Lösenord: `DemoPass123!`

## Installation
 
För att installera och köra lokalt:
 
    git clone https://github.com/frja2400/supplier-portal.git
    cd supplier-portal/SupplierPortal
    dotnet restore
 
Sätt upp en lokal connection string via .NET User Secrets:
 
    dotnet user-secrets init
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=supplierportal.db"
 
Skapa databasen genom att köra migrationerna:
 
    dotnet ef database update
 
Detta skapar en lokal SQLite-databasfil (`supplierportal.db`) med samtliga tabeller. Rollerna `MedsEmployee` och `Supplier` seedas automatiskt vid applikationens första uppstart.
 
Starta utvecklingsservern:
 
    dotnet run
 
## Funktioner
 
Applikationen har två roller: **MEDS-medarbetare** (administratör) och **leverantör**, samt en publik, oinloggad vy.
 
**Publikt**
Startsida med aggregerad statistik (antal registrerade aktiveringar och leverantörer), synlig utan inloggning.
 
**MEDS-medarbetare**
Fullständig hantering av leverantörer (skapa, redigera, ta bort, tilldela ansvarig kontaktperson, registrera länk till extern rapportering) och aktiveringar (skapa, redigera, ta bort, sök, filtrera och sortera). Import av data från xlsx- eller csv-filer via drag and drop, med automatisk matchning av leverantörer och en förhandsgranskning innan data sparas. Export av filtrerad data till xlsx. Skapande av leverantörskonton kopplade till en specifik leverantör, med slumpmässigt genererat lösenord.
 
**Leverantör**
Egen, avgränsad vy där enbart den egna datan visas (filtrerbar på period, sorterbar, exporterbar till xlsx). Visar ansvarig kontaktperson på MEDS, samt länk till extern Looker Studio-rapport om en sådan är registrerad.
 
## Projektstruktur
 
    Controllers/
    ├── HomeController.cs         # Publik startsida
    ├── AccountController.cs      # Registrering, inloggning, utloggning
    ├── ActivationsController.cs  # CRUD, sök/filter/sortering, export (admin)
    ├── SuppliersController.cs    # CRUD leverantörer, leverantörskonton (admin)
    ├── MyPortalController.cs     # Leverantörens egen, avgränsade vy
    ├── ImportController.cs       # Uppladdning, förhandsgranskning, import
    └── DemoController.cs         # Återställning av demo-data (publik demo)
 
    Services/
    └── ActivationImportService.cs  # Parser för xlsx/csv till gemensam mellanmodell
 
    Data/
    ├── ApplicationDbContext.cs   # EF Core-kontext, Fluent API-relationer
    └── SeedData.cs                # Rollseedning + demo-dataseedning
 
    Models/
    ├── Supplier.cs
    ├── Activation.cs
    ├── ApplicationUser.cs        # Utökad Identity-användare
    ├── ImportedActivationRow.cs  # Mellanmodell för import
    └── ViewModels/                # Formulärspecifika ViewModels
 
    Views/
    ├── Home/, Account/, Activations/, Suppliers/, MyPortal/, Import/
    └── Shared/_Layout.cshtml     # Rollbaserad sidopanel
 
## Tech stack
 
| | |
|---|---|
| **Backend** | ASP.NET Core MVC, C#, .NET 9 |
| **Databas** | Entity Framework Core, SQLite |
| **Autentisering** | ASP.NET Core Identity, rollbaserad åtkomstkontroll |
| **Excel/CSV-hantering** | ClosedXML, CsvHelper |
| **Frontend** | Bootstrap, Bootstrap Icons, vanilla JavaScript |
| **Deployment** | DigitalOcean VPS, Nginx, systemd, Certbot |
| **CI/CD** | GitHub Actions |
 
## Tekniska lösningar
 
### Dubbla relationer mellan samma två entiteter
 
`ApplicationUser` och `Supplier` har två separata relationer: en leverantör kan ha ett inloggningskonto kopplat till sig ("has login"), och en MEDS-medarbetare kan vara ansvarig kontaktperson för flera leverantörer ("manages"). EF Core kan inte självständigt avgöra vilken foreign key som hör till vilken koppling, så relationerna konfigureras explicit via Fluent API:
 
    builder.Entity<ApplicationUser>()
        .HasOne(u => u.Supplier)
        .WithMany()
        .HasForeignKey(u => u.SupplierId)
        .OnDelete(DeleteBehavior.SetNull);
 
    builder.Entity<Supplier>()
        .HasOne(s => s.AccountManager)
        .WithMany(u => u.ManagedSuppliers)
        .HasForeignKey(s => s.AccountManagerId)
        .OnDelete(DeleteBehavior.SetNull);
 
### Formatoberoende import
 
Både xlsx- och csv-filer mappas till samma mellanmodell (`ImportedActivationRow`) innan validering och leverantörsmatchning, oavsett vilket format som laddats upp:
 
    var worksheet = workbook.Worksheets.Contains("Sponsrade produkter")
        ? workbook.Worksheet("Sponsrade produkter")
        : workbook.Worksheet(1);
 
Detta gör att resten av importflödet (validering, leverantörsmatchning, förhandsgranskning) fungerar identiskt oavsett filformat.
 
### Domänbegränsad registrering
 
MEDS-medarbetare kan självregistrera, men enbart med en e-postadress inom en specifik domän. Leverantörskonton kan inte självregistreras alls, utan skapas av en administratör och kopplas till en befintlig leverantör.
 
    const string allowedDomain = "@meds.se";
    if (!model.Email.EndsWith(allowedDomain, StringComparison.OrdinalIgnoreCase))
    {
        ModelState.AddModelError(nameof(model.Email), $"Only email addresses ending in {allowedDomain} can register an account.");
    }
 
### Delad filtreringslogik för listvy och export
 
Filtreringen (leverantör, period) bröts ut till en delad hjälpmetod, `BuildFilteredQuery`, återanvänd av både listvyn och exportfunktionen.
 
### Automatisk återställning av demo-data
 
Den publika demon återställs automatiskt varje natt via ett schemalagt GitHub Actions-workflow, som anropar en skyddad endpoint på servern:
 
    on:
      schedule:
        - cron: '0 3 * * *'
      workflow_dispatch: {}
 
Endpointen skyddas med en hemlig nyckel (skild från vanlig autentisering) och kan även triggas manuellt.
 
## Publicering
 
Applikationen körs på en VPS med Nginx som reverse proxy och HTTPS via Certbot. GitHub Actions bygger och distribuerar automatiskt vid push till `main`, och en systemd-tjänst håller applikationen igång med automatisk omstart vid krasch.
 
## Om projektet
 
Projektarbete inom Webbutveckling (5,5 hp) vid Mittuniversitetet, VT 2026, utvecklat i samarbete med MEDS Apotek.
 
**Författare:** Frida Jansson