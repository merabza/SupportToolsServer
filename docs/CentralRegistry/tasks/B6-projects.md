# B6 — Projects-ის აგრეგატი

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B2 (NpmPackage), B3 (DB კავშირები, SmartSchema, FileStorage); არსებული GitRepo და EditorConfigFileType |
| ზომა | XL. თუ კონტექსტი არ ეყოფა, შეიძლება ორ სესიად დაიყოს: (ა) Domain + DbPart + მიგრაცია, (ბ) Application + WebApi + Shared |

## პრომპტი

````text
ეს არის ამოცანა B6 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G1/G2/G3/G7/G8, §4.2, §4.4, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“;
- წინა ნაკვეთების კოდი.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: Project-ის აგრეგატი სერვერზე, სრულად რელაციური (G1). ServerInfo-ები ამ ამოცანაში არ შედის; ისინი B7-ში დაემატება იმავე აგრეგატში.

წყარო: D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\ProjectModel.cs (`SupportToolsParameters.Projects: Dictionary<string, ProjectModel>`). `ProjectModel`-ს საკუთარი სახელის ველი არ აქვს: სახელი dictionary-ის key-ა. რეალურ მონაცემებში 61 პროექტია.

ნაბიჯი 0. სქემის შეთანხმება. კოდის წერამდე მომხმარებელს წარუდგინე ცხრილები, ველები, ტიპები, სიგრძეები და FK-ები, და დაელოდე დასტურს.

Project-ის ფესვის ველები. ყველა არასავალდებულოა, თუ სხვა რამ არ წერია.
- იდენტობა და აღწერა:
  - `Name` (სავალდებულო, უნიკალური);
  - `ProjectType`: enum `EProjectType` (Standard, IsService, IsPackage) სტრიქონად;
  - `ProjectGroupName`, `ProjectDescription`;
  - `MajorVersion`, `MinorVersion`;
  - `UseAlternativeWebAgent`;
  - `EditorConfigPatternName` → FK EditorConfigFileType, nullable, Restrict.
- სახელები (string):
  - `MainProjectName`, `ApiContractsProjectName`, `SpaProjectName`, `DbContextName`, `ProjectShortPrefix`, `ScaffoldSeederProjectName`, `DbContextProjectName`, `NewDataSeedingClassLibProjectName`;
  - ნიღბები: `ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`.
- კანონიკური გზები (G3). სერვერი მათ ინახავს ისე, როგორც მოვიდა, და ამოწმებს მხოლოდ სიგრძეს:
  - `ProjectFolderName`, `SolutionFileName`, `ProjectSecurityFolderPath`;
  - `MigrationStartupProjectFilePath`, `MigrationProjectFilePath`;
  - `DataSeederRulesByTableStartupProjectFilePath`, `OldDataConvertorForDataSeeder`;
  - `SeedProjectFilePath`, `SeedProjectParametersFilePath`;
  - `ExcludesRulesParametersFilePath`, `AppSetEnKeysJsonFileName`, `MigrationSqlFilesFolder`;
  - `PrepareProdCopyDatabaseProjectFilePath`, `PrepareProdCopyDatabaseProjectParametersFilePath`;
  - `PairedDbObjectsResultFileName`.
- საიდუმლო: `KeyGuidPart`. ღიად ინახება (G2) და არსად იბეჭდება.

ორი owned DatabaseParameters: `DevDatabaseParameters` და `ProdCopyDatabaseParameters`.
- წყარო: D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibDatabaseParameters\DatabaseParameters.cs.
- ველები:
  - `DbConnectionName` → FK DatabaseServerConnection;
  - `DatabaseRecoveryModel` (სტრიქონი);
  - `DbServerFoldersSetName`, `DatabaseName`;
  - `SmartSchemaName` → FK SmartSchema;
  - `FileStorageName` → FK FileStorage;
  - `CommandTimeOut`, `SkipBackupBeforeRestore`;
  - `BackupNamePrefix`, `DateMask`, `BackupFileExtension`, `BackupNameMiddlePart`;
  - `Compress`, `Verify`;
  - `BackupType` (სტრიქონი).
- ყველა FK nullable-ია და Restrict.
- მოდელირება: EF owned type (`OwnsOne`), ცალკე ცხრილში ან table splitting-ით. აირჩიე და დაასაბუთე. B7 იმავე ტიპს ServerInfo-ს `CurrentDatabaseParameters`-ისა და `NewDatabaseParameters`-ისთვის გამოიყენებს, ამიტომ ხელახლა გამოყენებადად გააკეთე.

აგრეგატის შვილები (cascade):
- `ProjectGitRepo`:
  - ველები: ProjectId, GitRepoId (FK, Restrict), `Kind` (Main | ScaffoldSeed);
  - unique (ProjectId, GitRepoId, Kind);
  - წყარო: `GitProjectNames` და `ScaffoldSeederGitProjectNames`.
- `ProjectNpmPackage`: FK → NpmPackage, Restrict. წყარო: `FrontNpmPackageNames`.
- `ProjectRedundantFile`: `FileName`. წყარო: `RedundantFileNames`.
- `ProjectAllowedTool`: `ToolName`, ანუ `EProjectTools`-ის სახელი სტრიქონად; სერვერი enum-ს არ იცნობს. წყარო: `AllowToolsList`.
- `ProjectEndpoint`:
  - ველები: key, `EndpointName`, `EndpointRoute`, `RequireAuthorization`, `HttpMethod` (სტრიქონი), `EndpointType` (სტრიქონი), `ReturnType`, `SendMessageToCurrentUser`;
  - წყარო: `Endpoints: Dictionary<string, EndpointModel>`.
- `ProjectRouteClass`:
  - ველები: key, `Root`, `ApiVersion`, `Base`;
  - წყარო: `RouteClasses`. კლიენტის ველს `Version` ჰქვია. სერვერზე და კონტრაქტში სხვა სახელი დაარქვი, რომ აგრეგატის `Version`-ში არ აგერიოს. mapping-ს C4 გააკეთებს.

აგრეგატის სემანტიკა (G7):
- update მთელ აგრეგატს ანაცვლებს: შვილი კოლექციები ახლით იცვლება და ფესვის `Version` ნებისმიერ ცვლილებაზე იზრდება.
- GET-ის სია სრულ აგრეგატებს აბრუნებს; 61 პროექტისთვის ეს მისაღებია. ბევრი `Include`-ის გამო გამოიყენე `AsSplitQuery`.
- კონტრაქტში სიმრავლის სიები სახელით დალაგებულად ბრუნდება (`OrdinalIgnoreCase`), რომ კლიენტის ჰეში სტაბილური იყოს: git-ების სიები, npm, redundant files, allowed tools. endpoint-ები და route class-ები key-ით დალაგებული სიაა ან dictionary.

კონტრაქტი: `StsProjectDataModel`.
- შეიცავს ყველა ზემოთ ჩამოთვლილ ველს.
- მითითებები სახელებითაა: `EditorConfigPatternName`, `GitProjectNames`, `ScaffoldSeederGitProjectNames`, `FrontNpmPackageNames`, `DbConnectionName` და სხვ.
- ჩადგმული `StsDatabaseParametersDataModel` ორი ეგზემპლარით.
- აქვს `Version`.
- B7-ისთვის კონტრაქტში დაიტოვე ადგილი ServerInfo-ების სიისთვის, მაგრამ ჯერ არ დაამატო.

ვალიდაცია:
- სიგრძეები;
- ყველა მითითებული სახელი უნდა არსებობდეს: git-ები, npm, editorconfig, DB კავშირი, SmartSchema, FileStorage. თუ არ არსებობს, 404 `ReferencedRecordsNotFound` სრული სიით;
- სიებში დუბლიკატები დაუშვებელია.

წაშლის შემოწმებები. Project-ის წაშლა შვილებს cascade-ით შლის. დანარჩენი delete handler-ები ახლა პროექტებსაც უნდა ამოწმებდეს → 409 `RecordIsInUse` პროექტების სახელებით:
- GitRepo: არსებულ `DeleteGitRepo`-ს დღეს შემოწმება საერთოდ არ აქვს;
- NpmPackage, EditorConfigFileType, DatabaseServerConnection, SmartSchema, FileStorage;
- EditorConfigFileType-ის SyncUp `merge=false`-ით: გამოყენებულ ტიპს აღარ უნდა შლიდეს. ნიმუშია GitIgnoreFileType-ის არსებული შემოწმება.

ფენები და ტესტები Environments-ის ნიმუშითაა, დამატებით:
- აგრეგატის ჩანაცვლება: შვილების დამატება, წაშლა და ცვლილება;
- ვერსიის ზრდა მხოლოდ შვილის ცვლილებისას;
- owned პარამეტრების FK-ები;
- რეპოზიტორის ტესტი SQLite-ზე, `Foreign Keys=True`-ით;
- ზემოთ ჩამოთვლილი 409-ები.

მიგრაცია: `AddProjects` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები;
- CLAUDE.md-ში აგრეგატის წესები ჩაწერილია: სრული ჩანაცვლება, დალაგებული სიები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B6-ის სტატუსი განაახლე.
````
