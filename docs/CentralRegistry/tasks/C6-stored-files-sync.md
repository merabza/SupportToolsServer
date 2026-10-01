# C6 — საიდუმლო ფაილების სინქრონიზაცია

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportTools\SupportToolsServerShared` (მხოლოდ წასაკითხად), `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools |
| დამოკიდებულია | C5, B8 (და SupportToolsServerShared კლონის pull) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა C6 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G2/G3, §4.1, §4.4, §7);
- ორივე CLAUDE.md;
- C2–C5-ის კოდი;
- B8-ის კონტრაქტები (`StsStoredFileInfoDataModel`, `StsStoredFileDataModel`) და კლიენტის მეთოდები.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

წინაპირობა: D:\1WorkDotnet\SupportTools\SupportToolsServerShared კლონი სერვერის workspace-ის კლონთან ერთ commit-ზე უნდა იყოს.

მიზანი: C2-ის ადაპტერი `StoredFiles` კოლექციისთვის. საიდუმლო ფაილები, რომლებზეც რეესტრი მიუთითებს, ყველა კომპიუტერზე უნდა გაჩნდეს. G2-ის მიხედვით ისინი სერვერზე ღიად ინახება.

სამუშაო:

1. რომელი ფაილები სინქრონიზდება. ნაგულისხმევად მხოლოდ ის, რაზეც რეესტრი მიუთითებს:
   - ყველა `ServerInfo.AppSettingsJsonSourceFileName` (მთავარი შემთხვევა: `D:\1WorkSecurity\<Project>\<server>\...\appsettings.json`);
   - `ProjectModel.SeedProjectParametersFilePath`;
   - `ProjectModel.PrepareProdCopyDatabaseProjectParametersFilePath`;
   - `ProjectModel.PairedDbObjectsResultFileName`.
   დაწყებამდე მომხმარებელს ჰკითხე, დაემატოს თუ არა სხვა ფაილები ან მთელი `SecurityFolder`. `SecurityFolder`-ის შემთხვევაში გამონაკლისები: პარამეტრების ფაილი, `*.bak`, ლოგები, `GitIgnoreFiles` და `EditorConfigFiles`, რადგან ესენი C3-მა უკვე გაატარა. AppSettingsEncoder-ის გენერირებული `appsettingsEncoded.json` ნაგულისხმევად არ შედის.

2. გასაღები კანონიკური გზაა: `PathMapper.ToCanonical`. Pull-ისას ფაილი იწერება `PathMapper.ToLocal(Path)`-ზე და საჭირო ფოლდერები იქმნება.

3. ჰეში SHA-256-ია ფაილის შიგთავსზე (UTF-8 ტექსტი). სერვერის `Sha256` იმავე წესით უნდა ითვლებოდეს; გადაამოწმე B8-თან.
   - ზომის ზღვარი B8-ის ვალიდატორის მიხედვითაა.
   - ბინარული ან ზღვარზე დიდი ფაილი გამოირიცხება გაფრთხილებით.

4. სერვერის სია `GET files`-ით მოდის, შიგთავსის გარეშე. შიგთავსი მხოლოდ საჭირო ჩანაწერებისთვის მოითხოვება (`GET files/content`).

5. წაშლა:
   - ლოკალური ფაილი არასოდეს იშლება ავტომატურად; Pull(Delete) დასტურს ითხოვს;
   - სერვერიდან წაშლა მხოლოდ მაშინ ხდება, როცა რეესტრი ფაილზე აღარ მიუთითებს და მომხმარებელი დაადასტურებს.

6. შიგთავსი არასოდეს იბეჭდება: არც დიფში, არც ლოგში. დიფში მხოლოდ ზომა და ჰეშის დასაწყისი ჩანს.

7. ადაპტერი ფაბრიკაშია ბოლოსწინა ან ბოლო `Order`-ით. C5-ის ბრძანება მას ავტომატურად გამოიყენებს.

ტესტები (SupportTools.Tests, დროებითი ფოლდერები):
- კანდიდატი ფაილების აკრეფა;
- Linux-ის mapping (ჩაწერის გზა);
- ზომის და ბინარული ფაილის გამორიცხვა;
- Pull-ის ჩაწერა ფოლდერის შექმნით;
- წაშლის დასტური;
- შიგთავსის დაფარვა გამონატანში.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- C5-ის ბრძანება StoredFiles-საც აჩვენებს.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში C6-ის სტატუსი განაახლე.
````
