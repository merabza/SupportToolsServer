using SystemTools.Application.Abstractions.Messaging;

namespace SupportToolsServer.Application.GitIgnoreFileTypes.EnsureGitIgnoreFileType;

//POST .../updategitignorefiletype/{key}: კლიენტი მხოლოდ სახელს აგზავნის. ახალი ტიპი ცარიელი შინაარსით ემატება,
//არსებული უცვლელი რჩება. შინაარსს ატვირთვა (UploadGitRepos, SyncUp) ავსებს
public class EnsureGitIgnoreFileTypeCommand : ICommand
{
    public EnsureGitIgnoreFileTypeCommand(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
