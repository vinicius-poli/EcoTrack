using EcoTrack.Domain.Condicionantes;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Infrastructure.Persistence.Repositories;

public class CondicionanteRepository : ICondicionanteRepository
{
    private readonly EcoTrackDbContext _context;

    public CondicionanteRepository(EcoTrackDbContext context)
    {
        _context = context;
    }

    public async Task CadastrarAsync(Condicionante condicionante, CancellationToken cancellationToken = default)
    {
        await _context.Condicionantes.AddAsync(condicionante, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EditarAsync(Condicionante condicionante, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Condicionantes
            .Include(c => c.Evidencias)
            .FirstOrDefaultAsync(c => c.Id == condicionante.Id, cancellationToken);

        if (registro is null) return false;

        _context.Entry(registro).CurrentValues.SetValues(condicionante);

        // Sincroniza evidências adicionadas ou removidas no agregado
        var evidenciasAtuais = registro.Evidencias.ToList();
        var idsAtualizados = condicionante.Evidencias.Select(e => e.Id).ToHashSet();

        foreach (var evidenciaRemovida in evidenciasAtuais.Where(e => !idsAtualizados.Contains(e.Id)))
        {
            _context.Evidencias.Remove(evidenciaRemovida);
        }

        foreach (var evidenciaNova in condicionante.Evidencias.Where(e => evidenciasAtuais.All(a => a.Id != e.Id)))
        {
            evidenciaNova.CondicionanteId = registro.Id;
            await _context.Evidencias.AddAsync(evidenciaNova, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        var registro = await _context.Condicionantes.FindAsync([idSelecionado], cancellationToken);
        if (registro is null) return false;

        _context.Condicionantes.Remove(registro);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Condicionante?> SelecionarPorIdAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        return await _context.Condicionantes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idSelecionado, cancellationToken);
    }

    public async Task<Condicionante?> SelecionarPorIdComEvidenciasAsync(Guid idSelecionado, CancellationToken cancellationToken = default)
    {
        return await _context.Condicionantes
            .AsNoTracking()
            .Include(c => c.Evidencias)
            .FirstOrDefaultAsync(c => c.Id == idSelecionado, cancellationToken);
    }

    public async Task<List<Condicionante>> SelecionarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Condicionantes
            .AsNoTracking()
            .Include(c => c.Evidencias)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Condicionante>> SelecionarPorLicencaIdAsync(Guid licencaId, CancellationToken cancellationToken = default)
    {
        return await _context.Condicionantes
            .AsNoTracking()
            .Include(c => c.Evidencias)
            .AsSplitQuery()
            .Where(c => c.LicencaId == licencaId)
            .ToListAsync(cancellationToken);
    }
}