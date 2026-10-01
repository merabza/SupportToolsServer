# B2 — ცნობარები: Runtimes, NpmPackages, ReactAppTemplates, DotnetTools

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools`, `D:\1WorkDotnet\SupportTools\SupportTools` (მხოლოდ წასაკითხად) |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B1 |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა B2 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3, §4.1, §4.2, §7);
- რეპოს CLAUDE.md, განსაკუთრებით B1-ის მიერ დამატებული სექცია „Registry conventions“;
- Environments-ის ნაკვეთის მთელი კოდი (B1). ის შაბლონია.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: სერვერზე ოთხი ცნობარის აგრეგატი, ზუსტად Environments-ის ნიმუშით. თითოეულს სჭირდება:
- Domain entity: typed Id, `Version`, სიგრძის კონსტანტები, ფაბრიკა და განახლების მეთოდი;
- რეპოზიტორის ინტერფეისი და EF რეპოზიტორი;
- configuration და DbSet ორივეგან;
- Application-ში list, get, update (ვერსიით) და delete, ვალიდატორებით;
- endpoint-ების group (`.RequireAuthorization()`) და route-ები Shared-ში;
- კონტრაქტი და კლიენტის მეთოდები;
- ტესტები ყველა ფენაში.

კლიენტის მოდელებს წაიკითხავ D:\1WorkDotnet\SupportTools\SupportTools\SupportToolsData\Models\-ში: `SupportToolsParameters.cs` და `DotnetToolData.cs`.

აგრეგატები:

1. `Runtime`. წყარო: `RunTimes: Dictionary<string,string>`, ანუ RID → აღწერა (მაგ. `win-x64`, `linux-x64`). ველები: `Name`, `Description` (არასავალდებულო).

2. `NpmPackage`. წყარო: `NpmPackages: Dictionary<string,string>`, ანუ სახელი → აღწერა. ველები: `Name`, `Description` (არასავალდებულო). npm-ის სახელის მაქსიმალური სიგრძე 214 სიმბოლოა.

3. `ReactAppTemplate`. წყარო: `ReactAppTemplates: Dictionary<string,string>`, ანუ სახელი → create-react-app-ის `--template` მნიშვნელობა (`ReCreateReactAppFiles.cs:51`). ველები: `Name`, `Template`.

4. `DotnetTool`. წყარო: `DotnetTools: Dictionary<string, DotnetToolData>`.
   - სერვერზე მიდის მხოლოდ საერთო ველები: `Name` (dictionary-ის key), `PackageId`, `MaxVersion` და `Description`.
   - `InstalledVersion`, `LatestVersion` და `CommandName` კომპიუტერისაა ან გამოთვლადია (G9), ამიტომ სერვერზე არ მიდის.
   - ჯერ გაარკვიე, რა არის dictionary-ის key კლიენტში. `DotnetToolData.GetItemKey()` `Description`-ს აბრუნებს; ნახე `DotnetToolCruder` და `DotnetToolsVersionsCheckerUpdater`. თუ key და Description ერთმანეთს ემთხვევა, მომხმარებელს ჰკითხე, როგორ შევინახოთ.

სიგრძეები:
- შეარჩიე გონივრული მაქსიმალური სიგრძეები და გადაამოწმე რეალურ მონაცემებზე.
- D:\1WorkSecurity\SupportTools\SupportTools.json-ში შეგიძლია წაიკითხო მხოლოდ სექციები `RunTimes`, `NpmPackages`, `ReactAppTemplates` და `DotnetTools`. ისინი საიდუმლოს არ შეიცავს. სექციის საზღვრები grep-ით იპოვე (`^  "RunTimes"` და ა.შ.). ფაილის სხვა ნაწილს არ კითხულობ.

წაშლის შემოწმება:
- ჯერჯერობით ამ ჩანაწერებს არავინ მიმართავს.
- მომავალში მიმართავენ:
  - Runtime-ს — Server (B4);
  - NpmPackage-ს — Project (B6);
  - ReactAppTemplate-ს — ProjectTemplate (B5).
- ეს ამოცანები FK-ს `Restrict`-ით დაამატებენ და აქაურ delete handler-ს 409 `RecordIsInUse`-ს შეამოწმებინებენ. გაფართოების ადგილი სუფთად დატოვე, მაგალითად B1-ში არჩეული ნიმუშით.

მიგრაცია: `AddLookups` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა SupportToolsServer.slnx-ზე და Core-ის, DbPart-ისა და Shared-ის solution-ებზე;
- route-ების სრული სიის ტესტი განახლებულია;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B2-ის სტატუსი განაახლე.
````
