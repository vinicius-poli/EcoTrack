namespace EcoTrack.Domain.Common;

public abstract class Entity<T>
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public abstract List<string> Validar();

    public abstract void Atualizar(T entidadeAtualizada);
}