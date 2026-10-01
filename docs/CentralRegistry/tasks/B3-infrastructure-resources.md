# B3 — რესურსები: SmartSchemas, FileStorages, ApiClients, DatabaseServerConnections

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B1 (და A3: საიდუმლოებები მხოლოდ ავთენტიფიკაციის შემდეგ) |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა B3 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G1/G2, §4.1, §4.2, §4.5, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“;
- Environments-ის და B2-ის ნაკვეთების კოდი. ის შაბლონია.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: ოთხი რესურსის აგრეგატი სერვერზე, საიდუმლო ველების ჩათვლით. G2-ის მიხედვით საიდუმლოებები ღიად ინახება. ფენები ზუსტად Environments-ის ნიმუშითაა: Domain, რეპოზიტორი, configuration და DbSet, Application, endpoint-ები, კონტრაქტი და კლიენტი, ტესტები.

წყარო მოდელები. წაიკითხე SupportTools workspace-ში: ეს კლონები SupportTools-ს ემსახურება და ყველაზე ახალია.
- D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibFileParameters\Models\: `SmartSchema.cs`, `SmartSchemaDetail.cs`, `FileStorageData.cs`.
- D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibApiClientParameters\ApiClientSettings.cs.
- D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibDatabaseParameters\DatabaseServerConnectionData.cs.
- D:\1WorkDotnet\SupportTools\DatabaseTools\DatabaseTools.DbTools\Models\DatabaseFoldersSet.cs.
- enum-ები: `EPeriodType`, `EDatabaseProvider` (იპოვე grep-ით).

აგრეგატები:

1. `SmartSchema`.
   - ველები: `Name`, `LastPreserveCount`.
   - შვილი `SmartSchemaDetail` (cascade): `PeriodType` სტრიქონად (enum-ის სახელი), `PreserveCount`.
   - შეამოწმე, მნიშვნელოვანია თუ არა დეტალების რიგი და შეიძლება თუ არა დუბლიკატები. ამის მიხედვით აირჩიე გასაღები და დალაგება. კონტრაქტში რიგი დეტერმინისტული უნდა იყოს.

2. `FileStorage`.
   - ველები: `Name`, `FileStoragePath`, `UserName`, `Password`, `FileNameMaxLength`, `FileSizeSplitPositionInRow`, `FtpSiteLsFileOffset`.
   - `FileStoragePath` შეიძლება იყოს URL (ftp://…) ან ლოკალური გზა. სერვერი მას ინახავს ისე, როგორც მოვიდა; გზების გარდაქმნა კლიენტის საქმეა (C1/C3).

3. `ApiClient`.
   - ველები: `Name`, `Server` (URL), `ApiKey`.

4. `DatabaseServerConnection`.
   - ველები:
     - `Name`;
     - `DatabaseServerProvider` (სტრიქონი);
     - `DbWebAgentName`: FK → ApiClient, nullable, Restrict;
     - `RemoteDbConnectionName`, `ServerAddress`;
     - `WindowsNtIntegratedSecurity`;
     - `ServerUser`, `ServerPass`;
     - `TrustServerCertificate`, `ConnectionTimeOut`, `Encrypt`.
   - შვილი `DatabaseFoldersSet` (cascade): კლიენტში `Dictionary<string, DatabaseFoldersSet>?`. ველები: სახელი (key, connection-ის შიგნით უნიკალური), `Backup`, `Data`, `DataLog`. ეს გზები DB სერვერზეა და არ გარდაიქმნება.

წესები:
- enum-ები სტრიქონად ინახება. სერვერი კლიენტის enum-ებს არ იცნობს; ამოწმებს მხოლოდ სიგრძეს. თუ დასაშვები მნიშვნელობების შემოწმება გინდა, ჰკითხე.
- საიდუმლოები (`Password`, `ApiKey`, `ServerPass`, `ServerUser`) არ უნდა მოხვდეს `Debug.WriteLine` ტრეისში, ლოგში, შეცდომის ტექსტში ან ვალიდაციის შეტყობინებაში. ტრეისი შეიცავდეს მხოლოდ სახელს. ტესტებში გამოიყენე გამოგონილი მნიშვნელობები.
- კონტრაქტები: `StsSmartSchemaDataModel` (დეტალების სიით), `StsFileStorageDataModel`, `StsApiClientDataModel`, `StsDatabaseServerConnectionDataModel` (folders set-ების სიით/dictionary-ით), ყველა `Version`-ით. ვალიდაციისას `DbWebAgentName`-ის არარსებული სახელი → 404 `ReferencedRecordsNotFound`.
- წაშლა: ApiClient-ის წაშლისას შეამოწმე DatabaseServerConnections-ის მიმართვები → 409 `RecordIsInUse`. B4–B7 ამ შემოწმებას ახალი მომხმარებლებით გააფართოებს.
- სიგრძეები: სახელებისა და გზების რეალური სიგრძეები შეგიძლია გადაამოწმო D:\1WorkSecurity\SupportTools\SupportTools.json-ში, მხოლოდ შემდეგი გასაღებების grep-ით: `"FileStoragePath"`, `"Server"` (ApiClients სექციაში), `"ServerAddress"`, `"Backup"`, `"Data"`, `"DataLog"`. პაროლების, მომხმარებლებისა და გასაღებების ხაზებს არ კითხულობ.

მიგრაცია: `AddInfrastructureResources` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- ტესტები ფარავს FK-ს არარსებულ ApiClient-ზე, ApiClient-ის წაშლის 409-ს და შვილი კოლექციების ჩანაცვლებას განახლებისას;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B3-ის სტატუსი განაახლე.
````
