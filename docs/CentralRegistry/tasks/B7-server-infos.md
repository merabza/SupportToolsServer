# B7 — ServerInfo-ები Project-ის აგრეგატში

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B6, B4 (Server), B1 (Environment), B3 (ApiClient) |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა B7 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G1/G3/G7, §4.2, §4.4, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“ და აგრეგატის წესები;
- B6-ის კოდი.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: ServerInfo-ები Project-ის აგრეგატის შვილებად (G7). წყარო არის კლიენტის D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\ServerInfoModel.cs (`ProjectModel.ServerInfos: Dictionary<string, ServerInfoModel>`). რეალურ მონაცემებში 42 ჩანაწერია.

`ServerInfo` (cascade Project-იდან). ველები:
- ServerId → FK Server, სავალდებულო, Restrict;
- EnvironmentId → FK Environment, სავალდებულო, Restrict;
- `WebAgentNameForCheck` → FK ApiClient, nullable, Restrict;
- `ServerSidePort` (int), `ApiVersionId`;
- `AppSettingsJsonSourceFileName`, `AppSettingsEncodedJsonFileName`: კანონიკური გზები (G3); სერვერი მხოლოდ სიგრძეს ამოწმებს;
- `ServiceUserName`;
- owned `CurrentDatabaseParameters` და `NewDatabaseParameters`: B6-ის DatabaseParameters ტიპი, იმავე FK-ებით.
- შვილი `ServerInfoAllowedTool`: `ToolName`, ანუ `EProjectServerTools`-ის სახელი სტრიქონად.
- unique (ProjectId, ServerId, EnvironmentId).

გასაღებები:
- კლიენტის dictionary-ის key-ები ნატურალური გასაღები არ არის. ზოგი GUID-ია (`ServerInfoCruder`, `fieldKeyFromItem=true`), ზოგი `"Server|Env"` (`ProjectRecordCreator`).
- ნატურალური გასაღებია (ServerName, EnvironmentName). სწორედ ეს გამოიყენე.
- კლიენტის key სერვერზე არ მიდის. ლოკალური key-ის შენარჩუნება C4-ის საქმეა.

კონტრაქტი:
- `StsProjectDataModel`-ს ემატება `ServerInfos: List<StsServerInfoDataModel>`, დალაგებული (ServerName, EnvironmentName)-ით.
- ჩანაწერის ველები: `ServerName`, `EnvironmentName`, `WebAgentNameForCheck`, `ServerSidePort`, `ApiVersionId`, `AppSettingsJsonSourceFileName`, `AppSettingsEncodedJsonFileName`, `ServiceUserName`, `AllowToolsList` (დალაგებული სტრიქონები), `CurrentDatabaseParameters`, `NewDatabaseParameters`.
- ServerInfo-ს საკუთარი ვერსია არ აქვს: ის აგრეგატის ფესვის `Version`-ის ნაწილია.

ვალიდაცია:
- (ServerName, EnvironmentName) პროექტის შიგნით უნიკალურია → `ValuesNotUnique`;
- Server, Environment, ApiClient და DB პარამეტრების მითითებები უნდა არსებობდეს → 404 `ReferencedRecordsNotFound`.

Project-ის update handler აგრეგატს ServerInfo-ებითურთ ანაცვლებს. ServerInfo-ს დამატება, წაშლა ან ცვლილება ფესვის ვერსიას ზრდის.

წაშლის შემოწმებები: Server-ის, Environment-ისა და ApiClient-ის delete handler-ები ახლა ServerInfo-ებსაც ამოწმებს → 409 `RecordIsInUse` „Project X / Server|Env“ ფორმატის მითითებებით. ასევე შეამოწმე DatabaseServerConnection, SmartSchema და FileStorage, რადგან ServerInfo-ს DB პარამეტრებიც მიმართავს მათ.

ტესტები:
- ServerInfo-ების დამატება, ცვლილება და წაშლა აგრეგატის განახლებით;
- უნიკალურობის დარღვევა;
- FK-ების ვალიდაცია;
- 409-ები;
- რეპოზიტორი SQLite-ზე;
- კონტრაქტის დალაგება;
- route-ების სიის ტესტი, თუ ახალი route დაემატა.

მიგრაცია: `AddServerInfos` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B7-ის სტატუსი განაახლე.
````
