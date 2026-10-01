# A3 — API key ავთენტიფიკაცია ყველა endpoint-ზე

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\WebSystemTools`, `D:\1WorkDotnet\SupportToolsServer\SystemTools` |
| რეპოები | SupportToolsServer, WebSystemTools, SystemTools |
| დამოკიდებულია | — (B1-მდე უნდა დასრულდეს, რომ ახალი group-ები ავთენტიფიკაციით დაიბადოს) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა A3 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G2/G4, §4.5, §7);
- რეპოს CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი. ამჟამად ყველა endpoint ანონიმურია. G2-ის მიხედვით სერვერზე საიდუმლოებები ღიად შეინახება, ამიტომ ავთენტიფიკაცია პირველი საიდუმლოს ატვირთვამდე უნდა ჩაირთოს. ინფრასტრუქტურა უკვე არსებობს:
- `WebSystemTools.ApiKeyIdentity` (`AddApiKeyIdentity`, `UseApiKeysAuthorization`, `TokenAuthenticationHandler`) კითხულობს query პარამეტრს `ApiKey` და კლიენტის IPv4-ს.
- გასაღებებს `SystemTools.ApiKeysManagement` → `ApiKeysDomain` კითხულობს კონფიგურაციის სექციიდან `ApiKeys:AppSettingsByApiKey: [{ ApiKey, RemoteIpAddress }]`. გასაღებიც და IP-იც ზუსტად უნდა დაემთხვეს.
- კლიენტი (`SystemTools.ApiContracts.ApiClient`) ყოველ მოთხოვნას `?apikey=`-ს უმატებს. SupportTools გასაღებს `ApiClients[SupportToolsServerWebApiClientName].ApiKey`-დან იღებს.
- `GitReposEndpoints`-ში `RequireAuthorization` საერთოდ არ წერია; `GitIgnoreFileTypesEndpoints.cs:41`-სა და `EditorConfigFileTypesEndpoints.cs:31`-ში კომენტარშია.
- SignalR hub (`api/v1/messages`) უკვე ითხოვს ავთენტიფიკაციას.
- `api/v1/test/...`-ს VersionChecker იყენებს და ანონიმური უნდა დარჩეს.
- appsettings.json-ში `ApiKeys` სექცია არ არის.

დაწყებამდე მომხმარებელს ჰკითხე გასაღების მოდელი:
(ა) თითო კომპიუტერს თავისი გასაღები და ფიქსირებული IP. კოდის ცვლილება არ სჭირდება, მაგრამ ცვლადი IP-ის მქონე ან მოგზაური კომპიუტერი ვერ დაუკავშირდება.
(ბ) გასაღები IP-ის გარეშე, მხოლოდ იმ ჩანაწერისთვის, რომლის `RemoteIpAddress` არის `*`. ეს ცვლის `SystemTools.ApiKeysManagement`-ს, საზიარო ბიბლიოთეკას, რომელსაც WebAgent-ებიც იყენებს; ნაგულისხმევი ქცევა უცვლელი რჩება.
რეკომენდაცია: (ბ), რადგან ჰოსტი ჯერ გადაწყვეტილი არ არის (G4) და Linux კომპიუტერიც არსებობს.

სამუშაო:

1. სამივე group-ზე (GitRepos, GitIgnoreFileTypes, EditorConfigFileTypes) დაამატე `.RequireAuthorization()`. CLAUDE.md-ში ჩაწერე კონვენცია: ყოველი ახალი group იბადება `.RequireAuthorization()`-ით.

2. დარწმუნდი, რომ ავთენტიფიკაცია pipeline-ში რეალურად მუშაობს. `UseApiKeysAuthorization` მხოლოდ `UseAuthorization()`-ს იძახებს. შეამოწმე, ემატება თუ არა `UseAuthentication` ავტომატურად და სწორია თუ არა middleware-ების რიგი `Program.cs`-ში.
   - დაწერე ინტეგრაციული ტესტი `Microsoft.AspNetCore.TestHost`-ით ან მსგავსით. პაკეტი, თუ საჭიროა, `Directory.Packages.props`-ში დაამატე.
   - შემთხვევები:
     - გასაღების გარეშე → 401;
     - სწორი გასაღები და IP → 200;
     - არასწორი გასაღები → 401;
     - `api/v1/test/getversion` გასაღების გარეშე → 200.
   - TestServer-ში `RemoteIpAddress` null-ია, ამიტომ ტესტში middleware-ით დააყენე.

3. თუ მომხმარებელმა (ბ) აირჩია: `ApiKeysDomain`-ში დაამატე `*`-ის მხარდაჭერა და ტესტები SystemTools-ში. თუ შესაბამისი სატესტო პროექტი არ არსებობს, ჰკითხე, შეიქმნას თუ არა.

4. appsettings.json-ში დაამატე `ApiKeys` სექციის placeholder (არა რეალური გასაღები). CLAUDE.md-ში აღწერე, სად იწერება რეალური გასაღებები: Development-ში user secrets, Prod-ში D1-ის მიხედვით.

5. `TokenAuthenticationHandler.cs:90-91`-ში უარყოფილი გასაღები ლოგში აღარ დაიწეროს. დაწერე მხოლოდ IP და, მაგალითად, გასაღების სიგრძე.

6. კლიენტის `SystemTools.ApiContracts.ApiClient`. ტესტები `SystemTools.ApiContracts.Tests`-ში.
   - (ა) შეცდომის გამონატანში URI-დან `apikey`-ის მნიშვნელობა მოაშორე (~373-375).
   - (ბ) გასაღები გაატარე `Uri.EscapeDataString`-ში (~510-521).
   - (გ) `SetAuthorizationAccessToken`-ში (112-120) ოპერატორების პრიორიტეტის შეცდომაა: `AccessToken` null-ისას ცარიელ `Bearer` header-ს აგზავნის.

7. Swagger (`AddSwagger`, Development) JWT bearer-ს აცხადებს. თუ მარტივია, შეცვალე query `ApiKey` სქემით; თუ არა, ჩაწერე ღია კითხვებში.

8. SupportTools workspace-ის SystemTools კლონი მეორე კლონია და არ უნდა შეცვალო. ბოლოს მომხმარებელს:
   - შეახსენე, რომ იქ pull და CLI-ის rebuild უნდა გააკეთოს;
   - მიეცი ინსტრუქცია: Dev სერვერის user secrets-ში `ApiKeys:AppSettingsByApiKey` ჩანაწერის დამატება და SupportTools-ში შესაბამისი ApiClient-ის `ApiKey`-ის შევსება.

არ გააკეთო: რეალური გასაღების შექმნა ან ჩაწერა რომელიმე ფაილში.

დასრულების კრიტერიუმები:
- `dotnet test SupportToolsServer.slnx` მწვანეა;
- `dotnet test SystemTools.slnx` (ან SystemTools-ის შესაბამისი solution) მწვანეა;
- WebSystemTools-ის build მწვანეა;
- ინტეგრაციული ტესტი 401/200 შემთხვევებს ფარავს.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში A3-ის სტატუსი განაახლე.
````
