# ცენტრალური SupportToolsServer-ის დეპლოი (D1)

Runbook შედგენილია 2026-10-08-ს, ამოცანა [D1](tasks/D1-deployment.md)-ის ფარგლებში. ყველა ნაბიჯს მომხმარებელი ასრულებს. კოდში ცვლილება არ დასჭირდა (§2).

ტექსტში `<…>` ნიშნავს მნიშვნელობას, რომელსაც თავად ჩაწერ. საიდუმლოებები (პაროლები, ApiKey-ები) არც ამ ფაილში წერია და არც რეპოში.

## 1. გადაწყვეტილებები

| # | საკითხი | გადაწყვეტილება | შედეგი |
|-|-|-|-|
| ა | ჰოსტი | **guria** (Linux, `192.168.10.20`, linux-x64). იქ უკვე მუშაობს `guria.WebAgent`, SQL Server და სხვა სერვისები | სერვისი: `SupportToolsServerProd` (`/var/dotnet/SupportToolsServer/Prod`, ServerInfo `guria/Prod`) |
| ბ | კონფიგურაცია | **ღია კონფიგურაცია ჰოსტზე**. `AddConfigurationEncryption` გათიშული რჩება | საიდუმლოებები systemd-ის `EnvironmentFile`-შია (root, `600`). AppSettingsEncoder/Installer არ გამოიყენება |
| გ | TLS | **TLS არ არის**, მხოლოდ LAN/VPN | პორტი 5033 მხოლოდ LAN/VPN-იდან იხსნება. ინტერნეტში გატანა (port forward) G2-ით აკრძალულია, სანამ TLS არ ჩაირთვება |
| დ | systemd | **WebAgent-ის unit-ი**: `Type=` არ აქვს, ანუ `simple` | `UseSystemd()` საჭირო არ არის. `UseWindowsServiceOnWindows` Linux-ზე არაფერს აკეთებს |
| ე | საწყისი მონაცემები | **PAZISI-ის Dev ბაზის აღდგენა** guria-ზე | ჩანაწერების `Version`-ები იგივე რჩება, ამიტომ ყველა კომპიუტერის `RegistrySyncState` ძალაში რჩება და თავიდან seed არ სჭირდება (§4) |

### რატომ არა დაშიფრული appsettings (ბ)

- `AddConfigurationEncryption` 2026-09-23-ს (commit `431946d`) გაითიშა, `appsetenkeys.json`-თან ერთად.
- გასაღებია `appKey + Environment.MachineName.Capitalize()`. SupportTools-ის AppSettingsEncoder კი შიფრავს გასაღებით `KeyGuidPart + ServerName.Capitalize()`. ანუ მუშაობისთვის პროექტის `KeyGuidPart` უნდა ემთხვეოდეს `Program.cs`-ის `appKey`-ს, ხოლო ServerInfo-ს `ServerName` ჰოსტის სახელს.
- ღია კონფიგურაცია ამ პირობებს არ საჭიროებს. საიდუმლოებები ისედაც ღიად ინახება ბაზაში (G2), ამიტომ ჰოსტზე ფაილის დაშიფვრა დაცვის დონეს მნიშვნელოვნად არ ცვლის.
- ფაილი აუცილებლად ინსტალაციის საქაღალდის გარეთ უნდა იყოს: ProgramInstaller ყოველ ინსტალაციაზე შლის `/var/dotnet/SupportToolsServer/Prod`-ს (`InstallerBase.RunUpdateService`). ამიტომ არც `appsettings.Production.json` გამოდგება.
- unit-ის ფაილს (`/etc/systemd/system/SupportToolsServerProd.service`) ინსტალერი ყოველ ჯერზე თავიდან წერს. მის drop-in საქაღალდეს (`SupportToolsServerProd.service.d/`) კი არ ეხება.
- თუ ოდესმე დაშიფვრა დაგჭირდება: `Program.cs`-ში დააბრუნე `appKey` (= პროექტის `KeyGuidPart`) და `AddConfigurationEncryption`, აღადგინე `appsetenkeys.json` (`git show 431946d^:SupportToolsServer/appsetenkeys.json`) და დაამატე `ApiKeys:AppSettingsByApiKey`. ServerInfo-ში შეავსე `AppSettingsJsonSourceFileName` და `AppSettingsEncodedJsonFileName`. Linux-ზე კოდი ფაილს `Directory.GetCurrentDirectory()`-ში ეძებს, ანუ unit-ის `WorkingDirectory`-ში.

## 2. კოდი და კონფიგურაცია

- **კოდი არ შეცვლილა.**
  - `Program.cs` Linux-ზე ისე მუშაობს, როგორც არის: `ContentRootPath = AppContext.BaseDirectory`, Kestrel `http://*:5033`.
  - linux-x64 self-contained publish (ProgPublisher-ის ბრძანება) შემოწმდა.
  - `UseSystemd` არცერთ აპლიკაციაში არ გამოიყენება.
- **`appsettings.json`** მხოლოდ placeholder-ებს შეიცავს. ჰოსტზე მათ გარემოს ცვლადები ფარავს (`__` = `:`):

| სექცია | ცვლადი | guria-ზე |
|-|-|-|
| `Data:SupportToolsServerDatabase:ConnectionString` | `Data__SupportToolsServerDatabase__ConnectionString` | საიდუმლო (§3.3) |
| `ApiKeys:AppSettingsByApiKey` (A3) | `ApiKeys__AppSettingsByApiKey__<N>__ApiKey`, `…__RemoteIpAddress` | თითო ჩანაწერი თითო კომპიუტერზე. ინდექსი 0 placeholder-ს ფარავს |
| `AppOptions:WorkFolder` | `AppOptions__WorkFolder` | `/home/merab/SupportToolsServerData`. **ინსტალაციის საქაღალდის გარეთ**, თორემ ყოველი დეპლოი ყველა git-ს თავიდან დაკლონავს |
| `AppOptions:GitProjectsRefreshHours` | — | ნაგულისხმევი 24 |
| `Serilog:WriteTo:1:Args:path` | `Serilog__WriteTo__1__Args__path` | `/home/merab/SupportToolsServerData/Logs/SupportToolsServer-.log` (დღიური ფაილები) |
| `Kestrel:Endpoints:Http:Url` | — | `http://*:5033` რჩება |

- `IdentitySettings:JwtSecret` ძველი placeholder-ია: კოდი მას არ კითხულობს. `WebSystemTools.ConfigurationEncrypt`-ის ProjectReference და `Program.cs`-ის კომენტარში მოქცეული ხაზები (b) პუნქტის გამო მკვდარია. შეხება არ მოხდა (D4-ისთვის).

## 3. ნაბიჯები

### 3.1 guria-ს მომზადება

SSH-ით შედი guria-ზე (`ssh merab@192.168.10.20`).

1. **git** (სერვერი GitRepo-ებს თვითონ კლონავს):
   ```bash
   git --version || sudo apt install -y git
   ```
2. **SSH გასაღები `merab`-ისთვის.** სერვისი `merab`-ით ეშვება (`ServiceUserName`), ამიტომ `HOME=/home/merab`. მისამართები `git@github.com:merabza/…` და `ssh://ds920plus/…` ფორმისაა. systemd-ში ტერმინალი არ არის, ამიტომ host key-ის კითხვაზე git ჩავარდება: `known_hosts` წინასწარ უნდა შეივსოს.
   ```bash
   ls ~/.ssh/id_ed25519 || ssh-keygen -t ed25519 -C "merab@guria SupportToolsServer"
   ssh-keyscan github.com >> ~/.ssh/known_hosts
   ssh -T git@github.com
   ```
   - საჯარო გასაღები (`~/.ssh/id_ed25519.pub`) დაამატე GitHub-ზე: ანგარიშის SSH key, ან read-only deploy key თითო რეპოზე.
   - `ssh -T` უნდა დაწეროს „successfully authenticated“.
3. **NAS-ის git-ები** (`ssh://ds920plus/…`, 31 რეპო): `~/.ssh/config`-ში დააკოპირე PAZISI-ის `Host ds920plus` ბლოკი (HostName, User, Port, IdentityFile). NAS-ზე guria-ს საჯარო გასაღები დაამატე და შეამოწმე:
   ```bash
   ssh-keyscan -p <port> <NAS IP> >> ~/.ssh/known_hosts
   ssh ds920plus true && echo ok
   ```
   თუ NAS მიუწვდომელია, ამ git-ების კლონი ჩავარდება (მხოლოდ ლოგში ჩანს) და მათი `GitProjects` სერვერზე არ იქნება (B9).
4. **მონაცემების საქაღალდე:**
   ```bash
   mkdir -p ~/SupportToolsServerData/Logs && chmod 700 ~/SupportToolsServerData
   ```
   - git-ები `~/SupportToolsServerData/Gits`-ში ჩაიწერება. ~80 რეპო, დაახლოებით 2 GB.
   - ადგილი შეამოწმე: `df -h ~`.
5. **firewall:** 5033 მხოლოდ LAN-იდან (და VPN-ის ქსელიდან, თუ ის guria-ზე სხვა მისამართით ჩანს):
   ```bash
   sudo ufw status
   sudo ufw allow from 192.168.10.0/24 to any port 5033 proto tcp
   sudo ufw allow from <VPN subnet> to any port 5033 proto tcp
   ```
   როუტერზე 5033-ის port forward **არ გააკეთო** (გადაწყვეტილება გ).

### 3.2 ბაზა: PAZISI Dev → guria

წყარო PAZISI-ის `SupportToolsServerDevelopment`-ია (`Pazisi` კავშირი, `(local)`). სამიზნე guria-ს SQL Server-ია (`Guria` კავშირი, `192.168.10.20`). ახალი ბაზის სახელი: `SupportToolsServer`.

1. **გაყინვა.** სანამ backup-ს გააკეთებ:
   - სინქრონიზაცია არცერთ კომპიუტერზე არ გაუშვა;
   - PAZISI-ის სერვერი გააჩერე (VS/კონსოლი ან სერვისი), რომ backup-ის შემდეგ ჩანაწერი აღარ შეიცვალოს.
2. **მიგრაციების შემოწმება PAZISI-ზე:**
   ```bash
   sqlcmd -S . -E -C -d SupportToolsServerDevelopment -Q "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId"
   ```
   - `20261006113002_Initial` (და შეიძლება `20261007131749_AddGitRepoProjects`): ყველაფერი რიგზეა. სკრიპტი (ნაბიჯი 6) დანარჩენს თვითონ დაამატებს.
   - სხვა, ძველი `…_Initial` (მაგ. `20261005113410_Initial`, `20260929190002_Initial`): ბაზა ძველი სქემითაა შექმნილი. ID-ის ხელით რეგისტრაცია მხოლოდ მაშინ შეიძლება, თუ ყველა ცხრილი არსებობს: `StoredFiles`, `ServerInfos`, `ProjectGitRepos`, `GlobalSettings`. მაშინ:
     ```bash
     sqlcmd -S . -E -C -d SupportToolsServerDevelopment -Q "IF OBJECT_ID(N'StoredFiles') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = N'20261006113002_Initial') INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES (N'20261006113002_Initial', N'10.0.12')"
     ```
     თუ რომელიმე ცხრილი აკლია, გაჩერდი: ასეთ ბაზაში რეესტრი სრულად არ არის.
3. **backup PAZISI-ზე:**
   ```bash
   sqlcmd -S . -E -C -Q "BACKUP DATABASE [SupportToolsServerDevelopment] TO DISK = N'<D:\Backups>\SupportToolsServer_D1.bak' WITH COPY_ONLY, CHECKSUM, INIT"
   ```
4. **გადატანა guria-ზე.** ფაილი საიდუმლოებებს შეიცავს (G2), ამიტომ საჯარო FTP-ით/exchange storage-ით არ გადაიტანო:
   ```bash
   scp "<D:\Backups>\SupportToolsServer_D1.bak" merab@192.168.10.20:/tmp/
   ```
   შემდეგ guria-ზე:
   ```bash
   sudo mkdir -p /var/opt/mssql/backup && sudo mv /tmp/SupportToolsServer_D1.bak /var/opt/mssql/backup/ && sudo chown mssql:mssql /var/opt/mssql/backup/SupportToolsServer_D1.bak && sudo chmod 600 /var/opt/mssql/backup/SupportToolsServer_D1.bak
   ```
5. **აღდგენა guria-ზე.** `sqlcmd`-ს `-P` არ მისცე: პაროლს თვითონ იკითხავს და ის shell-ის ისტორიაში არ მოხვდება.
   - ჯერ ფაილების ლოგიკური სახელები:
     ```bash
     /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -Q "RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/SupportToolsServer_D1.bak'"
     ```
   - შემდეგ აღდგენა:
     ```bash
     /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -Q "RESTORE DATABASE [SupportToolsServer] FROM DISK = N'/var/opt/mssql/backup/SupportToolsServer_D1.bak' WITH MOVE N'<data logical>' TO N'/var/opt/mssql/data/SupportToolsServer.mdf', MOVE N'<log logical>' TO N'/var/opt/mssql/data/SupportToolsServer_log.ldf', CHECKSUM"
     ```
6. **მიგრაციები** idempotent სკრიპტით: [Deployment-migrations.sql](Deployment-migrations.sql). ის ორივე მიგრაციას (`20261006113002_Initial`, `20261007131749_AddGitRepoProjects`) მხოლოდ მაშინ უშვებს, თუ ისტორიაში ჯერ არ წერია.
   - 2026-10-08-ს ორჯერ ზედიზედ გაეშვა ცარიელ LocalDB-ზე: შეიქმნა 31 ცხრილი, მეორე გაშვებამ არაფერი შეცვალა.
   ```bash
   scp docs/CentralRegistry/Deployment-migrations.sql merab@192.168.10.20:/tmp/
   /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d SupportToolsServer -b -i /tmp/Deployment-migrations.sql
   /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d SupportToolsServer -Q "SELECT MigrationId FROM __EFMigrationsHistory"
   ```
7. **აპლიკაციის login** (მხოლოდ DML; DDL-ს მიგრაციის სკრიპტი ადმინით აკეთებს):
   ```sql
   CREATE LOGIN [sts_app] WITH PASSWORD = N'<ძლიერი პაროლი>', CHECK_POLICY = ON;
   USE [SupportToolsServer];
   CREATE USER [sts_app] FOR LOGIN [sts_app];
   ALTER ROLE db_datareader ADD MEMBER [sts_app];
   ALTER ROLE db_datawriter ADD MEMBER [sts_app];
   ```
   - გაუშვი ინტერაქტიულად: `sqlcmd -S localhost -U sa -C`, შემდეგ ბრძანებები და `GO`.
   - ბოლოს წაშალე PAZISI-დან ჩამოყოლილი Windows მომხმარებლები, თუ არის: `SELECT name FROM sys.database_principals WHERE type IN ('U','G')`, შემდეგ `DROP USER [<name>]`.
8. **სამომავლო მიგრაციები** (ახალი B/D ამოცანების შემდეგ): სკრიპტი თავიდან დააგენერირე DbTools workspace-ში და გაუშვი ProgramUpdater-მდე:
   ```bash
   dotnet ef migrations script --idempotent --project SupportToolsServerDbTools.DbMigration --startup-project SupportToolsServerDbTools.FakeHost --output <გზა>\SupportToolsServer-idempotent.sql
   ```
   - გაშვება: `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, Core/DbPart კლონების pull-ის შემდეგ.
   - **squash-ის შემდეგ** (ახალი `Initial`) guria-ს ბაზაში ძველი ID დარჩება. ახალ `Initial`-ს არ გაუშვებ: ჯერ მის ID-ს დაარეგისტრირებ, როცა სქემა ემთხვევა (როგორც ნაბიჯ 2-ში).

### 3.3 სერვისის კონფიგურაცია (guria, root)

1. **გარემოს ფაილი** `/etc/SupportToolsServer/Prod.env`. ApiKey-ები ლოკალურად დააგენერირე (`openssl rand -hex 32`), თითო კომპიუტერზე თითო. `RemoteIpAddress` არის ის მისამართი, რომლითაც კომპიუტერი guria-ს უკავშირდება; `*` ნიშნავს ნებისმიერს (A3).
   ```bash
   sudo mkdir -p /etc/SupportToolsServer
   sudo nano /etc/SupportToolsServer/Prod.env
   sudo chown root:root /etc/SupportToolsServer/Prod.env && sudo chmod 600 /etc/SupportToolsServer/Prod.env
   ```
   შიგთავსის ნიმუში (ფრჩხილები შეცვალე):
   ```ini
   Data__SupportToolsServerDatabase__ConnectionString="Server=localhost;Database=SupportToolsServer;User Id=sts_app;Password=<პაროლი>;TrustServerCertificate=True"
   ApiKeys__AppSettingsByApiKey__0__ApiKey=<PAZISI-ის გასაღები>
   ApiKeys__AppSettingsByApiKey__0__RemoteIpAddress=<PAZISI-ის LAN IP>
   ApiKeys__AppSettingsByApiKey__1__ApiKey=<მეორე კომპიუტერის გასაღები>
   ApiKeys__AppSettingsByApiKey__1__RemoteIpAddress=<მისი IP ან *>
   AppOptions__WorkFolder=/home/merab/SupportToolsServerData
   Serilog__WriteTo__1__Args__path=/home/merab/SupportToolsServerData/Logs/SupportToolsServer-.log
   ```
   - ფაილს systemd კითხულობს, root-ით, სერვისის გაშვებამდე. `merab`-ს მისი წაკითხვა არ სჭირდება.
   - ორმაგ ბრჭყალებში `\` და `"` სპეციალური სიმბოლოებია. პაროლში ისინი არ გამოიყენო.
2. **drop-in** (ინსტალერი მას არ ეხება და unit-ის თავიდან ჩაწერის შემდეგაც რჩება):
   ```bash
   sudo mkdir -p /etc/systemd/system/SupportToolsServerProd.service.d
   printf '[Service]\nEnvironmentFile=/etc/SupportToolsServer/Prod.env\n' | sudo tee /etc/systemd/system/SupportToolsServerProd.service.d/override.conf
   sudo systemctl daemon-reload
   ```
   ეს ProgramInstaller-მდე გააკეთე, რომ სერვისი პირველივე გაშვებაზე კონფიგურაციით ავიდეს. drop-in-ის შეცვლის შემდეგ: `sudo systemctl daemon-reload && sudo systemctl restart SupportToolsServerProd`.

### 3.4 ServerInfo SupportTools-ში (PAZISI)

`Projects → SupportToolsServer → Server Infos List → Create New Server Info`:

| ველი | მნიშვნელობა |
|-|-|
| Server | `guria` |
| Environment | `Prod` |
| ServerSidePort | `5033` (VersionChecker ამ შემთხვევაში `guria.WebAgent`-ის proxy-ით მიდის) |
| ApiVersionId | `v1` |
| WebAgentNameForCheck | `guria.WebAgent` |
| ServiceUserName | `merab` |
| AppSettingsJsonSourceFileName, AppSettingsEncodedJsonFileName | **ცარიელი**. ProgramInstaller მაშინ პარამეტრების ფაილს არ ეძებს და არ წერს |
| Current/New DatabaseParameters | სურვილისამებრ: `Guria` კავშირი, ბაზა `SupportToolsServer` (backup-ის ხელსაწყოებისთვის, §5.2) |
| AllowToolsList | `ProgPublisher`, `ProgramInstaller`, `ProgramUpdater`, `ServiceStarter`, `ServiceStopper`, `VersionChecker`, `ProgRemover` |

- ძველი `dl360/Prod` ServerInfo აღარ გამოიყენება. წაშლა შენი გადასაწყვეტია.
- ცვლილება რეესტრშია. მისი სერვერზე ასვლა სინქრონიზაციით მოხდება, უკვე guria-ზე (§3.7).

### 3.5 დეპლოი

`Projects → SupportToolsServer → guria/Prod → Project Tools List`:

1. **`ProgramUpdater`** (= `ProgPublisher` + `ProgramInstaller`). ცალ-ცალკეც შეიძლება:
   - `ProgPublisher`: `dotnet publish -c Release -r linux-x64 --self-contained`, შემდეგ zip-ის ატვირთვა exchange storage-ზე;
   - `ProgramInstaller`: `guria.WebAgent` ჩამოტვირთავს, გააჩერებს `SupportToolsServerProd`-ს, თავიდან ჩაწერს `/var/dotnet/SupportToolsServer/Prod`-ს, გააკეთებს `chown merab:merab`-ს, დაწერს unit-ს (`User=merab`, `ASPNETCORE_ENVIRONMENT=Production`) და გაუშვებს.
2. ProgramInstaller ბოლოს თვითონ იძახებს VersionChecker-ს. ცალკეც გაუშვი **`VersionChecker`**: ვერსია build-ისას უნდა ემთხვეოდეს.
3. AppSettingsEncoder, AppSettingsInstaller, AppSettingsUpdater არ გამოიყენება (გადაწყვეტილება ბ).
4. ServiceInstallScriptCreator მხოლოდ სათადარიგოა, WebAgent-ის გარეშე ინსტალაციისთვის. ის იმავე unit-ს წერს. სკრიპტში FTP-ის მომხმარებელი და პაროლი ჩაიწერება (README §9, №15), ამიტომ გაშვების შემდეგ წაშალე.
5. სერვისის მართვა guria-ზე:
   ```bash
   systemctl status SupportToolsServerProd
   journalctl -u SupportToolsServerProd -f
   ```

### 3.6 შემოწმება

PAZISI-დან (PowerShell). გასაღები ბრძანებაში პირდაპირ არ ჩაწერო: `Read-Host` მას ისტორიაში არ ტოვებს.

```powershell
curl.exe -s http://192.168.10.20:5033/api/v1/test/getversion
curl.exe -s -o NUL -w "%{http_code}`n" http://192.168.10.20:5033/api/v1/environments
$k = Read-Host 'ApiKey' -MaskInput
curl.exe -s -o NUL -w "%{http_code}`n" "http://192.168.10.20:5033/api/v1/environments?apikey=$([uri]::EscapeDataString($k))"
Remove-Variable k
```

| შემოწმება | მოსალოდნელი |
|-|-|
| `test/getversion`, ანონიმურად | 200 და ვერსია |
| `environments` გასაღების გარეშე | **401** |
| `environments` სწორი გასაღებით, სწორი მისამართიდან | **200** |
| `environments` სხვა კომპიუტერიდან, რომლის IP ამ გასაღებს არ ემთხვევა | 401 |
| guria-ზე: `ls ~/SupportToolsServerData/Gits \| wc -l` (რამდენიმე წუთის შემდეგ) | რეპოების რაოდენობა (~79). NAS-ისები მხოლოდ მაშინ, თუ 3.1.3 გავიდა |
| guria-ზე: `grep -ri "apikey=" ~/SupportToolsServerData/Logs \| grep -v "apikey=\*\*\*" \| head` | ცარიელი: გასაღები ლოგში მხოლოდ `***`-ითაა |
| `journalctl -u SupportToolsServerProd -p err --since today` | არცერთი შეცდომა: ბაზასთან კავშირი და მიგრაციები რიგზეა |

### 3.7 კლიენტები ყველა კომპიუტერზე

ApiClient, რომლითაც SupportTools სერვერს უკავშირდება, ლოკალურია (bootstrap, G6). თითო კომპიუტერზე:

1. **SupportTools.json-ის backup** (`D:\1WorkSecurity\SupportTools\SupportTools.json` ან ამ კომპიუტერის `--use` ფაილი).
2. ApiClient `guria.SupportToolsServer`: Server `http://192.168.10.20:5033/api/v1/`, ApiKey ამ კომპიუტერის გასაღები (§3.3).
3. `SupportToolsServerWebApiClientName` = `guria.SupportToolsServer`.
4. **PAZISI:** `Sync Registry With SupportToolsServer...`. ბაზა PAZISI-ის Dev-ის ასლია, ამიტომ გეგმა უნდა იყოს „InSync“ ან მხოლოდ ის ცვლილებები, რაც backup-ის შემდეგ ლოკალურად გააკეთე.
   - **თუ გეგმაში ბევრი `Pull(Delete)` ჩანს, არ გაუშვა** (Dry run / უარი). ეს ნიშნავს, რომ guria-ს ბაზა სხვა სერვერის ასლია, ვიდრე ის, რომლის ვერსიებიც ამ კომპიუტერის `RegistrySyncState`-შია.
   - შემდეგ გაუშვი `Update Git Projects From SupportToolsServer` და StoredFiles-ის სინქრონიზაცია (C6).
5. **სხვა კომპიუტერები** (ახალი ან ძველი JSON-ით):
   - ჯერ კომპიუტერის პროფილი (C1: `MachineName`, `CurrentMachineServerName`, `PathMappings`, ფოლდერები), შემდეგ ნაბიჯები 2–3 და ერთი სინქრონიზაცია.
   - `RegistrySyncState` ცარიელი თუ არის, ჩანაწერები ჩამოვა.
   - თუ JSON-ს სხვა სერვერის ვერსიები აქვს, `RegistrySyncState` ჯერ ცარიელ ობიექტად (`{}`) აქციე (backup-ის შემდეგ). განსხვავებული ჩანაწერები `FirstSyncDiffers` კონფლიქტად გამოჩნდება.
6. **VersionChecker** (`SupportToolsServer`, `guria/Prod`) ნებისმიერი კომპიუტერიდან, რომელსაც `guria.WebAgent` მისაწვდომი აქვს.
7. PAZISI-ის Dev სერვერი დეველოპმენტისთვის რჩება, მაგრამ კლიენტები მას აღარ უკავშირდებიან. PAZISI-ზე მისი ApiClient (`PAZISI.SupportToolsServer`) მხოლოდ დეველოპმენტისას აირჩიე, და მხოლოდ ცალკე, ასლის JSON-ით: ერთი JSON ორ სერვერთან რომ არ დასინქრონდეს.

## 4. რატომ აღდგენა და არა seed

- `RegistrySyncState` ინახავს სერვერის `Version`-ს თითო ჩანაწერზე, მაგრამ არ ინახავს, რომელ სერვერს ეკუთვნის.
- თუ ის ცარიელ სერვერს შეხვდება, დამგეგმავი (`RegistrySyncPlanner`) ყოველ ჩანაწერს „სერვერზე წაშლილად“ ჩათვლის. უცვლელი ჩანაწერისთვის ეს `Pull(Delete)`-ია, ანუ ლოკალური წაშლა.
- აღდგენა ვერსიებს უცვლელად ინარჩუნებს. ალტერნატივა იყო ცარიელი ბაზა, ყველა კომპიუტერზე `RegistrySyncState`-ის გასუფთავება და PAZISI-დან ხელახალი seed. ის საჭირო გახდება, თუ PAZISI-ის Dev ბაზა რეესტრს სრულად არ შეიცავს.

## 5. უსაფრთხოება, backup, ლოგები

### 5.1 ქსელი

- HTTP, TLS-ის გარეშე. ApiKey query-ში იგზავნება, ამიტომ ქსელში ღიად მიდის. ეს მისაღებია მხოლოდ LAN/VPN-ში (გადაწყვეტილება გ).
- 5033 ufw-ით მხოლოდ LAN/VPN ქსელებისთვისაა ღია (§3.1.5). port forward არ არის.
- თუ სერვერი ოდესმე ინტერნეტიდან უნდა იყოს მისაწვდომი, საჭიროა nginx reverse proxy სერტიფიკატით (ფორმა, როგორც `bagetter.merab.pvt.ge`). Kestrel მაშინ `http://127.0.0.1:5033`-ზე გადავა, ApiKey-ის მისამართების წესი კი forwarded headers-ს მოითხოვს (`UseForwardedHeaders`; დღეს არ არის, კოდის ცვლილებაა).
- SQL Server-ის 1433 ღია რჩება მხოლოდ იმ ქსელებისთვის, საიდანაც დღესაც გჭირდება. `sts_app`-ს DDL-ის უფლება არ აქვს.

### 5.2 Backup

ბაზაში საიდუმლოებები ღიადაა (G2: ApiKey-ები, პაროლები, `KeyGuidPart`, StoredFiles), ამიტომ **backup-ი საიდუმლოა**.

- დღიური სრული backup guria-ზე. ორივე ვარიანტი იმავე წესებს იცავს:
  - SupportTools-ის ბაზის ხელსაწყოებით (`Guria` კავშირი, SmartSchema, ServerInfo-ს `CurrentDatabaseParameters`);
  - ან cron-ით:
    ```bash
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U <backup login> -C -Q "BACKUP DATABASE [SupportToolsServer] TO DISK = N'/var/opt/mssql/backup/SupportToolsServer_$(date +%F).bak' WITH CHECKSUM, INIT"
    ```
    cron-ში პაროლი `~/.sqlcmd`-ის ცვლადში ან `SQLCMDPASSWORD`-ში, root/`600` ფაილიდან.
- ფაილები: `mssql:mssql`, `600`. ასლი მხოლოდ დაცულ ადგილას (NAS-ის პირადი share). **არა** საერთო FTP exchange storage-ზე, რომლითაც ProgPublisher პაკეტებს ცვლის.
- სურვილისამებრ `WITH ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = …)`. მაშინ სერტიფიკატისა და მისი გასაღების backup ცალკე, სხვა ადგილას შეინახე.
- `/etc/SupportToolsServer/Prod.env` საიდუმლოა: მისი ასლი პაროლების მენეჯერში და არა რეპოში.
- **შემოწმება:** ერთხელ აღადგინე backup სატესტო სახელით (`RESTORE … WITH MOVE`) და წაშალე.
- `/tmp`-ში და PAZISI-ზე დარჩენილი `SupportToolsServer_D1.bak` გადატანის შემდეგ წაშალე.

### 5.3 ლოგები

| ადგილი | შიგთავსი |
|-|-|
| `/home/merab/SupportToolsServerData/Logs/SupportToolsServer-<თარიღი>.log` (Serilog, დღიური) | მოთხოვნები, git-ის მუშაობა, შეცდომები |
| `journalctl -u SupportToolsServerProd` | კონსოლის იგივე ლოგი |

საიდუმლოებები ლოგში არ იწერება:
- `ApiKeyRedactionEnricher` query-ის `apikey`-ს `***`-ით ცვლის (A3, `Host/RequestLogTests`);
- უარყოფილი გასაღებიდან მხოლოდ მისამართი და სიგრძე იწერება;
- ვალიდაციის და შეცდომის ტექსტები ველს ასახელებს და არა მნიშვნელობას;
- StoredFile-ის შიგთავსი ლოგში არ ხვდება (B8).

ლოგის საქაღალდე `700`-ია (§3.1.4). ძველი ფაილები ხელით ან cron-ით წაშალე (`find … -mtime +90 -delete`), რადგან `retainedFileCountLimit` კონფიგურაციაში არ წერია (Serilog-ის ნაგულისხმევი 31 ფაილია).

## 6. შეჯამება: ნაბიჯების რიგი

1. 3.1: git, SSH (GitHub + NAS), საქაღალდეები, ufw.
2. 3.2: გაყინვა → მიგრაციების შემოწმება → backup → scp → restore → სკრიპტი → `sts_app`.
3. 3.3: `Prod.env` და drop-in.
4. 3.4: ServerInfo `guria/Prod`.
5. 3.5: ProgramUpdater → VersionChecker.
6. 3.6: 200 / 401 / 200, git-ები, ლოგი.
7. 3.7: PAZISI-ის ApiClient და სინქრონიზაცია, შემდეგ დანარჩენი კომპიუტერები.
8. 5.2: backup-ის გეგმა.

ყველაფრის დასრულების შემდეგ README §5-ში D1 მონიშნე ✅.
