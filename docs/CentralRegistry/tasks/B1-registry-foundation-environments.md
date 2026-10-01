# B1 — რეესტრის საფუძველი (`Version`, კონვენციები) და Environments

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools` |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | A1, A3 |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა B1 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3, §4.2, §4.3, §7);
- რეპოს CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი: სერვერის მოდელი სრულად რელაციურია (G1). სინქრონიზაცია 3-მხრივია და ჩანაწერის `Version`-ს ეყრდნობა (G5); აგრეგატები G7-ითაა განსაზღვრული, სახელების დამთხვევა G8-ით. B1 აწესებს კონვენციებს, რომლებსაც B2–B8 მიჰყვება, და აკეთებს ერთ საცნობარო ნაკვეთს (Environments), რომელსაც დანარჩენები დააკოპირებს.

ნაბიჯი 0. დიზაინის შეთანხმება კოდის წერამდე. მომხმარებელს წარუდგინე მოკლე დოკუმენტი ხუთ საკითხზე და დაელოდე დასტურს:
- `Version`-ის მექანიზმი;
- upsert/delete სემანტიკა;
- route-ების კონვენცია;
- შეცდომების კოდები;
- Environments-ის სქემა.

სამუშაო:

1. Version (optimistic concurrency).
   - რეესტრის ყოველ აგრეგატის ფესვს აქვს `int Version`. შექმნისას ის 1-ია და ყოველ ცვლილებაზე 1-ით იზრდება; ზრდა დომენის მეთოდში ხდება. EF-ში ის concurrency token-ია.
   - `Version` დაემატოს არსებულ `GitRepo`-ს, `GitIgnoreFileType`-სა და `EditorConfigFileType`-საც. არსებულმა handler-ებმა, რომლებიც მათ ცვლიან (UpdateGitRepo, UploadGitRepos, SyncUp-ები, EnsureGitIgnoreFileType), ვერსია უნდა გაზარდონ.
   - `GitIgnoreFileType` და `EditorConfigFileType` ახლა „მხოლოდ კონსტრუქტორით“ იქმნება: განახლებისას ახალი ეგზემპლარი შენახული Id-ით გადაეცემა. ამ შემთხვევაში ვერსია ცხადად უნდა გადავიდეს (`stored.Version + 1`).
   - აირჩიე ერთიანი ნიმუში, მაგალითად `Entity<TId>`-ის მემკვიდრე საბაზო კლასი ან ინტერფეისი, და აღწერე CLAUDE.md-ში.
   - მნიშვნელოვანი EF დეტალი. რეპოზიტორები no-tracking-ით კითხულობენ და ობიექტს `Update(entity)`-ს გადასცემენ. ასეთ დროს EF concurrency token-ის „ორიგინალ“ მნიშვნელობად მიმდინარე, უკვე გაზრდილ ვერსიას იღებს, ამიტომ `WHERE Version = <ახალი>` არასოდეს დაემთხვევა. ორი გამოსავალია; აირჩიე, დაასაბუთე და SQLite-ის ტესტით დაადასტურე (int concurrency token SQLite-ზეც მუშაობს):
     - რეპოზიტორის `Update`-ში დააყენე `Entry(e).Property(x => x.Version).OriginalValue = previousVersion`;
     - ან ვერსია მხოლოდ handler-ში შეამოწმე და token არ გამოიყენო.
   - არსებულ კონტრაქტებს (`StsGitDataModel`, `StsGitIgnoreFileTypeDataModel`, `StsEditorConfigFileTypeDataModel`) დაემატოს `int Version`, default 0, რომ ძველი CLI-ის JSON-მა ისევ იმუშაოს. არსებული endpoint-ების ქცევა არ შეცვალო: ძველი CLI ვერსიას არ აგზავნის, ამიტომ ვერსია მხოლოდ GET-ების პასუხში ჩნდება.

2. ახალი რეესტრის endpoint-ების სემანტიკა (B2–B8-ისთვის).
   - `GET {area}`: სია, სახელით დალაგებული (`OrdinalIgnoreCase`), ყველა ველითა და `Version`-ით.
   - `GET {area}/{key}`: ერთი ჩანაწერი; თუ არ არსებობს, 404 `RecordWithNameNotFound`.
   - `POST {area}/update/{key}`: upsert. body არის კონტრაქტი, რომლის `Version` მოსალოდნელი ვერსიაა.
     - `Version == 0`: შექმნა. თუ ჩანაწერი უკვე არსებობს, 409 `ConcurrencyConflict`.
     - `Version == N`: განახლება მხოლოდ მაშინ, თუ შენახული ვერსია N-ია; თორემ 409 `ConcurrencyConflict` ფაქტობრივი ვერსიით.
     - ჩანაწერი არ არსებობს და N > 0: 404 (სერვერზე წაიშალა).
     - route-ის key იმარჯვებს body-ს სახელზე, როგორც `UpdateGitRepo`-ში.
     - პასუხი არის ახალი ვერსია: `Ok<int>` ან პატარა `StsSaveResult { Version }`. აირჩიე ერთი და დააფიქსირე.
   - `DELETE {area}/delete/{key}?version=N`:
     - 404, თუ ჩანაწერი არ არსებობს;
     - 409 `ConcurrencyConflict`, თუ ვერსია არ ემთხვევა;
     - 409 `RecordIsInUse` მომხმარებლების სიით, თუ მას სხვა აგრეგატი მიმართავს;
     - `version`-ის გარეშე წაშლა უპირობოა (ხელით რედაქტორებისთვის). თუ არ ეთანხმები, ჰკითხე.
   - სახელები რეგისტრის გარეშე ემთხვევა, როგორც დღეს: handler-ში `OrdinalIgnoreCase` და unique index. SQLite-ის ტესტში unique index რეგისტრზე მგრძნობიარეა; ამას handler-ის შემოწმება ფარავს.
   - ყოველი group იბადება `.RequireAuthorization()`-ით (A3).

3. route-ების კონვენცია `SupportToolsServerApiRoutes`-ში.
   - თითო არეალს თავისი ჩადგმული სტატიკური კლასი აქვს, მაგალითად `Environments`:
     - `Base = "/environments"`;
     - სიის route `""`;
     - `ByKey = "/{key}"`;
     - `UpdatePrefix = "/update"`, `Update = UpdatePrefix + "/{key}"`;
     - `DeletePrefix = "/delete"`, `Delete = DeletePrefix + "/{key}"`.
   - სერვერი group-ს `api/v1/environments`-ზე map-ავს.
   - კლიენტი key-ს `Uri.EscapeDataString`-ით უმატებს, როგორც დღეს.

4. შეცდომების ფაბრიკები (`SupportToolsServerApiClientErrors`).
   - ზოგადი ფაბრიკები:
     - `RecordWithNameNotFound(entityName, name)` — 404;
     - `RecordIsInUse(entityName, name, usages)` — 409;
     - `ConcurrencyConflict(entityName, name, expectedVersion, actualVersion)` — 409;
     - `ReferencedRecordsNotFound(entityName, names)` — 404. გამოიყენება, როცა კონტრაქტი არარსებულ სახელს მიმართავს, მაგალითად Server.Runtime.
   - არსებული სპეციფიკური ფაბრიკები რჩება.
   - თუ ფიქრობ, რომ თითო entity-ზე სპეციფიკური კოდები სჯობს, ჰკითხე.

5. საცნობარო ნაკვეთი Environments. წყარო: `SupportToolsParameters.Environments: Dictionary<string,string>` (სახელი → აღწერა). ნაგულისხმევ ნაკრებს კლიენტის `StandardEnvironmentsGenerator` ქმნის: Prod, Stage, Test, Dev.
   - Domain:
     - entity `DeploymentEnvironment`. სახელი `Environment` არ გამოიყენო: ის `System.Environment`-ს ემთხვევა.
     - ველები: `Name` (კონსტანტა `NameMaxLength`), `Description` (არასავალდებულო, `DescriptionMaxLength`), `Version`;
     - typed Id, ფაბრიკა და განახლების მეთოდი;
     - რეპოზიტორის ინტერფეისი: `ICrudRepository` და `GetByName`.
   - DbPart: configuration (ცხრილი `Environments`, unique `Name`); DbSet ორივეგან: `ISupportToolsServerDbContext`-სა და `SupportToolsServerDbContext`-ში.
   - Infrastructure: რეპოზიტორი (`AsNoTracking`) და მისი DI.
   - Application: `GetEnvironments`, `GetEnvironmentByName`, `UpdateEnvironment` (upsert ვერსიით), `DeleteEnvironment`; ვალიდატორები (`RequiredWithMaxLength`).
   - WebApi: `EnvironmentsEndpoints`, ჩართული `UseSupportToolsServerApi`-ში.
   - Shared:
     - კონტრაქტი `StsEnvironmentDataModel { Name, Description, Version }`;
     - კლიენტის მეთოდები: `GetEnvironments`, `GetEnvironment`, `UpdateEnvironment` (`Result<int>` ან შერჩეული ტიპი), `DeleteEnvironment(key, version?)`.
   - წაშლის შემოწმება: ჯერჯერობით Environment-ს არავინ მიმართავს. B5 და B7 FK-ს `Restrict`-ით დაამატებს და ამ handler-ს 409-ს შეამოწმებინებს. სამომავლო გაფართოების ადგილი სუფთად დატოვე.

6. ტესტები ყველა ფენაში (README §7-ის წესი 7-ის ნიმუშები):
   - დომენი: ვერსიის ზრდა;
   - handler: ყველა შტო — შექმნა, განახლება, კონფლიქტი, 404, წაშლა ვერსიით და მის გარეშე;
   - ვალიდატორი;
   - რეპოზიტორი SQLite-ზე, concurrency-ის ჩათვლით;
   - endpoint-ები, route-ების სრული სია და Debug ტრეისები;
   - DI;
   - Shared კლიენტი (`StubHttpMessageHandler`);
   - არსებული git-ის handler-ები: ვერსიის ზრდა.

7. მიგრაცია `AddVersionAndEnvironments`: სამ არსებულ ცხრილს ემატება `Version int NOT NULL DEFAULT 1`, იქმნება ახალი ცხრილი `Environments`. იხელმძღვანელე README §7-ის წესი 10-ით.

8. CLAUDE.md-ში დაამატე სექცია „Registry conventions“: Version, upsert/delete სემანტიკა, route-ები, შეცდომები, ავთენტიფიკაცია, სახელების შედარება, ტესტების მინიმუმი და Environments როგორც შაბლონი. B2–B8 ამ სექციას მიჰყვება.

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა SupportToolsServer.slnx-ზე და Core-ის, DbPart-ისა და Shared-ის solution-ებზე;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები;
- CLAUDE.md განახლებულია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B1-ის სტატუსი განაახლე.
````
