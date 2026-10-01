# D2 — ავტომატური სინქრონიზაცია

| | |
|-|-|
| სესიის საქაღალდე | `D:\1WorkDotnet\SupportTools\SupportTools` |
| დამატებითი საქაღალდეები | `D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry` |
| რეპოები | SupportTools |
| დამოკიდებულია | C5 (სტაბილური, ხელით შემოწმებული), D1, A4 (SimpleNames cruder-ები ინახავს) |
| ზომა | M |

## პრომპტი

````text
ეს არის ამოცანა D2 SupportTools-ის ცენტრალიზებული რეესტრის გეგმიდან. ჯერ წაიკითხე:
- D:\1WorkDotnet\SupportToolsServer\SupportToolsServer\docs\CentralRegistry\README.md (§3 G5, §4.3, §7);
- ორივე CLAUDE.md;
- C2–C6-ის კოდი;
- D:\1WorkDotnet\SupportTools\ParametersManagement\ParametersManagement.LibParameters\ (`IParametersManager`, `ParametersManager`, `DependencyInjection\ParametersManagerServicesExtensions.cs`) და `SupportTools\DependencyInjection\SupportToolsServices.cs`.
§7-ის წესები სავალდებულოა: commit, push და pull არ გააკეთო; build-ი მკაცრია; ტესტები სავალდებულოა; საიდუმლოებებს არ კითხულობ და არ ბეჭდავ.

მიზანი: სინქრონიზაცია ავტომატურად ხდება ორ მომენტში: პროგრამის გაშვებისას (Pull) და ყოველი შენახვის შემდეგ (Push). ფუნქცია opt-in-ია. ხელით ბრძანება (C5) რჩება კონფლიქტებისა და რთული შემთხვევებისთვის.

სამუშაო:

1. ლოკალური პარამეტრები:
   - `AutoSyncWithSupportToolsServer` (bool, default false);
   - `SupportToolsServerSyncTimeoutSeconds` (მოკლე, მაგ. 10).
   - რედაქტორი „Support Tools Parameters Editor“-ში.

2. გაშვებისას. პარამეტრების ჩატვირთვისა და C1-ის `IsLocal`-ის გამოთვლის შემდეგ, მენიუმდე და `--run`-მდე:
   - აიგება გეგმა;
   - ავტომატურად სრულდება მხოლოდ არაკონფლიქტური Pull-ები;
   - ჩანს ერთხაზიანი შეჯამება;
   - თუ არის კონფლიქტი ან Push-ის მომლოდინე ჩანაწერი, ჩანს შეტყობინება და რჩევა, გამოიძახო „Sync Registry With SupportToolsServer...“;
   - თუ სერვერი მიუწვდომელია (timeout ან შეცდომა), ჩანს გაფრთხილება და მუშაობა ლოკალური ასლით გრძელდება (offline). პროგრამა არ ჩერდება.

3. შენახვის შემდეგ:
   - `IParametersManager`-ის decorator SupportTools-ში. ParametersManagement-ს არ ცვლი.
   - ის `SupportToolsMainParametersManager`-ს ფუთავს და ~150 cast-ისთვის იმავე `Parameters` ობიექტს აბრუნებს.
   - `AddMainParametersManager<T>` მოითხოვს `T : ParametersManager`-ს, ამიტომ რეგისტრაცია სხვანაირად მოაწყვე. ნახე `ParametersManagerServicesExtensions` და `MainParametersManagerOptions`. ერთადერთი `IParametersManager` singleton-ი decorator უნდა იყოს.
   - წარმატებული ლოკალური შენახვის შემდეგ გეგმიდან სრულდება მხოლოდ არაკონფლიქტური Push-ები.
     - კონფლიქტისას ჩანს გაფრთხილება; ლოკალური ცვლილება რჩება მომლოდინედ და შემდეგ სინქრონიზაციაზე გამოჩნდება.
     - სერვერის შეცდომა ლოკალურ შენახვას **არასოდეს** აფუჭებს: `Save` აბრუნებს ლოკალური შენახვის შედეგს.
   - ერთი ოპერაცია ფაილს 2–3-ჯერ ინახავს (ParCruder + Cruder). მეორე და მესამე Push ცვლილებას აღარ იპოვის, რადგან მდგომარეობა უკვე განახლებულია. შეამოწმე, რომ ეს ზედმეტი მოთხოვნების ნაკადს არ ქმნის. თუ ქმნის, მოიფიქრე მარტივი გამოსავალი, მაგალითად ბოლო გეგმის ჰეში ან Push-ის გამოტოვება, როცა ცვლილება არ არის.
   - თვითონ ძრავა Save-ს იძახებს მდგომარეობის შესანახად. decorator-ში რეკურსია არ უნდა წარმოიშვას: ძრავისთვის „შიდა“ Save ან ალამი.

4. `--run` რეჟიმი (`ProjectToolRunner`): გაშვებისას Pull იგივე წესით. Push შენახვის შემდეგ, თუ ხელსაწყო რამეს ინახავს.

ტესტები (SupportTools.Tests):
- decorator: ლოკალური Save წარმატებულია და Push გამოიძახება; სერვერი მიუწვდომელია → Save წარმატებულია და ჩანს გაფრთხილება; კონფლიქტი → მომლოდინე; ალამი გამორთულია → სერვერი არ გამოიძახება; რეკურსია არ ხდება;
- გაშვების Pull: მხოლოდ არაკონფლიქტური, offline ქცევა.
სერვერი fake-ია.

დასრულების კრიტერიუმები:
- `dotnet build SupportTools.slnx` და `dotnet test SupportTools.slnx` მწვანეა;
- CLAUDE.md-ში აღწერილია ავტოსინქრონიზაცია და decorator-ის რეგისტრაცია;
- მომხმარებლისთვის ხელით შესამოწმებელი სცენარი: ორი კომპიუტერი, ცვლილება ერთზე, გაშვება მეორეზე.
ბოლოს დაწერე README §7-ის წესი 13-ის ანგარიში და README §5-ში D2-ის სტატუსი განაახლე.
````
