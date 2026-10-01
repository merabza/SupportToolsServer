# B8 — საიდუმლო ფაილების საცავი

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerCore`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerDbPart`, `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared`, `D:\1WorkDotnet\SupportToolsServerDbTools\SupportToolsServerDbTools` |
| რეპოები | SupportToolsServer, SupportToolsServerCore, SupportToolsServerDbPart, SupportToolsServerShared, SupportToolsServerDbTools (მიგრაცია) |
| დამოკიდებულია | B1 (და A3) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა B8 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G2/G3, §4.1, §4.4, §4.5, §7);
- რეპოს CLAUDE.md, სექცია „Registry conventions“.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი:
- რეესტრი მიუთითებს ფაილებზე, რომლებიც მხოლოდ მთავარ კომპიუტერზეა და საიდუმლოებებს შეიცავს. მაგალითად, `ServerInfo.AppSettingsJsonSourceFileName` = `D:\1WorkSecurity\<Project>\<server>\<env>\appsettings.json` (connection string-ები და სხვა).
- მომხმარებელმა გადაწყვიტა, რომ ესენიც სერვერზე ღიად შეინახება (G2).
- B8 სერვერზე ქმნის ტექსტური ფაილების საცავს. კლიენტის მხარეს C6 გააკეთებს.

აგრეგატი `StoredFile`. ველები:
- `Path`: კანონიკური გზა (G3), Windows-ის აბსოლუტური ფორმა. რეგისტრის გარეშე უნიკალურია. მაქსიმალური სიგრძე შეარჩიე და დაასაბუთე (~400).
- `Content`: ტექსტი, `nvarchar(max)`. ვალიდატორი ზომას ზღუდავს, მაგალითად 1 MB; ზუსტი ზღვარი დაასაბუთე.
- `Sha256`: hex, 64 სიმბოლო, სერვერი თვითონ ითვლის.
- `Version`, `UpdatedAtUtc`.

გზის ვალიდაცია:
- აბსოლუტური Windows ფორმა: `X:\...`;
- `..` სეგმენტები არ არის;
- აკრძალული სიმბოლოები არ არის;
- სიგრძე ზღვარს არ აღემატება.

endpoint-ები B1-ის კონვენციის მიხედვით, ერთი გამონაკლისით: გზა route-ის key-ში ვერ ჩაჯდება (`\`, `:`), ამიტომ query-ში ან body-ში გადაეცემა.
- `GET files`: მეტამონაცემების სია შიგთავსის გარეშე — `Path`, `Sha256`, `Length`, `Version`, `UpdatedAtUtc`.
- `GET files/content?path=...`: ერთი ფაილი შიგთავსით.
- `POST files/update`: body `{ Path, Content, Version }`. upsert-ის სემანტიკა B1-ისაა. პასუხი ახალი ვერსიაა.
- `DELETE files/delete?path=...&version=N`.
- group იბადება `.RequireAuthorization()`-ით.
- route-ის კლასი Shared-ში.

კონტრაქტები: `StsStoredFileInfoDataModel` (მეტამონაცემები) და `StsStoredFileDataModel` (შიგთავსით). კლიენტის მეთოდები Shared-ში.

უსაფრთხოება:
- შიგთავსი არ უნდა მოხვდეს ლოგში, Debug ტრეისში ან შეცდომის ტექსტში. ტრეისში მხოლოდ გზა იწერება.
- ტესტებში მხოლოდ გამოგონილი შიგთავსი გამოიყენე.

ტესტები:
- ვალიდატორი: გზები და ზომა;
- handler-ები: შექმნა, განახლება, კონფლიქტი, წაშლა;
- `Sha256`-ის გამოთვლა;
- რეპოზიტორი SQLite-ზე;
- endpoint-ები და route-ები;
- Shared კლიენტი, query-ში გზის სწორი escape-ის ჩათვლით.

მიგრაცია: `AddStoredFiles` (README §7-ის წესი 10).

დასრულების კრიტერიუმები:
- `dotnet build` და `dotnet test` მწვანეა ყველა შეხებულ solution-ზე;
- მიგრაცია შექმნილია, ან მომხმარებელს მიცემული აქვს ზუსტი ნაბიჯები.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში B8-ის სტატუსი განაახლე.
````
