# D3 — „ამ კომპიუტერზეა“ და „ჩამოიტანე პროექტი აქ“

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools (სურვილისამებრ სერვერის რეპოებიც, თუ მომხმარებელი კომპიუტერების რეესტრს მოისურვებს) |
| დამოკიდებულია | C5 |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა D3 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§1, §2.1, §7);
- ორივე CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი:
- სინქრონიზაციის შემდეგ ყველა კომპიუტერზე ყველა პროექტი ჩანს, მაგრამ ფიზიკურად თითოეული მხოლოდ ზოგიერთ კომპიუტერზეა.
- დღეს „Sync All Projects All Gits V2“ ყველა რეგისტრირებულ პროექტს კლონავს.
- `SpaProjectName`-ის მქონე პროექტზე, რომლის `GitProjects` ჩანაწერი აკლია, V2 სინქრონიზაცია ჩერდება: `GitSyncParameters.cs:58-59`, `SyncOneProjectAllGitsParameters.cs:72-73` და `WrongGitignoreFilesListCreator.cs:63-66` (`GitProjects.GetGitProjectByKey` ისვრის exception-ს).

სამუშაო:

1. „ამ კომპიუტერზეა“ ნიშნავს, რომ `ProjectFolderName` არსებობს (ლოკალური გზა, pull-ის შემდეგ უკვე გარდაქმნილი).
   - პროექტების სიებში და ჯგუფის სტატუსებში გამოჩნდეს ნიშანი, მაგ. `[local]` / `[remote]`.
   - ადგილები: `Menu\ProjectGroupsList\ProjectGroupsListFactoryStrategy.cs`, `ProjectGroupSubMenuCliMenuCommand`, `ProjectsList\ProjectsListFactoryStrategy.cs`, `ProjectSubMenuCliMenuCommand`.
   - ლოკალური პარამეტრი „Hide remote-only projects“ (default false).
   - შემოწმება ყოველ render-ზე ხდება, ამიტომ იაფი უნდა იყოს: მხოლოდ `Directory.Exists`, მოკლევადიანი ქეშით, თუ საჭიროა.

2. ბრძანება „Clone Project To This Computer“ პროექტის მენიუში, მხოლოდ `[remote]` პროექტებისთვის:
   - (ა) `ProjectFolderName`-ის შექმნა;
   - (ბ) პროექტის ყველა git-ის კლონი (`GitProjectNames`; ScaffoldSeeder-ის git-ები ცალკე კითხვით) არსებული V2 მექანიზმით: `GitProjectSyncronizer` / `SyncOneProjectAllGitsToolAction`;
   - (გ) თუ `GitProjects`-ში პროექტის csproj/esproj ჩანაწერები აკლია, ამ git-ებისთვის „Update Git Projects“-ის გაშვება (ან B9-ის სერვერული ვარიანტი, თუ არსებობს). ამით `SpaProjectName`-ის exception-ი თავიდან აიცილება;
   - (დ) შაბლონების ფაილების შემოწმება;
   - (ე) საიდუმლო ფაილების (C6) არსებობის შემოწმება. თუ აკლია, შეთავაზება: „Sync Registry“.
   - შედეგის ანგარიში.

3. ჯგუფური ბრძანებები ნაგულისხმევად მხოლოდ `[local]` პროექტებზე მუშაობს, ცხადი ოფციით „ყველა“:
   - Sync All/Group V2, CheckAllProjectsBuild, ClearAll, DistributeAll, ReversePackageDistribution, RemoveUnusedPackageVersions, UpdateOutdatedPackages, OpenAllProjectsByVisualStudio;
   - ყოველ ბრძანებაში შეამოწმე, აზრი აქვს თუ არა ფილტრს;
   - არსებულ ქცევაში ცვლილება მომხმარებელს ჯერ აჩვენე.

4. სურვილისამებრ: კომპიუტერების რეესტრი სერვერზე, ანუ რომელი პროექტი რომელ კომპიუტერზეა. დაწყებამდე მომხმარებელს ჰკითხე. თუ კი, ეს ცალკე, პატარა სერვერის ნაკვეთია B1-ის კონვენციებით (`Machines`, `MachineProjects`) და კლიენტი გაშვებისას თავის სიას აგზავნის. თუ არა, გამოტოვე.

5. V2 სინქრონიზაციის exception-ი (`SpaProjectFolderRelativePath`, `GitProjects.GetGitProjectByKey`): ამ ადგილებში ნათელი შეცდომა დააბრუნე exception-ის ნაცვლად, მაგ. „GitProjects-ში X არ არის — გაუშვი Update Git Projects“.

ტესტები (SupportTools.Tests):
- ნიშნის გამოთვლა (დროებითი ფოლდერი);
- ფილტრი;
- ბრძანების ნაბიჯები fake-ებით (git-ის რეალური გაშვების გარეშე, ან `%TEMP%`-ის ლოკალური bare რეპოებით, როგორც არსებულ git ტესტებშია);
- exception-ის ნაცვლად შეცდომა.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- CLAUDE.md და docs-ის შესაბამისი ადგილი განახლებულია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში D3-ის სტატუსი განაახლე.
````
