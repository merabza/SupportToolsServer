# A2 — git-ის მონაცემების ვალიდაცია და git-ის უსაფრთხო გამოძახება სერვერზე

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServerShared` |
| რეპოები | SupportToolsServer, SupportToolsServerShared |
| დამოკიდებულია | — (სასწრაფოა, სხვა ამოცანებისგან დამოუკიდებელი) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა A2 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§4.5, §7);
- რეპოს CLAUDE.md.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

კონტექსტი: `POST api/v1/git/updategitrepo/{key}` და `POST api/v1/git/uploadgitrepos` ანონიმურია. ავთენტიფიკაციას A3 ჩართავს, მაგრამ ეს ვალიდაცია მისგან დამოუკიდებლად საჭიროა. GitRepo-ს შენახვა domain event-ით სერვერზე git-ს უშვებს. სუსტი ადგილები:
- `SupportToolsServer.Infrastructure\GitProjects\GitsWorkFolder.cs:45`: `Path.Combine(gitsFolder, folderName.Replace(Path.DirectorySeparatorChar, '.'))`. Windows-ზე `/` და `..` არ იცვლება, ფესვიან გზას (`C:\x`) კი `Path.Combine` მთლიანად იღებს. შედეგად გზა `{WorkFolder}\Gits`-ის გარეთ გადის.
- `SupportToolsServer.Application\GitRepos\UpdateGitProject\UpdateGitProjectCommandHandler.cs:45-48`: თუ ფოლდერის origin სხვაა, ფოლდერი რეკურსიულად იშლება. წინა პუნქტთან ერთად ეს თვითნებური ფოლდერის წაშლას ნიშნავს.
- `SupportToolsServer.Infrastructure\GitProjects\GitClient.cs:29`: `$"clone {gitProjectAddress} \"{path}\""`. მისამართი ბრჭყალების გარეშე ჩაისმება, რაც git-ის ოფციების ინექციის საშუალებას იძლევა (`--upload-pack=…`, `-c core.sshCommand=…`).
- ვალიდატორები (`GitRepoModelValidator`, `UploadGitRepos`-ის ვალიდატორი) მხოლოდ სიცარიელესა და სიგრძეს ამოწმებს.

სამუშაო:

1. ფოლდერის სახელის წესი. საერთო helper `SupportToolsServer.Application\Validation\`-ში, რომელსაც ორივე ვალიდატორი იყენებს.
   - მნიშვნელობა შეფარდებითი გზაა: ერთი ან მეტი სეგმენტი, გამყოფი `\` ან `/`.
   - თითო სეგმენტი:
     - არ არის ცარიელი, `.` ან `..`;
     - არ შეიცავს Windows-ის აკრძალულ სიმბოლოებს. ნაკრები ფიქსირებულად აიღე და არა `Path.GetInvalidFileNameChars()`-ით, რომ შედეგი OS-ზე არ იყოს დამოკიდებული.
   - მნიშვნელობა არ არის ფესვიანი: `C:`, `\\server`, `/x`.
   - დასაშვებია წინსართი `{SpaProjectFolderRelativePath}` (კლიენტის `GitDataModel.SpaProjectFolderRelativePathName`), რადგან CLI ასეთ მნიშვნელობებს აგზავნის. ნახე, როგორ ამუშავებს მას კლიენტი ქეშისთვის (`D:\1WorkDotnet\SupportTools\SupportTools\LibGitWork\GitRepos.cs:49-67` და `GitFolderCountHelper.cs`). სერვერმაც იგივე უნდა გააკეთოს: ასეთ ჩანაწერს ფოლდერის სახელად git-ის სახელი შეურჩიოს.
   - შეცდომის კოდები: ახალი ფაბრიკები `SupportToolsServerApiClientErrors`-ში (Shared რეპო), მაგალითად `InvalidGitFolderName` და `InvalidGitAddress`.

2. მისამართის წესი.
   - დასაშვებია მხოლოდ `git@host:path` (scp-სტილის SSH), `ssh://…` და `https://…`.
   - მისამართი არ იწყება `-`-ით და არ შეიცავს whitespace-ს ან control სიმბოლოებს.
   - აკრძალულია `file://`, `ext::` და ლოკალური გზები.

3. `GitsWorkFolder`.
   - გზის აწყობისას ორივე გამყოფი `.`-ით შეცვალე (კლიენტის ქეშის წესი).
   - `Path.GetFullPath`-ით დარწმუნდი, რომ შედეგი `Gits` ფოლდერის შიგნითაა: `StartsWith(root + separator, OrdinalIgnoreCase)`.
   - თუ არ არის, დააბრუნე შეცდომა (`GitProjectsErrors`); ამ შემთხვევაში არც წაშლა ხდება და არც კლონი.
   - ეს ვალიდატორის შემდეგ დაცვის მეორე ფენაა.

4. `GitClient`.
   - ყველა ბრძანების არგუმენტები `ProcessStartInfo.ArgumentList`-ით გადაეცი, string-ის ინტერპოლაციის გარეშე: `clone`, `config`, `status`, `remote update`, `pull`, `checkout`, `reset`, `clean` და დანარჩენი.
   - clone: `git clone -- <address> <path>`.

5. ტესტები (README §7-ის წესი 7-ის ნიმუშები).
   - ვალიდატორები, უარყოფის შემთხვევები: `..\..\x`, `a/../../b`, `C:\Windows`, `\\srv\share`, `/etc`, `.`, `x\.\y`, ცარიელი სეგმენტი, `-oProxyCommand=x`, `--upload-pack=x`, `file:///c/x`, `ext::sh -c x`, whitespace მისამართში.
   - ვალიდატორები, მისაღები მნიშვნელობები: ჩვეულებრივი სახელი, ორსეგმენტიანი გზა, SPA-ის წინსართი, `git@github.com:merabza/X.git`, `https://github.com/merabza/X.git`.
   - `GitsWorkFolder`: გზის აწყობა და გარეთ გასული გზის უარყოფა (`TestInfrastructure\TempFolder`).
   - `GitClient`: არგუმენტების სია. თუ პროცესის გაშვება ადვილად არ იზოლირდება, გამოყავი internal მეთოდი, რომელიც `ArgumentList`-ს აწყობს, და ის დატესტე.
   - handler-ის ტესტი: `Gits`-ის გარეთ არსებული ფოლდერი არ იშლება.

6. თავსებადობა CLI-ის რეალურ მონაცემებთან.
   - D:\1WorkSecurity\SupportTools\SupportTools.json-დან grep-ით ამოიღე მხოლოდ `"GitProjectFolderName"` და `"GitProjectAddress"` ხაზები. სხვა გასაღებები არ დაბეჭდო.
   - შეამოწმე, რომ 73-ვე მნიშვნელობა ახალ წესებს აკმაყოფილებს.
   - თუ რომელიმე არ აკმაყოფილებს, აჩვენე და მომხმარებელს ჰკითხე, წესი შეიცვალოს თუ მონაცემი.

არ გააკეთო:
- ავთენტიფიკაცია (ეს A3-ია);
- კლიენტის კოდი (ეს A4-ია).
CLAUDE.md-ში მოკლედ ჩაწერე ფოლდერისა და მისამართის წესები.

დასრულების კრიტერიუმები:
- `dotnet build SupportToolsServer.slnx` და `dotnet test SupportToolsServer.slnx` მწვანეა;
- Shared რეპოს ტესტები (`SupportToolsServerShared.slnx`) მწვანეა, თუ იქ შეიცვალა რამე;
- რეალური მონაცემები ახალ წესებს აკმაყოფილებს.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში A2-ის სტატუსი განაახლე.
````
