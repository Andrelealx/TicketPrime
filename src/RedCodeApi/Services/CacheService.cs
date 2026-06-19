using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using RedCodeApi.Dtos.FlyCompare;

namespace RedCodeApi.Services;

/// <summary>
/// Servico de cache para resultados de busca de voos (SPEC-016 / SPEC-020).
/// Suporta:
/// - IDistributedCache (Redis) como cache primario quando disponivel
/// - IMemoryCache como fallback (sempre disponivel)
///
/// Estrategia de leitura (cache-aside com fallback em cascata):
///   Redis (L2) → Memoria (L1) → null
/// Se Redis falhar ou retornar null, tenta memoria.
/// Se ambos falharem, retorna null para que o chamador execute os scrapers.
///
/// Estrategia de escrita:
///   Escreve em AMBAS as camadas quando Redis esta configurado,
///   ou apenas na memoria quando Redis nao esta disponivel.
///   TTL padrao de 30 minutos.
/// </summary>
public class CacheService
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _ttlPadrao;
    private readonly bool _usarCacheDistribuido;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CacheService(
        ILogger<CacheService> logger,
        IDistributedCache? distributedCache = null,
        IMemoryCache? memoryCache = null)
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;
        _ttlPadrao = TimeSpan.FromMinutes(30);
        _usarCacheDistribuido = distributedCache != null;

        if (_usarCacheDistribuido)
        {
            _logger.LogInformation("[Cache] Inicializado com Redis (L2) + Memoria (L1)");
        }
        else
        {
            _logger.LogInformation("[Cache] Inicializado apenas com Memoria (Redis nao configurado)");
        }
    }

    /// <summary>
    /// Gera a chave de cache padrao para uma busca de voo.
    /// Formato: "voo:{ORIGEM}:{DESTINO}:{yyyyMMdd}"
    /// </summary>
    private static string GerarChave(string origem, string destino, DateTime dataPartida)
    {
        return $"voo:{origem.ToUpperInvariant()}:{destino.ToUpperInvariant()}:{dataPartida:yyyyMMdd}";
    }

    /// <summary>
    /// Tenta obter resultados do cache em cascata: Redis → Memoria → null.
    /// Se Redis falhar ou retornar null, faz fallback para memoria.
    /// </summary>
    public async Task<List<ResultadoBusca>?> ObterAsync(
        string origem,
        string destino,
        DateTime dataPartida)
    {
        var chave = GerarChave(origem, destino, dataPartida);

        // Tenta Redis primeiro (L2)
        if (_usarCacheDistribuido && _distributedCache != null)
        {
            var redisResult = await ObterDoRedisAsync(chave);
            if (redisResult is { Count: > 0 })
                return redisResult;

            // Redis miss ou falha — faz fallback para memoria (L1)
            _logger.LogInformation("[Cache] Redis MISS para {Chave}. Tentando memoria...", chave);
            var memoryResult = ObterDaMemoria(chave);
            if (memoryResult is { Count: > 0 })
            {
                _logger.LogInformation("[Cache] Memory HIT (fallback apos Redis miss) para {Chave}", chave);
                return memoryResult;
            }

            _logger.LogInformation("[Cache] Cache MISS completo (Redis + Memoria) para {Chave}", chave);
            return null;
        }

        // Apenas memoria (Redis nao configurado)
        return ObterDaMemoria(chave);
    }

    /// <summary>
    /// Armazena resultados no cache. Se Redis estiver configurado,
    /// escreve em ambas as camadas (Redis + Memoria).
    /// Caso contrario, apenas na memoria.
    /// </summary>
    public async Task ArmazenarAsync(
        string origem,
        string destino,
        DateTime dataPartida,
        List<ResultadoBusca> resultados)
    {
        var chave = GerarChave(origem, destino, dataPartida);

        // Sempre armazena na memoria (L1)
        ArmazenarNaMemoria(chave, resultados);

        // Se Redis disponivel, armazena tambem (L2)
        if (_usarCacheDistribuido && _distributedCache != null)
        {
            await ArmazenarNoRedisAsync(chave, resultados);
        }
    }

    /// <summary>
    /// Remove uma entrada do cache de ambas as camadas.
    /// </summary>
    public async Task RemoverAsync(string origem, string destino, DateTime dataPartida)
    {
        var chave = GerarChave(origem, destino, dataPartida);

        _memoryCache?.Remove(chave);
        _logger.LogInformation("[Cache] Removido da memoria: {Chave}", chave);

        if (_usarCacheDistribuido && _distributedCache != null)
        {
            await _distributedCache.RemoveAsync(chave);
            _logger.LogInformation("[Cache] Removido do Redis: {Chave}", chave);
        }
    }

    /// <summary>
    /// Obtem dados do Redis (cache distribuido).
    /// </summary>
    private async Task<List<ResultadoBusca>?> ObterDoRedisAsync(string chave)
    {
        try
        {
            var json = await _distributedCache!.GetStringAsync(chave);
            if (json != null)
            {
                var resultados = JsonSerializer.Deserialize<List<ResultadoBusca>>(json, _jsonOptions);
                _logger.LogInformation(
                    "[Cache] Redis HIT para {Chave}: {Quantidade} resultados",
                    chave, resultados?.Count ?? 0);
                return resultados;
            }

            _logger.LogInformation("[Cache] Redis MISS para {Chave}", chave);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Erro ao ler do Redis para {Chave}. Usando fallback.", chave);
        }

        return null;
    }

    /// <summary>
    /// Obtem dados da memoria local.
    /// </summary>
    private List<ResultadoBusca>? ObterDaMemoria(string chave)
    {
        if (_memoryCache != null &&
            _memoryCache.TryGetValue<List<ResultadoBusca>>(chave, out var resultados))
        {
            _logger.LogInformation(
                "[Cache] Memory HIT para {Chave}: {Quantidade} resultados",
                chave, resultados?.Count ?? 0);
            return resultados;
        }

        _logger.LogInformation("[Cache] Memory MISS para {Chave}", chave);
        return null;
    }

    /// <summary>
    /// Armazena dados no Redis.
    /// </summary>
    private async Task ArmazenarNoRedisAsync(string chave, List<ResultadoBusca> resultados)
    {
        try
        {
            var json = JsonSerializer.Serialize(resultados, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _ttlPadrao
            };

            await _distributedCache!.SetStringAsync(chave, json, options);

            _logger.LogInformation(
                "[Cache] Armazenado no Redis: {Chave}: {Quantidade} resultados (TTL: {Ttl}min)",
                chave, resultados.Count, _ttlPadrao.TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] Erro ao escrever no Redis para {Chave}", chave);
        }
    }

    /// <summary>
    /// Armazena dados na memoria local.
    /// </summary>
    private void ArmazenarNaMemoria(string chave, List<ResultadoBusca> resultados)
    {
        if (_memoryCache == null)
        {
            _logger.LogWarning("[Cache] Memoria nao disponivel para armazenar {Chave}", chave);
            return;
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttlPadrao,
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _memoryCache.Set(chave, resultados, options);

        _logger.LogInformation(
            "[Cache] Armazenado na memoria: {Chave}: {Quantidade} resultados (TTL: {Ttl}min)",
            chave, resultados.Count, _ttlPadrao.TotalMinutes);
    }
}
