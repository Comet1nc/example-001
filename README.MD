.NET 8.0 SDK  
Firebird 5

<b>Uruchamianie</b>

1. Tworzenie nowej bazy
dotnet run -- build-db --db-dir "<ścieżka>" --scripts-dir "<ścieżka>"

2. Eksport struktury z istniejącej bazy
dotnet run -- export-scripts --connection-string "<connStr>" --output-dir "<ścieżka>"

3. Aktualizacja istniejącej bazy
dotnet run -- update-db --connection-string "<connStr>" --scripts-dir "<ścieżka>"


Aplikacja obsługuje następujące typy DDL:
Domeny,
Tabele,
Procedury

Do tworzenia nowej bazy użyte są domyślne dane logowania:
UserID = "SYSDBA",
Password = "masterkey",
