# D4 — ძველი ბრძანებების მოწესრიგება, მკვდარი კოდი, დოკუმენტაცია

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared` |
| რეპოები | SupportTools, SupportToolsServerShared, SupportToolsServer (docs) |
| დამოკიდებულია | C6, D2, D3 |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა D4, SupportTools-ის ცენტრალიზებული რეესტრის გეგმის ბოლო ამოცანა. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (მთლიანად);
- ორივე CLAUDE.md;
- რეესტრის კოდი (`LibSupportToolsServerWork\Registry\`).
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: ძველი, პარალელური გზების მოწესრიგება და დოკუმენტაციის დასრულება.

სამუშაო:

1. დაწყებამდე მომხმარებელს ჰკითხე (AskUserQuestion):
   - (ა) „Support Tools Server Editor“ (`GitStsCruder`, `GitIgnoreFileTypesStsCruder`, `EditorConfigFileTypesStsCruder` და მათი CliMenuCommand-ები): წაიშალოს, თუ დარჩეს სერვერის პირდაპირი დათვალიერებისა და რედაქტირების ხელსაწყოდ?
   - (ბ) „Sync Git Projects With SupportToolsServer...“, „Sync .gitignore files...“, „Sync .editorconfig files...“: წაიშალოს, თუ გადაკეთდეს ძრავზე, ერთ კოლექციაზე გაფილტრული „Sync Registry“-ის მალსახმობად?
   - რეკომენდაცია: (ა) დარჩეს, თუ მომხმარებელი იყენებს, ოღონდ `Version`-ის გათვალისწინებით; (ბ) გადაკეთდეს მალსახმობად, რომ სემანტიკა ერთი იყოს.

2. არჩეულის განხორციელება:
   - თუ ძველი ბრძანება რჩება, გაასწორე ცნობილი პრობლემები. მაგალითად, Git Sync Up ჯერ შლის და მერე ტვირთავს, ამიტომ წარუმატებელი ატვირთვა წაშლას ტოვებს (`SyncGitProjectsCliMenuCommand.cs:196-205`).
   - ასევე sync-over-async `.Result`-ის შემთხვევები, სადაც ეს შესაძლებელია.

3. მკვდარი კოდის წაშლა. თითოეული grep-ით გადაამოწმე, რომ არავინ იყენებს:
   - `LibSupportToolsServerWork\SupportToolsServerWork.cs` (`GetGitRepos`);
   - `SupportToolsServerApiClient.GetGitIgnoreFileNames` (Shared რეპო): გაუმართავია, `List<string>`-ს ელოდება. წაშალე ისიც და მისი ტესტიც, რომელიც შეცდომას მალავს;
   - `GetGitRepoByKey`, თუ არავინ იყენებს;
   - `CheckAndGenerateGuidKeysGitignoreFilesCliMenuCommand`;
   - მთლიანად კომენტარში მოქცეული ფაილები: `GitsFieldEditor.cs`, `UpdateGitProjectsParameters.cs`, `IParametersWithGits.cs`, `GitDataModelRem.cs`;
   - მკვდარი npm ბრძანებები: `AddAllPossibleNpmPackageNamesFromStpToProjectCliMenuCommand`, `NewFrontNpmPackageNameCliMenuCommand`, `NpmPackageInProjectSubMenuCliMenuCommand`, `DeleteNpmPackageFromProjectCliMenuCommand`. ესენი ჯერ მომხმარებელს აჩვენე: შეიძლება ჩართვა სურდეს და არა წაშლა.

4. დოკუმენტაცია:
   - SupportTools-ის `docs\en\` და `docs\ka\`:
     - `configuration.md`: საერთო და კომპიუტერის ველები; `PathMappings`; `CurrentMachineServerName`; `RegistrySyncState`; ავტოსინქრონიზაცია; რა ინახება სერვერზე ღიად (G2);
     - `getting-started.md`: ახალი კომპიუტერი = ფოლდერები + `PathMappings` + bootstrap ApiClient + Sync Registry + Clone Project To This Computer;
     - `architecture.md`: Registry ფენა;
     - `use-cases\git-operations.md`: შესაბამისი ცვლილებები.
     - ქართული ვერსია ინგლისურს უნდა შეესაბამებოდეს.
   - ორივე CLAUDE.md და SupportToolsServer-ის CLAUDE.md: საბოლოო სურათი.
   - README.md-ის (გეგმის) §2-ში დაამატე „შედეგი“ ქვესექცია: რა გაკეთდა, რა დარჩა ღიად. §5-ის სტატუსები გადაამოწმე.

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა SupportTools.slnx-ზე და SupportToolsServerShared.slnx-ზე;
- დოკუმენტაცია კოდს შეესაბამება.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში D4-ის სტატუსი განაახლე.
````
