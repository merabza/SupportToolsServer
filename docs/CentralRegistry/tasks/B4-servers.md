# B4 — Servers

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B2 (Runtime), B3 (ApiClient) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა B4 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G1/G6, §4.1, §4.2, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“;
- წინა ნაკვეთების კოდი, განსაკუთრებით FK-იანი DatabaseServerConnection (B3).
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: `Server` აგრეგატი სერვერზე. წყარო არის კლიენტის D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\ServerDataModel.cs (`SupportToolsParameters.Servers: Dictionary<string, ServerDataModel>`).

ველები:
- `Name`: dictionary-ის key, მაგალითად PAZISI, dl360, guria, bee, Formula, Merinson.
- `WebAgentName` და `WebAgentInstallerName`: FK → ApiClient, nullable, Restrict.
- `FilesUserName`, `FilesUsersGroupName`: OS ანგარიშები სამიზნე სერვერზე.
- `Runtime`: FK → Runtime, nullable, Restrict.
- `ServerSideDownloadFolder`, `ServerSideDeployFolder`: გზები სამიზნე სერვერზე. კლიენტის path-mapping მათ არ ეხება.
- `IsLocal` სერვერზე **არ** ინახება (G6). კომპიუტერზე ის `CurrentMachineServerName`-ით გამოითვლება (C1).

წესები:
- ვალიდაცია: ყველა მითითებული სახელი (ApiClient-ები, Runtime) უნდა არსებობდეს. თუ არ არსებობს, 404 `ReferencedRecordsNotFound` ყველა აკლებული სახელის სიით.
- წაშლა: Server-ს მომავალში მიმართავს ProjectCreatorSettings.ProductionServerName (B5) და ServerInfo (B7). ეს ამოცანები 409-ის შემოწმებას თვითონ დაამატებს.
- აქ გააფართოე Runtime-ისა და ApiClient-ის delete handler-ების შემოწმება Servers-ის მიმართვებით: 409 `RecordIsInUse` სერვერების სახელებით.
- გადარქმევა: სერვერის სახელი AppSettings-ის დაშიფვრის გასაღების ნაწილია (`KeyGuidPart + ServerName.Capitalize()`, კლიენტის `EncodeParametersAction.cs:81`). ამიტომ გადარქმევის endpoint არ კეთდება; გადარქმევა = წაშლა + შექმნა. ეს CLAUDE.md-ში ჩაწერე.
- კონტრაქტი: `StsServerDataModel { Name, WebAgentName, WebAgentInstallerName, FilesUserName, FilesUsersGroupName, Runtime, ServerSideDownloadFolder, ServerSideDeployFolder, Version }`, სახელებით და არა Id-ებით.

ფენები და ტესტები Environments-ის ნიმუშითაა. ტესტები სპეციალურად ფარავს FK-ების ვალიდაციას და Runtime-ისა და ApiClient-ის წაშლის 409-ს, როცა სერვერი მათ იყენებს.

მიგრაცია: `AddServers` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B4-ის სტატუსი განაახლე.
````
