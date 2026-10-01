# D1 — ცენტრალური სერვერის დეპლოი

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportToolsServer\WebSystemTools`, `D:\1WorkDotnet\SupportTools\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer (კოდი/კონფიგურაცია/runbook); სხვა საჭიროებისამებრ |
| დამოკიდებულია | A3; მეორე კომპიუტერიდან გამოყენებამდე უნდა დასრულდეს |
| ზომა | M + მომხმარებლის ოპერაციული ნაბიჯები |

## პრომპტი

````text
ეს არის ამოცანა D1 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G2/G4, §4.5, §7);
- რეპოს CLAUDE.md;
- D:\1WorkDotnet\SupportTools\SupportTools\docs\en\use-cases\deployment.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი:
- ცენტრალური სერვერი ყველა კომპიუტერიდან მისაწვდომი უნდა იყოს და საიდუმლოებებს ღიად ინახავს (G2).
- ჰოსტი ჯერ არ არის გადაწყვეტილი (G4). SupportTools.json-ში პროექტ SupportToolsServer-ს უკვე აქვს ServerInfo-ები: dl360/Prod (linux-x64, WebAgent) და PAZISI/Dev (Windows, IsLocal). ApiClient-ია `PAZISI.SupportToolsServer`.
- სერვერი ახლა ღია HTTP-ზე უსმენს: `http://*:5033`.
- `Program.cs`-ში `AddConfigurationEncryption` კომენტარშია. ამიტომ SupportTools-ის AppSettingsEncoder-ის დაშიფრულ appsettings-ს build ვერ წაიკითხავს.
- სერვერი GitRepo-ებს თვითონ კლონავს. მისამართები SSH ფორმისაა (`git@github.com:merabza/…`), ამიტომ ჰოსტს სჭირდება git და SSH გასაღები, ან HTTPS მისამართები.

ნაბიჯი 0. მომხმარებელთან ერთად გადაწყვიტე (AskUserQuestion):
- (ა) ჰოსტი: PAZISI (LAN/VPN) თუ dl360 (ინტერნეტიდან) თუ სხვა;
- (ბ) კონფიგურაციის მიწოდება: SupportTools-ის AppSettingsEncoder-ის ნაკადი, რაც `AddConfigurationEncryption`-ის აღდგენას ნიშნავს, თუ ღია appsettings / გარემოს ცვლადები ჰოსტზე;
- (გ) TLS, თუ ჰოსტი LAN-ის გარეთაა: reverse proxy (nginx + სერტიფიკატი) თუ Kestrel-ის HTTPS;
- (დ) Linux-ზე სერვისად გაშვება: `UseWindowsServiceOnWindows` Linux-ზე უვნებელია? საჭიროა `UseSystemd()` (`Microsoft.Extensions.Hosting.Systemd`)? როგორ უშვებს WebAgent სხვა სერვისებს Linux-ზე?

სამუშაო:

1. კოდი და კონფიგურაცია (ნაბიჯი 0-ის მიხედვით):
   - `AddConfigurationEncryption`-ის აღდგენა ან მისი არსებობის მიზეზის აღწერა;
   - systemd-ის ინტეგრაცია, თუ საჭიროა;
   - `appsettings.json`-ში მხოლოდ placeholder-ები.
   - საჭირო სექციები: `Data:SupportToolsServerDatabase:ConnectionString`, `AppOptions:WorkFolder`, `ApiKeys:AppSettingsByApiKey` (A3-ის მოდელით), `Kestrel`, `Serilog`.

2. ბაზა:
   - მომხმარებლისთვის ნაბიჯები: SQL Server ჰოსტზე ან ხელმისაწვდომ სერვერზე, ბაზის შექმნა.
   - მიგრაციები `--idempotent` სკრიპტით: DbTools workspace-ში `dotnet ef migrations script --idempotent`, scratch ასლით, README §7-ის წესი 10.
   - ყურადღება: PAZISI-ს Dev ბაზაში შეიძლება ძველი მიგრაციის ID-ები ეწეროს (Initial რამდენჯერმე გადაკეთდა; ბოლოა `20260929190002_Initial`). ასეთ შემთხვევაში `database update` ჩავარდება. მომხმარებელს სთხოვე, ჯერ `SELECT MigrationId FROM __EFMigrationsHistory` შეამოწმოს. გადაწყვიტეთ: ID-ის რეგისტრაცია, თუ Dev ბაზის თავიდან შექმნა (მასში ცოტა მონაცემია).

3. დეპლოი SupportTools-ის ხელსაწყოებით: ProgPublisher → ProgramInstaller/Updater (WebAgent-ით ან ლოკალურად), ServiceInstallScriptCreator, AppSettingsEncoder/Installer, VersionChecker. მომხმარებლისთვის ნაბიჯ-ნაბიჯ ინსტრუქცია.

4. კლიენტების კონფიგურაცია ყველა კომპიუტერზე:
   - ApiClient `<host>.SupportToolsServer`: Server URL `/api/v1`-ით, ApiKey;
   - `SupportToolsServerWebApiClientName`;
   - VersionChecker-ის შემოწმება.

5. უსაფრთხოება და backup:
   - TLS, firewall-ის პორტი;
   - DB-ის backup-ის გეგმა (backup საიდუმლოებებს შეიცავს, G2);
   - ლოგების ადგილი და შიგთავსი: საიდუმლოებები ლოგში არ იწერება.

6. Runbook: D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\Deployment.md. შეიცავს ყველა ზემოთ ჩამოთვლილს:
   - გადაწყვეტილებებს;
   - ბრძანებებს;
   - შემოწმების ნაბიჯებს: `api/v1/test/getversion` ანონიმურად; რეესტრის endpoint გასაღების გარეშე → 401, გასაღებით → 200.

არ გააკეთო: რეალური საიდუმლოების ფაილში ჩაწერა; მომხმარებლის user secrets-ის წაკითხვა; დეპლოის თვითნებურად გაშვება. დეპლოი, ბაზაზე ოპერაციები და სერვისის ინსტალაცია მომხმარებლის ნაბიჯებია.

დასრულების კრიტერიუმები:
- კოდის ცვლილებები build/test მწვანეა;
- runbook მზადაა;
- მომხმარებელს აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში D1-ის სტატუსი განაახლე (runbook-ის შესრულებამდე „🔄“).
````
