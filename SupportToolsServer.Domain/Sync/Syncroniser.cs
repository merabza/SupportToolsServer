using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SupportToolsServer.Domain.Primitives;

namespace SupportToolsServer.Domain.Sync;

public class Syncroniser<T, TId> where T : Entity<TId> where TId : notnull
{
    private readonly ICrudRepository<T, TId> _crudRepository;
    private readonly List<T> _entities;

    public Syncroniser(ICrudRepository<T, TId> crudRepository, List<T> entities)
    {
        _crudRepository = crudRepository;
        _entities = entities;
    }

    public async Task<bool> DoSyncUp(bool merge = true, CancellationToken cancellationToken = default)
    {
        //ჩავტვირთოთ ბაზაში არსებული ყველა ჩანაწერი
        var existingEntities = await _crudRepository.GetAll(cancellationToken);

        if (!merge) //თუ არ გვინდა მერჯი, მაშინ წავშალოთ ყველა ჩანაწერი, რომელიც ბაზაშია, მაგრამ მოწოდებულ სიაში არაა
        {
            //ვიპოვით ბაზაში ყველა ისეთი ჩანაწერი, რომელიც არ გვაქვს მოწოდებულ სიაში
            var entitiesToDelete = existingEntities.Where(e => _entities.All(r => r != e)).ToList();
            //წავშალოთ ისინი
            entitiesToDelete.ForEach(e => _crudRepository.Delete(e));
        }

        //ვიპოვით ყველა ჩანაწერი, რომელიც მოწოდებულ სიაშია, მაგრამ ბაზაში არ არსებობს
        var entitiesToAdd = _entities.Where(r => existingEntities.All(e => e != r)).ToList();
        //დავამატოთ ისინი
        entitiesToAdd.ForEach(r => _crudRepository.Add(r));

        //ვიპოვოთ ყველა ჩანაწერი, რომელიც ორივე სიაშია და შევადაროთ მათი შინაარსი
        var entitiesToUpdate = _entities.Where(r => existingEntities.Any(e => e == r)).ToList();
        //განვაახლოთ ისინი
        entitiesToUpdate.ForEach(r => _crudRepository.Update(r));

        return true;
    }
}