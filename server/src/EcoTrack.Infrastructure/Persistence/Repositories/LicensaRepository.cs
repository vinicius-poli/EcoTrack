using EcoTrack.Domain.Licencas;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence.Repositories;

public class LicencaRepository : ILicencaRepository
{
    private readonly EcoTrackDbContext _context;

    public LicencaRepository(EcoTrackDbContext context)
    {
        _context = context;
    }

    public async Task CadastrarAsync(Licenca licenca, CancellationToken cancellationToken = default)
    {
        await _context.Licencas.AddAsync(licenca, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EditarAsync(Guid idSelecionado, Licenca licencaAtualizada, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Licencas.FindAsync([idSelecionado], cancellationToken);
        if (registro is null) return false;

        _context.Entry(registro).CurrentValues.SetValues(licencaAtualizada);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Licencas.FindAsync([idSelecionado], cancellationToken);
        if (registro is null) return false;

        _context.Licencas.Remove(registro);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Licenca?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        return await _context.Licencas
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == idSelecionado, cancellationToken);
    }

    public async Task<List<Licenca>> SelecionarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Licencas
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Licenca>> SelecionarPorEmpreendimentoIdAsync(Guid empreendimentoId, CancellationToken cancellationToken = default)
    {
        return await _context.Licencas
            .AsNoTracking()
            .Where(l => l.EmpreendimentoId == empreendimentoId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExisteNumeroLicencaAsync(string numeroLicenca, Guid? ignorarId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Licencas.AsQueryable();

        if (ignorarId.HasValue)
        {
            query = query.Where(l => l.Id != ignorarId.Value);
        }

        return await query.AnyAsync(l => l.NumeroLicenca == numeroLicenca, cancellationToken);
    }
}