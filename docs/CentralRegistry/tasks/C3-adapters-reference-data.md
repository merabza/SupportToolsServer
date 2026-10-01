# C3 — ადაპტერები: ცნობარები, რესურსები, სერვერები, პარამეტრები, გიტები, შაბლონები

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportTools\SupportToolsServerShared` (მხოლოდ წასაკითხად), `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools |
| დამოკიდებულია | C1, C2, B1–B5 (და SupportTools workspace-ის SupportToolsServerShared კლონის pull) |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა C3 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3, §4.1, §4.3, §4.4, §7);
- ორივე CLAUDE.md;
- C1-ის (PathMapper, კომპიუტერის ველები) და C2-ის (ადაპტერის ინტერფეისი, ჰეში, მდგომარეობა) კოდი.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

წინაპირობა:
- D:\1WorkDotnet\SupportTools\SupportToolsServerShared კლონი იმავე commit-ზე უნდა იყოს, რაც D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared (`git log -1`). ასე უნდა იყოს SystemTools-იც.
- თუ ეს ასე არ არის, შეჩერდი და მომხმარებელს pull სთხოვე. კონტრაქტები და კლიენტის მეთოდები B1–B5-მა უკვე შექმნა.

მიზანი: C2-ის ადაპტერები ყველა კოლექციისთვის, Projects-ის გარდა (ის C4-ია). თითო ადაპტერს სჭირდება:
- mapper: ლოკალური მოდელი ↔ `Sts…DataModel`;
- ნორმალიზაცია ჰეშისთვის: `""` → null, სიმრავლეების დალაგება `OrdinalIgnoreCase`-ით, enum-ები სახელებად;
- სერვერის გამოძახება `SupportToolsServerApiClient`-ით (`SupportToolsParameters.GetSupportToolsServerApiClient`);
- `ApplyLocal`/`RemoveLocal`, კომპიუტერის ველების შენარჩუნებით.
კოდის ადგილი: `LibSupportToolsServerWork\Registry\Adapters\` და `...\Mappers\`.

კოლექციები და განსაკუთრებული წესები:
- **Environments, RunTimes, NpmPackages, ReactAppTemplates**: `Dictionary<string,string>` ↔ კონტრაქტი.
- **DotnetTools**: მხოლოდ `PackageId`, `MaxVersion`, `Description`. `ApplyLocal` არ ეხება `InstalledVersion`, `LatestVersion` და `CommandName` ველებს.
- **SmartSchemas** (დეტალებით), **FileStorages**, **ApiClients**, **DatabaseServerConnections** (folders set-ებით): საიდუმლოებები მიდის და მოდის (G2), მაგრამ არსად იბეჭდება.
  - ApiClient, რომელსაც `SupportToolsServerWebApiClientName` ასახელებს, ავტომატურად გამოირიცხება სინქრონიზაციიდან (G6, bootstrap).
  - `FileStorageData.FileStoragePath`, თუ ლოკალური აბსოლუტური გზაა და არა URL, PathMapper-ით გარდაიქმნება.
- **Servers**: `IsLocal` არ მიდის. `ApplyLocal`-ის შემდეგ `IsLocal` ხელახლა გამოითვლება `CurrentMachineServerName`-ით (C1-ის ლოგიკით).
- **GlobalSettings**: წყაროა `SupportToolsParameters`-ის ზედა დონის საერთო ველები და `DatabasesBackupFilesExchangeParameters` (`LocalPath`-ის გარდა). ერთი ჩანაწერია, singleton-ის გასაღებით, B5-ის მიხედვით.
- **ProjectCreatorSettings**: `AppProjectCreatorAllParameters`, `Templates`-ის გარეშე.
  - `ProjectsFolderPathReal` და `SecretsFolderPathReal` PathMapper-ით გარდაიქმნება.
  - თუ ობიექტი null-ია, `ApplyLocal` შექმნის.
- **ProjectTemplates**: `AppProjectCreatorAllParameters.Templates`.
- **Gits**: `GitDataModel` ↔ `StsGitDataModel`.
  - `GitProjectFolderName` შეფარდებითია და prefix-mapping-ს არ საჭიროებს; შეიძლება შეიცავდეს `{SpaProjectFolderRelativePath}`-ს.
  - არსებული endpoint-ები (`updategitrepo/{key}`, `deletegitrepo/{key}`) და მათი ვერსიის ქცევა გადაამოწმე B1-ის შემდეგ. თუ upsert/delete ვერსიით ჯერ არ არის, ჰკითხე: სერვერზე დაემატოს, თუ კლიენტი ამ კოლექციას ვერსიის გარეშე გაატაროს.
- **GitIgnore შაბლონები** და **EditorConfig შაბლონები**:
  - ლოკალური ჩანაწერი = სახელი `GitIgnorePatterns`/`EditorConfigPatterns` სიიდან + ფაილის შიგთავსი:
    - `{FolderForGitignoreFiles}\{name}.gitignore` (`SupportToolsParameters.GetGitIgnoreModelFilePath`);
    - `{FolderForEditorConfigFiles}\{name}.editorconfig` (`GetEditorConfigPatternFilePath`).
  - `ApplyLocal` ფაილს წერს (ფოლდერის შექმნით) და სიას ანახლებს. `RemoveLocal` სიიდან შლის; ფაილის წაშლაზე მომხმარებელს ჰკითხე.
  - თუ სიაში არსებული სახელის ფაილი არ არსებობს, ეს ლოკალური ჩანაწერის შეცდომაა: გაფრთხილება და გამორიცხვა, არა წაშლა სერვერიდან.
  - არსებულ ბრძანებებს (`SyncGitignoreFilesCliMenuCommand`, `SyncEditorConfigFilesCliMenuCommand`, `SyncGitProjectsCliMenuCommand`) ამ ეტაპზე არ ეხები; მათ ბედს D4 გადაწყვეტს.

დამოკიდებულების რიგი (`Order`): Environments, RunTimes, NpmPackages, ReactAppTemplates, DotnetTools, SmartSchemas, FileStorages, ApiClients → DatabaseServerConnections → Servers → GitIgnore შაბლონები → Gits → EditorConfig შაბლონები → ProjectTemplates → GlobalSettings → ProjectCreatorSettings. Projects (C4) ბოლოა.

ფაბრიკა: ერთი ადგილი, რომელიც ყველა ადაპტერს აწყობს. C4 მას Projects-ს დაუმატებს, C5 კი გამოიყენებს.

ტესტები (SupportTools.Tests):
- თითო mapper-ზე round-trip: ლოკალური → კონტრაქტი → ლოკალური = საწყისი, კომპიუტერის ველების შენარჩუნებით;
- ჰეშის სტაბილურობა: სიის რიგი და `""`/null ჰეშს არ ცვლის;
- PathMapper-ის გამოყენება Linux-ის სცენარში (FileStoragePath, ProjectCreator-ის გზები);
- bootstrap ApiClient-ის გამორიცხვა;
- `IsLocal`-ის ხელახალი გამოთვლა;
- შაბლონების ფაილების წერა დროებით ფოლდერში;
- სერვერის კლიენტი fake HttpMessageHandler-ით ან ადაპტერის ინტერფეისის ზემოთ fake-ით.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- ყველა ჩამოთვლილ კოლექციას ადაპტერი აქვს და ფაბრიკაშია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში C3-ის სტატუსი განაახლე.
````
