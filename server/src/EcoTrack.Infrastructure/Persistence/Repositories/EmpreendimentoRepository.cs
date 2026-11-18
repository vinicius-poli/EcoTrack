using EcoTrack.Domain.Empreendimentos;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence.Repositories;

public class EmpreendimentoRepository : IEmpreendimentoRepository
{
    private readonly EcoTrackDbContext _context;

    public EmpreendimentoRepository(EcoTrackDbContext context)
    {
        _context = context;
    }

    public async Task CadastrarAsync(Empreendimento empreendimento, CancellationToken cancellationToken = default)
    {
        await _context.Empreendimentos.AddAsync(empreendimento, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EditarAsync(Guid idSelecionado, Empreendimento empreendimentoAtualizado, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Empreendimentos.FindAsync([idSelecionado], cancellationToken);
        if (registro is null) return false;

        _context.Entry(registro).CurrentValues.SetValues(empreendimentoAtualizado);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Empreendimentos.FindAsync([idSelecionado], cancellationToken);
        if (registro is null) return false;

        _context.Empreendimentos.Remove(registro);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Empreendimento?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        return await _context.Empreendimentos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == idSelecionado, cancellationToken);
    }

    public async Task<List<Empreendimento>> SelecionarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Empreendimentos
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Empreendimento>> FiltrarAsync(Predicate<Empreendimento> filtro, CancellationToken cancellationToken = default)
    {
        var todos = await _context.Empreendimentos
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return todos.Where(e => filtro(e)).ToList();
    }

    public async Task<bool> ExisteCnpjAsync(string cnpjNormalizado, Guid? ignorarId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Empreendimentos.AsQueryable();

        if (ignorarId.HasValue)
        {
            query = query.Where(e => e.Id != ignorarId.Value);
        }

        return await query.AnyAsync(e => e.Cnpj == cnpjNormalizado, cancellationToken);
    }

    public async Task<Empreendimento?> SelecionarPorCnpjAsync(string cnpjNormalizado, CancellationToken cancellationToken = default)
    {
        return await _context.Empreendimentos
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Cnpj == cnpjNormalizado, cancellationToken);
    }
}