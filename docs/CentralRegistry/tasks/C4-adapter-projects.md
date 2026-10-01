# C4 — ადაპტერი: Projects და ServerInfo-ები

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportTools\SupportToolsServerShared` (მხოლოდ წასაკითხად), `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools |
| დამოკიდებულია | C3, B6, B7 (და SupportToolsServerShared კლონის pull) |
| ზომა | L |

## პრომპტი

````text
ეს არის ამოცანა C4 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G3/G7/G8, §4.3, §4.4, §7);
- ორივე CLAUDE.md;
- C1, C2 და C3-ის კოდი;
- სერვერის კონტრაქტი `StsProjectDataModel`, მისი ServerInfo-ებით (B6/B7).
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

წინაპირობა: D:\1WorkDotnet\SupportTools\SupportToolsServerShared კლონი იმავე commit-ზე უნდა იყოს, რაც სერვერის workspace-ში. თუ არ არის, შეჩერდი და მომხმარებელს pull სთხოვე.

მიზანი: Projects-ის ადაპტერი, ServerInfo-ებითურთ, ერთ აგრეგატად (G7). ის ფაბრიკაში ბოლო `Order`-ით ემატება.

mapping. წყარო: `ProjectModel.cs` და `ServerInfoModel.cs` (SupportToolsData\Models\).
- პროექტის სახელი dictionary-ის key-ა, ის კონტრაქტში `Name` ხდება.
- გზების ველები (README §4.4) PathMapper-ით გარდაიქმნება ორივე მიმართულებით:
  - პროექტის 15 ველი;
  - ServerInfo-ს `AppSettingsJsonSourceFileName` და `AppSettingsEncodedJsonFileName`.
- `KeyGuidPart` მიდის (G2) და არსად იბეჭდება.
- `EProjectType`, `EProjectTools`, `EProjectServerTools` სახელებად გადაიქცევა. სერვერიდან მოსული უცნობი სახელი (მაგ. ახალი ხელსაწყო, რომელიც ძველ კლიენტს არ აქვს) არ უნდა დაიკარგოს ან ჩავარდეს:
  - გაფრთხილება;
  - ჩანაწერი ავტომატურად აღარ გაიგზავნოს Push-ით, სანამ კლიენტს არ განაახლებ, რომ სერვერის მონაცემი არ წაიშალოს;
  - აირჩიე და დაასაბუთე.
- `RouteClasses` (კლიენტის `RouteClassModel.Version`) ↔ კონტრაქტის ის ველი, რომელიც B6-მა შეარჩია (მაგ. `ApiVersion`).
- `Endpoints`, `RouteClasses`, `RedundantFileNames`, `FrontNpmPackageNames`, `GitProjectNames`, `ScaffoldSeederGitProjectNames`, `AllowToolsList`: სიმრავლის სიები ჰეშისთვის ლაგდება.
  - `ScaffoldSeederGitProjectNames`-ს ScaffoldSeederCreator ავსებს git-ის ფოლდერის სახელებით, მაგრამ Gits-ის key-ებად იკითხება (README §9). სინქრონიზაცია მათ ისე ატარებს, როგორც არის.
- `DevDatabaseParameters` და `ProdCopyDatabaseParameters` ↔ `StsDatabaseParametersDataModel`. იგივე ServerInfo-ს `CurrentDatabaseParameters`-ისა და `NewDatabaseParameters`-ისთვის.
- ServerInfo-ები:
  - ნატურალური გასაღებია (ServerName, EnvironmentName).
  - `ApplyLocal`-ის დროს არსებული ლოკალური dictionary-ის key (GUID ან `"Server|Env"`) ნატურალური გასაღებით უნდა მოიძებნოს და შენარჩუნდეს.
  - ახალი ServerInfo იღებს `"{ServerName}|{EnvironmentName}"` key-ს (`ServerInfoModel.GetItemKey`-ის ფორმა).
  - თუ ორი ლოკალური ServerInfo-ს ერთი და იგივე ნატურალური გასაღები აქვს, ეს ლოკალური შეცდომაა. გაფრთხილება და პროექტის Push-ის შეჩერება.
- `ApplyLocal` არსებულ `ProjectModel` ობიექტს ადგილზე ანახლებს, თუ ეს შესაძლებელია; თუ არა, dictionary-ში ცვლის.
  - `init`-only თვისებებს შეცვლა მხოლოდ ახალი ობიექტით შეუძლია. გადაწყვიტე, როგორ მოიქცე, ისე რომ ღია რედაქტორებმა არ დაკარგონ მონაცემი. ჩვეულებრივ pull მენიუს ხელახალ აგებამდე ხდება (C5-ში reload), ასე რომ ეს უსაფრთხოა.
  - ლოკალური ველები, რომლებიც კონტრაქტში არ არის (§4.6: მომავალში დამატებული, ჯერ ლოკალური ველები), უცვლელი რჩება.

ვალიდაცია Push-მდე. ეს გაფრთხილებებია, რომლებსაც C5 აჩვენებს:
- პროექტი მიმართავს git-ს, npm-ს, DB კავშირს ან EditorConfig შაბლონს, რომელიც არც ლოკალურად არის და არც სერვერზე;
- გზა ვერ გარდაიქმნა კანონიკურად (PathMapper-ის issue);
- გასაღებები ერთმანეთისგან მხოლოდ რეგისტრით განსხვავდება.

ტესტები (SupportTools.Tests):
- სრული round-trip რეალისტური პროექტით: ყველა ველი, ორი ServerInfo, DB პარამეტრები, endpoint-ები. ყველა მნიშვნელობა გამოგონილია;
- Linux-ის mapping-ის round-trip: Windows-ის კანონიკური → Linux ლოკალური → კანონიკური = საწყისი;
- ServerInfo-ს key-ის შენარჩუნება და ახალი key-ის ფორმა;
- უცნობი enum სახელის ქცევა;
- ჰეშის სტაბილურობა;
- ლოკალური, კონტრაქტში არარსებული ველის შენარჩუნება;
- დუბლირებული ნატურალური გასაღების გაფრთხილება.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- Projects-ის ადაპტერი ფაბრიკაშია.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში C4-ის სტატუსი განაახლე.
````
