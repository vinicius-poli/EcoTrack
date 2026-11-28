using EcoTrack.Domain.Auth;
using EcoTrack.Domain.Condicionantes;
using EcoTrack.Domain.Licencas;
using EcoTrack.Infrastructure.Persistence;
using EcoTrack.Infrastructure.Persistence.Repositories;
using EcoTrack.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EcoTrack.IntegrationTests.Infrastructure;

public sealed class TestUserContext : IUserContext
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? Perfil { get; set; }
}

public sealed class IntegrationTestFixture : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly DbContextOptions<EcoTrackDbContext> _dbContextOptions;
    private bool _migrated;

    public string StorageBasePath { get; }
    public IOptions<EvidenciaStorageOptions> StorageOptions { get; }

    public IntegrationTestFixture(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("A connection string de teste é obrigatória.", nameof(connectionString));

        _connectionString = connectionString;

        StorageBasePath = Path.Combine(
            Path.GetTempPath(),
            "EcoTrackIntegrationTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(StorageBasePath);

        StorageOptions = Options.Create(new EvidenciaStorageOptions
        {
            BasePath = StorageBasePath
        });

        _dbContextOptions = new DbContextOptionsBuilder<EcoTrackDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_migrated)
            return;

        await using var context = CreateDbContext(new TestUserContext());
        await context.Database.MigrateAsync(cancellationToken);
        _migrated = true;
    }

    public EcoTrackDbContext CreateDbContext(IUserContext userContext)
    {
        return new EcoTrackDbContext(_dbContextOptions, userContext);
    }

    public OrganizacaoRepository CreateOrganizacaoRepository(IUserContext userContext)
    {
        return new OrganizacaoRepository(CreateDbContext(userContext));
    }

    public EmpreendimentoRepository CreateEmpreendimentoRepository(IUserContext userContext)
    {
        return new EmpreendimentoRepository(CreateDbContext(userContext));
    }

    public CondicionanteRepository CreateCondicionanteRepository(IUserContext userContext)
    {
        return new CondicionanteRepository(CreateDbContext(userContext));
    }

    public LicencaRepository CreateLicencaRepository(IUserContext userContext)
    {
        return new LicencaRepository(CreateDbContext(userContext));
    }

    public LocalEvidenciaStorageService CreateStorageService()
    {
        return new LocalEvidenciaStorageService(StorageOptions);
    }

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(StorageBasePath))
        {
            try
            {
                Directory.Delete(StorageBasePath, recursive: true);
            }
            catch (IOException)
            {
                // Ignora bloqueio transitório de arquivo/handle no Windows ao limpar diretório temporário
            }
        }

        await Task.CompletedTask;
    }
}