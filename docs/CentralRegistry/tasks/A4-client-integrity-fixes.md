# A4 — კლიენტის შეცდომები, რომლებიც სინქრონიზაციას გააფუჭებს

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportTools\AppCliTools`, `D:\1WorkDotnet\SupportTools\ParametersManagement`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools, AppCliTools, ParametersManagement |
| დამოკიდებულია | — |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა A4 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§2.1, §4.3, §7);
- D:\1WorkDotnet\SupportTools\CLAUDE.md;
- D:\1WorkDotnet\SupportTools\SupportTools\CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი: სინქრონიზაციის ძრავა (C2–C5, D2) ეყრდნობა იმას, რომ ყოველი ცვლილება საიმედოდ ინახება `IParametersManager.Save`-ით და პარამეტრების ფაილი არ ზიანდება. ქვემოთ ჩამოთვლილი შეცდომები ამ დაშვებას არღვევს. თითოეული გაასწორე და ტესტით დაფარე.

1. მონაცემების დაკარგვა.
   - `SupportTools\CliMenuCommands\DeleteTemplateCliMenuCommand.cs:28,48`: `_parametersManager.Save(...)`-ს `AppProjectCreatorAllParameters` გადაეცემა.
   - შედეგად მთავარი JSON ფაილი მხოლოდ ამ ობიექტით გადაიწერება და `IParametersManager.Parameters` იცვლება, რის გამოც შემდეგი cast-ები ჩავარდება.
   - გაასწორე ისე, რომ ძირეული `SupportToolsParameters` შეინახოს.

2. SimpleNames cruder-ები ცვლილებას არასოდეს ინახავს.
   - კლასები: `AppCliTools\AppCliTools.CliParameters\Cruders\SimpleNamesListCruder.cs`, `SimpleNamesWithDescriptionsCruder.cs` და `AppCliTools\AppCliTools.CliParametersEdit\Cruders\SmartSchemaDetailCruder.cs`.
   - ისინი `Save`-ს არ აფარავს, `Cruder.Save` კი no-op-ია.
   - ამიტომ შემდეგი ცვლილებები მხოლოდ სხვა, შემთხვევითი შენახვისას ინახება: Environments, RunTimes, ReactAppTemplates, NpmPackages, GitIgnorePatterns, EditorConfigPatterns, FrontNpmPackageNames, RedundantFileNames და SmartSchema-ს დეტალები.
   - გაასწორე ისე, რომ ცვლილება ძირეული ობიექტის `IParametersManager`-ით შენახვით დასრულდეს. ნახე, როგორ ქმნიან ამ კლასებს მემკვიდრეები SupportTools-ში და როგორ ინახავს `ParCruder`.
   - ერთი ოპერაცია ფაილს ისედაც 2–3-ჯერ წერს, ამიტომ ახალი ზედმეტი ჩაწერა არ დაამატო.
   - ტესტები AppCliTools-ის სატესტო პროექტებში.

3. `LibSupportToolsServerWork\Cruders\GitStsCruder.cs:223-225`.
   - სერვერიდან წაშლისას ლოკალურ `parameters.Gits[key]`-საც შლის, გამოყენების შემოწმებისა და შენახვის გარეშე.
   - გადარქმევა Remove + Add-ია, ამიტომ ლოკალური git ძველი სახელით იკარგება, პროექტები კი ისევ მას მიმართავს.
   - რეკომენდაცია: სერვერის რედაქტორი მხოლოდ სერვერს უნდა ცვლიდეს; ლოკალური მონაცემი სინქრონიზაციის საქმეა.
   - ჯერ მომხმარებლისგან დასტური მიიღე, მერე გაასწორე. ტესტი `SupportTools.Tests\Cruders\GitStsCruderTests.cs`-ში.

4. `ParametersManagement\ParametersManagement.LibParameters\ParametersManager.cs`, `Save` (28-77).
   - ჩაწერე ატომურად: დროებითი ფაილი იმავე ფოლდერში, შემდეგ `File.Move(tmp, path, overwrite: true)` ან `File.Replace` backup-ით.
   - გააკეთე კომენტარებში (53-56, 67) აღწერილი, მაგრამ განუხორციელებელი `.bak`: მინიმუმ ერთი ბოლო ვერსია. თუ მეტ ვერსიას შეინახავ, ძველები გაასუფთავე.
   - ფაილის ფორმატი (Newtonsoft, `Formatting.Indented`) არ შეცვალო.
   - ParametersManagement-ს სატესტო პროექტი არ აქვს. ჰკითხე მომხმარებელს, შეიქმნას თუ არა `ParametersManagement.LibParameters.Tests` (AppCliTools-ის სატესტო პროექტების მსგავსად, solution-ში ჩართული).

5. DotnetTools.
   - `SupportTools\Cruders\DotnetToolCruder.cs`: `BeforeGetListMenu` (27-30) მონაცემს ცვლის შენახვის გარეშე.
   - `UpdateAllToolsToLatestVersionCliMenuCommand` და `UpdateOneToolToLatestVersionCliMenuCommand` ცვლილებას არ ინახავს.
   - აირჩიე ორი გამოსავლიდან უფრო მარტივი და დაასაბუთე: ან ცვლილება ყოველთვის შეინახოს, ან სიის აგებისას მონაცემი აღარ შეიცვალოს.

6. ApiKey ეკრანზე ჩანს. G2-ის მიხედვით საიდუმლოებები სერვერზეც წავა, ამიტომ ეკრანზე მათი გამოჩენა განსაკუთრებით არასასურველია.
   - `ParametersManagement\ParametersManagement.LibApiClientParameters\ApiClientSettings.cs`: `GetItemKey` გასაღებსაც აბრუნებს და ის მენიუს სათაურებში ჩანს. დააბრუნოს მხოლოდ `Server`.
   - `AppCliTools\AppCliTools.CliParametersApiClientsEdit\ApiClientCruder.cs:30`: ApiKey ღია `TextFieldEditor`-ით რედაქტირდება. შეცვალე პაროლების მსგავსი დაფარული რედაქტორით (ნახე, როგორ რედაქტირდება `ServerPass`).

7. `LibGitWork\GitApi.cs:24`: `IsGitRemoteAddressValid` მისამართს git-ის არგუმენტებში ბრჭყალების გარეშე სვამს. გადაიყვანე `ProcessStartInfo.ArgumentList`-ზე და `--`-ზე, როგორც A2 სერვერზე. თუ `GitApi`-ში სხვა ბრძანებებიც ასეა აწყობილი, ესენიც გაასწორე.

არ გააკეთო:
- სინქრონიზაციის ფუნქციონალი (ეს C ფაზაა);
- საზიარო ბიბლიოთეკების საჯარო API-ის ცვლილება უკუთავსებადობის გარეშე. AppCliTools და ParametersManagement სხვა აპლიკაციებსაც ემსახურება; თუ ხელმოწერა უნდა შეიცვალოს, ჯერ ჰკითხე.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` (ის თითქმის მთელ workspace-ს აკომპილირებს) მწვანეა;
- `dotnet test` SupportTools.slnx-ზე, AppCliTools.slnx-ზე და, თუ შეიქმნა, ParametersManagement-ის ტესტებზე მწვანეა.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში A4-ის სტატუსი განაახლე.
````
