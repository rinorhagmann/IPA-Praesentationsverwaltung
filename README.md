# Präsentationsverwaltung – Wirtschaftsgymnasium Basel

Web-Applikation zur Verwaltung der Zusehenden an Maturaarbeitspräsentationen.
G3-Schüler/innen melden sich für zwei G4-Präsentationen an; die Administration
importiert die Daten, verwaltet sie und exportiert die Raum- und Zuseherlisten.

## Technologie

- **ASP.NET Core 10 (MVC)** in C# nach dem MVC-Muster
- **Entity Framework Core** mit **Microsoft SQL Server**
- **Bootstrap** für das responsive Frontend
- **Docker / Docker Compose** für Web-App und Datenbank
- Authentifizierung über Cookies mit rollenbasierter Autorisierung
  (Passwörter als PBKDF2-HMAC-SHA256 gehasht, Brute-Force-Schutz)

Der Code und die Kommentare sind auf Englisch, die Benutzeroberfläche auf
Deutsch.

## Schnellstart mit Docker

Voraussetzung: Docker Desktop läuft.

```bash
docker compose up --build
```

Anschliessend ist die Anwendung erreichbar unter:

- http://localhost:8080
- https://localhost:8081 (benötigt das ASP.NET-Core-Entwicklungszertifikat)

Die Datenbank (`sql-db`) wird automatisch gestartet; die Web-App wartet per
Healthcheck, wendet die EF-Core-Migrationen an und legt den Standard-Admin an.

### Standard-Anmeldung (Administration)

| Feld     | Wert            |
| -------- | --------------- |
| E-Mail   | `admin@wgbs.ch` |
| Passwort | `Admin123!`     |

> Das Passwort sollte nach dem ersten Login geändert werden. Die Zugangsdaten
> sind über `appsettings.json` (Abschnitt `DefaultAdmin`) konfigurierbar.

## Lokale Entwicklung (ohne Docker)

Eine erreichbare MSSQL-Instanz wird benötigt (Verbindungszeichenfolge in
`appsettings.json`). Danach:

```bash
dotnet run --project IPA-Praesentationsverwaltung
```

## Tests

```bash
dotnet test
```

## Projektstruktur

```
IPA-Praesentationsverwaltung/          ASP.NET-Core-MVC-Projekt
  Controllers/                         MVC-Controller
  Data/                                DbContext, Migrationen, Initialisierung
  Models/Domain                        Domänenmodell (User, Student, Admin, ...)
  Models/ViewModels, Models/Dtos       ViewModels und DTOs
  Services/                            Geschäftslogik (Services + Abstraktionen)
  Services/Infrastructure/             PBKDF2-Hashing, CSV-Parser, PDF-Writer, ...
  Views/                               Razor-Views (deutsche Oberfläche)
IPA-Praesentationsverwaltung.Tests/    xUnit-Unit-Tests
db-scripts/                            SQL-Skripte und Beispiel-CSV-Dateien
```

## Importformat (CSV)

- **G4-Präsentationen:** `Thema; Datum/Uhrzeit; Raum` (Semikolon oder Komma)
- **G3-Schüler/innen:** `Vorname; Nachname; E-Mail`

Beispieldateien liegen in `db-scripts/sample-data/`.

## Fachregeln

- Eine Präsentation hat maximal **6** Zuseher.
- Eine Schülerin / ein Schüler wählt genau **2** Präsentationen.
- Keine Anmeldung zu zwei **gleichzeitig** stattfindenden Präsentationen.
