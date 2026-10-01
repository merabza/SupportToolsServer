# A1 — DbTools FakeHost-ის გასწორება და მოძველებული დოკუმენტაცია

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportTools\SupportTools` |
| რეპოები | SupportToolsServerDbTools, SupportToolsServer, SupportToolsServerCore, SupportTools |
| დამოკიდებულია | — |
| ზომა | S |

## პრომპტი

````text
ეს არის ამოცანა A1 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§2, §7);
- რეპოს CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: მიგრაციების ხელსაწყო ისევ უნდა აკომპილირდეს, რომ შემდეგმა ამოცანებმა (B1…) ახალი მიგრაციები შექმნან. დოკუმენტაცია კოდს უნდა შეესაბამებოდეს.

სამუშაო:

1. FakeHost-ის გასწორება.
   - ფაილი: D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools\SupportToolsServerDbTools.FakeHost\SupportToolsServerDesignTimeDbContextFactory.cs.
   - პრობლემა: `new SupportToolsServerDbContext(options)` აღარ არსებობს. DbPart-ის commit 44c10b6-მა კონსტრუქტორები შეცვალა: ახლა არის `(options, bool isDesignTime)`, `(options, int)` და `(options, IDomainEventsDispatcher)`.
   - გამოსავალი: გამოიყენე დიზაინის დროის კონსტრუქტორი `(options, true)`. მას დომენის მოვლენების დისპეტჩერი არ სჭირდება.
   - ჯერ შეამოწმე, რომ DbTools workspace-ის SupportToolsServerCore, SupportToolsServerDbPart და SystemTools კლონები იმავე commit-ზეა, რაც D:\1WorkDotnet\SupportToolsServer\-ში (`git -C <path> log -1 --format=%H`). თუ არ არის, შეჩერდი და მომხმარებელს pull სთხოვე. ეს კლონები არ შეცვალო.
   - `dotnet build SupportToolsServerDbTools.slnx` მწვანე უნდა იყოს.

2. `dotnet ef`-ის შემოწმება scratch ასლში.
   - ასლი მოამზადე README §7-ის წესი 3-ითა და 10-ით: robocopy, მოკლე junction, FakeHost-ში ახალი UserSecretsId და dummy LocalDB ConnectionString.
   - გაუშვი (ორივეს ერთნაირი არგუმენტებით: `--project SupportToolsServerDbTools.DbMigration --startup-project SupportToolsServerDbTools.FakeHost`):
     - `dotnet ef migrations list --no-connect`
     - `dotnet ef migrations has-pending-model-changes`
   - მოსალოდნელი შედეგი: ერთი მიგრაცია `20260929190002_Initial` და pending ცვლილებები არ არის.
   - რეალურ რეპოში `dotnet ef` არ გაუშვა: ის მომხმარებლის user secrets-ს წაიკითხავდა.
   - ბოლოს junction წაშალე.

3. D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\CLAUDE.md გაასწორე. თითოეული ფაქტი ჯერ კოდში გადაამოწმე.
   - მიგრაციები: არსებობს მხოლოდ გაერთიანებული `20260929190002_Initial`, სამივე ცხრილით. წაშალე წინადადება „AddEditorConfigFileTypes is the latest after Initial“.
   - CLI-ის მომხმარებლები: `UploadGitProjectsToSupportToolsServerToolAction`, `SyncUpGitignoreFilesCliMenuCommand` და `SyncUpEditorConfigFilesCliMenuCommand` აღარ არსებობს. მათ ნაცვლად ჩაწერე:
     - `SupportTools\CliMenuCommands\SyncGitProjectsCliMenuCommand.cs`
     - `SupportTools\CliMenuCommands\GitIgnoreFileTypes\SyncGitignoreFilesCliMenuCommand.cs`
     - `SupportTools\CliMenuCommands\SyncEditorConfigFilesCliMenuCommand.cs`
     - `LibSupportToolsServerWork\Cruders\*StsCruder.cs`
   - GitRepo-ს განახლება: handler-ები ჩატვირთულ no-tracking ეგზემპლარს `GitRepo.Update(...)`-ით ცვლიან (ეს domain event-ს წარმოშობს) და იმავე ეგზემპლარს გადასცემენ რეპოზიტორის `Update`-ს. ნიმუში „ახალი ეგზემპლარი შენახული Id-ით“ ახლა მხოლოდ gitignore და editorconfig ტიპებს ეხება.
   - სერვერზე git clone/pull: `GitRepoAddedDomainEvent` / `GitRepoUpdatedDomainEvent` → `GitRepoSavedDomainEventHandler` → `UpdateGitProjectCommand` → რიგი (`GitProjectUpdateQueue`) → `GitProjectUpdateBackgroundService`. `AppOptions:WorkFolder` სავალდებულოა (`ValidateOnStart`). DI მეთოდია `AddSupportToolsServerGitProjects`.
   - DbTools workspace: მასში SupportToolsServer-ისა და SupportToolsServerShared-ის ძველი კლონებია, რომლებსაც მისი solution არ იყენებს. ეს სწორად აღწერე და მომხმარებელს ჰკითხე, წაშლის თუ არა.

4. D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore\README.md: Domain პროექტი `SystemTools.SharedKernel`-ს მიმართავს (commit 85acc60). გაასწორე წინადადება, რომლის მიხედვითაც Core-ს sibling რეპოებზე მითითება არ აქვს.

5. SupportTools-ის docs: D:\1WorkDotnet\SupportTools\SupportTools\docs\en\ და იგივე ფაილები docs\ka\-ში. ქართული ვერსია ინგლისურს უნდა შეესაბამებოდეს.
   - `configuration.md` (~117): ბრძანებები ახლა „Sync ... files...“ ჰქვია და Merge Up / Sync Up / Merge Down / Sync Down-ს სთავაზობს. ტექსტი დიფის ჩვენებითა და ორივე მიმართულებით აღწერე.
   - `configuration.md` (~165-166): „Support Tools Server Edit“-ში სინამდვილეში არის GitIgnore File Types, EditorConfig File Types და Gits from SupportToolsServer.
   - `architecture.md:27`: `LibSupportToolsServerWork` არის SupportToolsServer-ის რედაქტორები და სერვერის კლიენტი, არა „Remote server operations via WebAgent“.

არ გააკეთო:
- სხვა კოდის ცვლილება;
- DbTools workspace-ის Core/DbPart/SystemTools კლონების შეცვლა;
- SupportToolsServer რეპოს ignored obj-ფოლდერების წაშლა. თუ გინდა, მხოლოდ ჩამოთვალე და ჰკითხე.

დასრულების კრიტერიუმები:
- DbTools-ის solution აკომპილირდება;
- `migrations list --no-connect` და `has-pending-model-changes` scratch-ში წარმატებით გადის;
- ოთხივე დოკუმენტი განახლებულია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში A1-ის სტატუსი განაახლე.
````
