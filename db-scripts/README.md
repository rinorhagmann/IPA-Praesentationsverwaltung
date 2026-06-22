# Datenbank-Skripte

Dieser Ordner enthält die SQL-Skripte zur manuellen Bereitstellung der
MSSQL-Datenbank sowie Beispiel-Importdateien.

## Reihenfolge

| Skript | Zweck |
| ------ | ----- |
| `00_create_database.sql` | Legt die Datenbank `IPADatabase` an. |
| `01_create_schema.sql`   | Erstellt das Schema (Tabellen, Schlüssel, Indizes). Idempotent – kann mehrfach ausgeführt werden. |
| `02_seed_admin.sql`      | Legt den Standard-Administrator an (`admin@wgbs.ch` / `Admin123!`). |

Das Skript `01_create_schema.sql` wird aus den Entity-Framework-Core-Migrationen
erzeugt (`dotnet ef migrations script --idempotent`) und entspricht damit exakt
dem Datenmodell der Anwendung.

## Hinweis zur automatischen Einrichtung

Beim Start wendet die Anwendung die Migrationen selbst an und legt den
Standard-Administrator an (siehe `Data/DbInitializer.cs`). Beim Betrieb über
`docker compose up` ist daher kein manuelles Ausführen der Skripte nötig – sie
dienen der Dokumentation und der manuellen Provisionierung (z. B. in SSMS).

## Ausführen mit sqlcmd (Beispiel)

```bash
sqlcmd -S localhost,1433 -U sa -P "MeinSicheresPasswort123!" -C -i 00_create_database.sql
sqlcmd -S localhost,1433 -U sa -P "MeinSicheresPasswort123!" -C -d IPADatabase -i 01_create_schema.sql
sqlcmd -S localhost,1433 -U sa -P "MeinSicheresPasswort123!" -C -d IPADatabase -i 02_seed_admin.sql
```

## Beispiel-Importdateien

Im Unterordner `sample-data/` liegen Beispiel-CSV-Dateien für den Import über
den Administrationsbereich:

- `presentations.csv` – G4-Präsentationen (Thema; Datum/Uhrzeit; Raum)
- `students.csv` – G3-Schüler/innen (Vorname; Nachname; E-Mail)
