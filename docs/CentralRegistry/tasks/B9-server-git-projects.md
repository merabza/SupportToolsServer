# B9 — (სურვილისამებრ) `GitProjects`-ის გამოთვლა სერვერზე

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools\SupportTools` |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools, SupportTools (კლიენტის ოფცია) |
| დამოკიდებულია | B1; აზრი აქვს C5-ის შემდეგ |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა B9 (სურვილისამებრ) SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G9, §7);
- ორივე რეპოს CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი:
- კლიენტის `GitProjects` 268 ჩანაწერია. თითოეული csproj/esproj ფაილს აღწერს გიტ-რეპოს შიგნით: `GitName`, `ProjectRelativePath`, `ProjectFileName`, `DependsOnProjectNames`.
- მათ კლიენტის „Update Git Projects“ ითვლის (`LibGitWork\ToolActions\UpdateGitProjectsToolAction.cs`, `GitProjectsUpdater.cs`): ყველა git-ს `{WorkFolder}\Gits`-ში კლონავს და ფაილებს სკანირებს. ახალ კომპიუტერზე ეს ნელია, მაგრამ აუცილებელი: `SpaProjectName`-ის მქონე პროექტის სინქრონიზაცია მის გარეშე ჩერდება.
- 2026-10-01-დან სერვერი თვითონ კლონავს ყველა GitRepo-ს `{AppOptions:WorkFolder}\Gits`-ში (`UpdateGitProjectCommandHandler`).

მიზანი: სერვერმა თავისი კლონებიდან თვითონ გამოითვალოს `GitProjects` და გასცეს ისინი. კლიენტს ეს ოფციად უნდა ჰქონდეს „Update Git Projects“-ში.

სამუშაო:

1. დაწყებამდე ზუსტად გაიგე და მომხმარებელს აუხსენი კლიენტის ალგორითმი. სერვერმა ზუსტად იგივე შედეგი უნდა მისცეს:
   - `ProjectRelativePath`-ის ბაზა `{WorkFolder}\Gits`-ია;
   - ჩადგმული ფოლდერის სახელი ბრტყელდება (`A\B` → `A.B`, `GitFolderCountHelper.cs:23-24`);
   - `{SpaProjectFolderRelativePath}` წინსართისას git-ის სახელი გამოიყენება;
   - სკანირდება `*.csproj` და `*.esproj`;
   - `DependsOnProjectNames` `<ProjectReference>`-ებიდან იკრიბება;
   - `GitProjects`-ის გასაღები csproj-ის სახელია გლობალურად; ეს ცნობილი შეჯახების პრობლემაა (README §9).
   - შეჯახების პრობლემა გადაწყვიტე სერვერზე და კლიენტისთვის თავსებადი ფორმა შეინარჩუნე, ან შეინარჩუნე კლიენტის ქცევა. აირჩიე მომხმარებელთან ერთად.

2. სერვერზე `UpdateGitProjectCommandHandler`-ის წარმატებული clone/pull-ის შემდეგ ამ რეპოს ფაილები სკანირდება და ინახება ცხრილში.
   - მაგალითად `GitRepoProjects`: GitRepoId FK cascade, `ProjectRelativePath`, `ProjectFileName`, `DependsOn`. `DependsOn` ცალკე შვილ ცხრილში ან სერიალიზებულად (G1-ის სულისკვეთებით, ცხრილი სჯობს).
   - XML-ის წაკითხვა უსაფრთხოდ, DTD-ის გარეშე.

3. endpoint `GET gitprojects` აბრუნებს კლიენტის ფორმის მონაცემებს: `GitName`, `ProjectRelativePath`, `ProjectFileName`, `DependsOnProjectNames`. კონტრაქტი და კლიენტის მეთოდი Shared-ში.

4. SupportTools-ში „Update Git Projects“-ის მენიუს დაემატება ოფცია „From SupportToolsServer“. ის `GitProjects`-ს სერვერიდან ჩამოტვირთავს და ლოკალურ კლონირებას აღარ აკეთებს. ძველი გზა რჩება.

5. ტესტები:
   - სკანერი: დროებითი ფოლდერი, რამდენიმე csproj, ჩადგმული ფოლდერი და SPA შემთხვევა;
   - handler;
   - endpoint;
   - კლიენტის mapping.

მიგრაცია: `AddGitRepoProjects` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- სერვერის მონაცემები თანხვდება კლიენტის „Update Git Projects“-ის შედეგს. შეადარე მთავარ კომპიუტერზე: ჩამოტვირთული სია ლოკალურ 268 ჩანაწერს. `GitProjects`-ში საიდუმლო არ არის, ამიტომ მისი სექციის წაკითხვა შეიძლება;
- ყველა build და test მწვანეა.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B9-ის სტატუსი განაახლე.
````
