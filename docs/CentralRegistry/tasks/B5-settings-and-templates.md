# B5 — გლობალური პარამეტრები, პროექტის შემქმნელის პარამეტრები, შაბლონები

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B4 (და B2, B3) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა B5 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3, §4.1, §4.2, §4.4, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“;
- წინა ნაკვეთების კოდი.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: სამი აგრეგატი. ორი მათგანი singleton-ია, ერთი ჩვეულებრივი კოლექცია.

1. `GlobalSettings` (singleton). წყაროა კლიენტის `SupportToolsParameters`-ის საერთო სკალარული ველები და `DatabasesBackupFilesExchangeParameters`-ის საერთო ნაწილი (D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibDatabaseParameters\DatabasesBackupFilesExchangeParameters.cs).
   - სტრიქონები:
     - `ServiceDescriptionSignature`, `UploadTempExtension`;
     - `ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`;
     - `MediatRLicenseKey`: საიდუმლოა, ღიად ინახება (G2) და არსად იბეჭდება.
   - მითითებები (FK, nullable, Restrict):
     - `FileStorageNameForExchange` → FileStorage;
     - `SmartSchemaNameForExchange`, `SmartSchemaNameForLocal` → SmartSchema;
     - `LocalPackageManagerWebApiClientName` → ApiClient.
   - DatabasesBackupFilesExchange:
     - `DownloadTempExtension`, `UploadTempExtension`;
     - `ExchangeFileStorageName` → FileStorage;
     - `ExchangeSmartSchemaName`, `LocalSmartSchemaName` → SmartSchema.
     - `LocalPath` კომპიუტერისაა და სერვერზე არ მიდის.
   - სერვერზე **არ** მიდის: `SupportToolsServerWebApiClientName` (bootstrap), `LocalInstallerSettings` (კომპიუტერის), `Archivers` და კომპიუტერის ფოლდერები (README §4.1).

2. `ProjectCreatorSettings` (singleton). წყარო: D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\AppProjectCreatorAllParameters.cs, `Templates`-ის გარეშე.
   - ველები:
     - `IndentSize`, `FakeHostProjectName`;
     - `ProjectsFolderPathReal`, `SecretsFolderPathReal`: კანონიკური გზები (G3); სერვერი ინახავს ისე, როგორც მოვიდა;
     - `ProductionServerName` → Server;
     - `ProductionEnvironmentName` → Environment;
     - `DeveloperDbConnectionName` → DatabaseServerConnection;
     - `DatabaseExchangeFileStorageName` → FileStorage;
     - `UseSmartSchema` → SmartSchema. სახელის მიუხედავად, ეს SmartSchema-ს სახელია.
   - ყველა FK nullable-ია და Restrict.

3. `ProjectTemplate` (კოლექცია). წყარო: D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\TemplateModel.cs (`AppProjectCreatorAllParameters.Templates`).
   - ველები:
     - `Name` (key);
     - `SupportProjectType`: სტრიქონი, enum `ESupportProjectType`;
     - `TestProjectName`, `TestProjectShortName`;
     - bool-ები: `UseDatabase`, `UseDbPartFolderForDatabaseProjects`, `UseMenu`, `UseHttps`, `UseReact`, `UseCarcass`, `UseIdentity`, `UseReCounter`, `UseSignalR`, `UseFluentValidation`;
     - `ReactTemplateName` → ReactAppTemplate (FK, nullable, Restrict).

singleton-ის მოდელირება:
- ცხრილში ზუსტად ერთი ჩანაწერია ფიქსირებული გასაღებით. აირჩიე და დაასაბუთე.
- endpoint-ები:
  - `GET settings/global`;
  - `POST settings/global/update` — body კონტრაქტია `Version`-ით; პირველი შექმნა `Version == 0`-ით;
  - იგივე წყვილი `settings/projectcreator`-ისთვის.
- თუ ჩანაწერი ჯერ არ არსებობს, GET აბრუნებს 404-ს ან ცარიელ კონტრაქტს `Version = 0`-ით. აირჩიე ის ვარიანტი, რომელიც კლიენტის სინქრონიზაციისთვის (C2/C3) უფრო მარტივია, და CLAUDE.md-ში ჩაწერე.
- route-ის კლასები Shared-ში B1-ის კონვენციით.

წაშლის შემოწმებები: FileStorage, SmartSchema, ApiClient, Server, Environment, DatabaseServerConnection და ReactAppTemplate ახლა ამ აგრეგატებსაც შეიძლება მიმართავდეს. გააფართოე მათი delete handler-ები: 409 `RecordIsInUse`, მომხმარებლის მითითებით (მაგ. „GlobalSettings.FileStorageNameForExchange“, „ProjectTemplate X“).

ვალიდაცია: ყველა მითითებული სახელი უნდა არსებობდეს. თუ რომელიმე არ არსებობს, 404 `ReferencedRecordsNotFound` ყველა აკლებული სახელით.

მიგრაცია: `AddSettingsAndTemplates` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- ტესტები ფარავს singleton-ის პირველ შექმნას, ვერსიის კონფლიქტს და მითითებული ჩანაწერის წაშლის 409-ს;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B5-ის სტატუსი განაახლე.
````
