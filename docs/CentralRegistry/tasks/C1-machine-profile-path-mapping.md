# C1 — კომპიუტერის პროფილი და გზების გარდაქმნა

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools |
| დამოკიდებულია | A4 |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა C1 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G3/G6, §4.1, §4.4, §7);
- D:\1WorkDotnet\SupportTools\CLAUDE.md;
- D:\1WorkDotnet\SupportTools\SupportTools\CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი:
- სერვერზე გზები კანონიკური სახით ინახება: Windows-ის ფორმით, როგორც მთავარ კომპიუტერზე PAZISI (G3).
- SupportTools Linux კომპიუტერზეც ეშვება, ამიტომ სინქრონიზაციის ფენას სჭირდება კანონიკური ↔ ლოკალური გარდაქმნა.
- `ServerDataModel.IsLocal` კომპიუტერზეა დამოკიდებული (G6). დღეს ფაილში PAZISI-ც და Merinson-იც `IsLocal=true`-ა, რაც კომპიუტერებს შორის გაზიარებისას არასწორია.

სამუშაო:

1. კომპიუტერის ლოკალური პარამეტრები `SupportToolsParameters`-ში. ველები ზედა დონეზეა, არსებული სტილით. JSON უკუთავსებადი უნდა დარჩეს (Newtonsoft; ძველ ფაილში ეს ველები უბრალოდ არ არის).
   - `MachineName`: არასავალდებულოა, ნაგულისხმევად `Environment.MachineName`.
   - `CurrentMachineServerName`: `Servers`-ის ის ჩანაწერი, რომელიც ეს კომპიუტერია. null ნიშნავს, რომ ასეთი ჩანაწერი არ არის.
   - `PathMappings`: `List<PathMappingModel { CanonicalPrefix, LocalPrefix }>`.
   - მოდელი `SupportToolsData\Models\`-შია.

2. `PathMapper`: სუფთა, სატესტო კლასი, მაგალითად `LibSupportToolsServerWork\Registry\Paths\`-ში.
   - მეთოდები: `string? ToLocal(string? canonicalPath)` და `string? ToCanonical(string? localPath)`.
   - კანონიკური ფორმა Windows-ის აბსოლუტური გზაა (`\`). prefix-ები რეგისტრის გარეშე შედარდება.
   - რამდენიმე შესაფერის prefix-იდან იმარჯვებს ყველაზე გრძელი. დამთხვევა საზღვარზე უნდა მოდიოდეს: `D:\1WorkDotnet` არ ემთხვევა `D:\1WorkDotnetX`-ს. ბოლოში გამყოფი ნორმალიზდება (`d:\1WorkDotnet\` = `D:\1WorkDotnet`).
   - Windows-ის გარდა სხვა OS-ზე (`Path.DirectorySeparatorChar == '/'`): ToLocal დარჩენილ ნაწილში `\`-ს `/`-ით ცვლის, ToCanonical პირიქით აკეთებს.
   - თუ წესი არ ემთხვევა, გზა უცვლელი ბრუნდება და გაფრთხილება აღირიცხება. ამისთვის გამოიყენე, მაგალითად, `PathMappingIssue`-ების სია, რომელსაც სინქრონიზაციის ბრძანება (C5) აჩვენებს.
     - გამოსაჩენი შემთხვევები: Linux-ზე Windows-ის ფესვიანი გზა ToLocal-ისას, ან Linux-ის გზა ToCanonical-ისას, რომელიც არცერთ წესს არ ემთხვევა.
   - null და ცარიელი მნიშვნელობა უცვლელად გადის.
   - შეფარდებითი გზები (მაგ. `GitProjectFolderName`) prefix-ით არ გარდაიქმნება. დაამატე მეთოდი `NormalizeRelative`, რომელიც Linux-ზე მხოლოდ გამყოფებს ცვლის.

3. `IsLocal`-ის გამოთვლა.
   - პარამეტრების ჩატვირთვის შემდეგ, მენიუმდე და `--run`-მდე: თუ `CurrentMachineServerName` შევსებულია, `Servers[x].IsLocal = (x == CurrentMachineServerName)`, რეგისტრის გარეშე. თუ ცარიელია, ძველი მნიშვნელობები რჩება (უკუთავსებადობა).
   - ადგილი იპოვე `Program.cs` / `SupportToolsServices`-ში. გამოყავი პატარა კლასად, რომ დაიტესტოს.
   - მომხმარებელს ჰკითხე, რას ნიშნავს დღევანდელი Merinson `IsLocal=true` (ცალკე Linux კომპიუტერი? WSL?). პასუხი CLAUDE.md-ში ჩაწერე.

4. რედაქტორები „Support Tools Parameters Editor“-ში (`SupportTools\Menu\SupportToolsParametersEdit\SupportToolsParametersEditor.cs`):
   - `MachineName`;
   - `CurrentMachineServerName`: არჩევა `Servers`-იდან, არსებული `ServerDataNameFieldEditor`-ის მსგავსად, მაგრამ ჩანაწერის შექმნის გარეშე;
   - `PathMappings`: cruder, ველებით `CanonicalPrefix` და `LocalPrefix`.
   - დამხმარე ბრძანება „Suggest Path Mappings“:
     - პროექტების გზის ველებიდან, ServerInfo-ების appsettings გზებიდან და ProjectCreator-ის გზებიდან კრებს განსხვავებულ ფესვებს (დისკი + პირველი ფოლდერი, მაგ. `D:\1WorkDotnet`);
     - თითოზე ლოკალურ prefix-ს ეკითხება;
     - შედეგს `PathMappings`-ში ინახავს.
     - გზების ველების სია README §4.4-შია.
   - ინტერაქტიულ ბრძანებას შიდა კონსტრუქტორი უნდა ჰქონდეს input ფუნქციებით, ნიმუშის მიხედვით (`SaveGitIgnoreAsNewTemplateCliMenuCommand`).

5. კომპიუტერის ველების სია: README §4.1. ერთ ადგილას (მაგ. `Registry\MachineLocalFields.cs`-ის კომენტარში ან დოკუმენტაციაში) ჩაწერე, რომელი ველი არასოდეს სინქრონიზდება. C3/C4-ის mapper-ები ამას დაეყრდნობა.

6. Linux-თან თავსებადობა: სრული Linux მხარდაჭერა ამ ამოცანის საზღვრებს სცდება. თუ გზაში Windows-ზე მიბმულ კოდს წააწყდები (`Get-Command git`, Visual Studio-ს გამხსნელები, მყარად ჩაწერილი `d:\Logs` და ა.შ.), მხოლოდ ჩამოწერე ანგარიშში.

ტესტები (SupportTools.Tests):
- PathMapper: Windows identity (წესების გარეშე), სხვა დისკი, Linux (`D:\1WorkDotnet\X\Y.slnx` → `/home/u/1WorkDotnet/X/Y.slnx` და უკან), ყველაზე გრძელი prefix, საზღვარი, რეგისტრი, ბოლო გამყოფები, null/ცარიელი, დაუმთხვეველი გზის გაფრთხილება, `NormalizeRelative`;
- `IsLocal`-ის გამოთვლა;
- რედაქტორის დამხმარე ბრძანება შიდა კონსტრუქტორით.
OS-ზე დამოკიდებული ქცევისთვის PathMapper-ს separator პარამეტრად გადაეცი, რომ ორივე რეჟიმი Windows-ზე დაიტესტოს.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- SupportTools-ის CLAUDE.md-ში ახალი ველები და mapping-ის წესი აღწერილია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში C1-ის სტატუსი განაახლე.
````
