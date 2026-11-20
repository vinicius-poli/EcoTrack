namespace EcoTrack.Domain.Condicionantes;

public enum PeriodicidadeCondicionante
{
    Unica = 1,
    Mensal = 2,
    Trimestral = 3,
    Semestral = 4,
    Anual = 5,
    Continua = 6
}

public enum StatusCondicionante
{
    Pendente = 1,
    EmAndamento = 2,
    Concluida = 3
}

public enum NivelRiscoCondicionante
{
    Concluido = 1, // Azul / Cinza: Status == Concluida
    Critico = 2,   // Vermelho: Não concluída e DataAtual > DataVencimento
    Alerta = 3,    // Amarelo: Não concluída e faltam <= 30 dias para DataVencimento
    Normal = 4     // Verde: Não concluída e faltam > 30 dias para DataVencimento
}