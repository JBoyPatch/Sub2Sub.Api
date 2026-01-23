using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Sub2SubApi.Application.Models;

namespace Sub2SubApi.Infrastructure.Riot;

public sealed class RiotApiClient : IRiotApiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _platformBase;
    private readonly string _regionalBase;

    public RiotApiClient(HttpClient http, string apiKey)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));

        // Prefer environment overrides; default to common NA/platform + Americas/regional
        _platformBase = Environment.GetEnvironmentVariable("RIOT_BASE_URL_PLATFORM") ?? "https://na1.api.riotgames.com";
        _regionalBase = Environment.GetEnvironmentVariable("RIOT_BASE_URL_REGION") ?? "https://americas.api.riotgames.com";

        // Riot expects API key in X-Riot-Token header
        if (!_http.DefaultRequestHeaders.Contains("X-Riot-Token"))
            _http.DefaultRequestHeaders.Add("X-Riot-Token", _apiKey);

        // Log masked info for debugging (do NOT print the key itself)
        try
        {
            Console.WriteLine($"RiotApiClient initialized. platform={_platformBase} regional={_regionalBase} keyLength={_apiKey?.Length ?? 0}");
        }
        catch { /* best-effort logging */ }
    }

    public async Task<RiotAccountDto?> GetAccountByRiotIdAsync(string gameName, string tagLine)
    {
        var url = $"{_regionalBase}/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) return null;
        if (status >= 200 && status < 300)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var puuid = doc.RootElement.GetProperty("puuid").GetString() ?? string.Empty;
                return new RiotAccountDto { Puuid = puuid };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Riot account response", ex);
            }
        }

        var snippet = body is null ? string.Empty : (body.Length > 500 ? body.Substring(0, 500) : body);
        throw new Exception($"Riot API returned status {status} for account lookup. Response: {snippet}");
    }

    public async Task<RiotSummonerDto?> GetSummonerByPuuidAsync(string puuid)
    {
        var url = $"{_platformBase}/lol/summoner/v4/summoners/by-puuid/{Uri.EscapeDataString(puuid)}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) return null;
        if (status >= 200 && status < 300)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // `id` (encrypted summoner id) may be missing in some responses; treat it as optional.
                var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;

                var icon = root.TryGetProperty("profileIconId", out var iconEl) && iconEl.ValueKind != JsonValueKind.Null ? iconEl.GetInt32() : 0;
                var level = root.TryGetProperty("summonerLevel", out var levelEl) && levelEl.ValueKind != JsonValueKind.Null ? levelEl.GetInt64() : 0L;

                return new RiotSummonerDto { SummonerId = id, ProfileIconId = icon, SummonerLevel = level };
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse Riot summoner response", ex);
            }
        }

        throw new Exception($"Riot API returned status {status} for summoner lookup");
    }

    public async Task<IReadOnlyList<RiotRankedEntryDto>> GetRankedEntriesBySummonerIdAsync(string summonerId)
    {
        var url = $"{_platformBase}/lol/league/v4/entries/by-summoner/{Uri.EscapeDataString(summonerId)}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) return Array.Empty<RiotRankedEntryDto>();
        if (status >= 200 && status < 300)
        {
            try
            {
                var list = new List<RiotRankedEntryDto>();
                using var doc = JsonDocument.Parse(body);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var queue = el.GetProperty("queueType").GetString() ?? string.Empty;
                    var tier = el.GetProperty("tier").GetString() ?? string.Empty;
                    var rank = el.GetProperty("rank").GetString() ?? string.Empty;
                    var lp = el.GetProperty("leaguePoints").GetInt32();
                    var wins = el.GetProperty("wins").GetInt32();
                    var losses = el.GetProperty("losses").GetInt32();
                    list.Add(new RiotRankedEntryDto { QueueType = queue, Tier = tier, Rank = rank, LeaguePoints = lp, Wins = wins, Losses = losses });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse ranked entries", ex);
            }
        }

        throw new Exception($"Riot API returned status {status} for ranked entries");
    }

    public async Task<IReadOnlyList<RiotChampionMasteryDto>> GetChampionMasteryByPuuidAsync(string puuid)
    {
        var url = $"{_platformBase}/lol/champion-mastery/v4/champion-masteries/by-puuid/{Uri.EscapeDataString(puuid)}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) return Array.Empty<RiotChampionMasteryDto>();
        if (status >= 200 && status < 300)
        {
            try
            {
                var list = new List<RiotChampionMasteryDto>();
                using var doc = JsonDocument.Parse(body);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var champId = el.GetProperty("championId").GetInt32();
                    var champPoints = el.GetProperty("championPoints").GetInt64();
                    var champLevel = el.GetProperty("championLevel").GetInt32();
                    var lastPlay = el.TryGetProperty("lastPlayTime", out var lp) && lp.ValueKind != JsonValueKind.Null ? lp.GetInt64() : (long?)null;
                    var chest = el.TryGetProperty("chestGranted", out var cg) && cg.ValueKind != JsonValueKind.Null ? cg.GetBoolean() : (bool?)null;
                    list.Add(new RiotChampionMasteryDto { ChampionId = champId, ChampionPoints = champPoints, ChampionLevel = champLevel, LastPlayTime = lastPlay, ChestGranted = chest });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse champion mastery", ex);
            }
        }

        throw new Exception($"Riot API returned status {status} for champion mastery");
    }

    public async Task<IReadOnlyList<string>> GetMatchIdsByPuuidAsync(string puuid, int start, int count)
    {
        var url = $"{_regionalBase}/lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids?start={start}&count={count}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) return Array.Empty<string>();
        if (status >= 200 && status < 300)
        {
            try
            {
                var list = new List<string>();
                using var doc = JsonDocument.Parse(body);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(el.GetString() ?? string.Empty);
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse match ids", ex);
            }
        }

        throw new Exception($"Riot API returned status {status} for match ids");
    }

    public async Task<RiotMatchDto> GetMatchByIdAsync(string matchId)
    {
        var url = $"{_regionalBase}/lol/match/v5/matches/{Uri.EscapeDataString(matchId)}";
        var (status, body) = await GetStringWithRetriesAsync(url);
        if (status == 404) throw new Exception("Match not found");
        if (status >= 200 && status < 300)
        {
            return new RiotMatchDto { MatchId = matchId, RawJson = body };
        }

        throw new Exception($"Riot API returned status {status} for match details");
    }

    private async Task<(int StatusCode, string Body)> GetStringWithRetriesAsync(string url)
    {
        var attempts = 0;
        var maxAttempts = 4;
        var rnd = new Random();

        while (true)
        {
            attempts++;
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.ParseAdd("application/json");
            // Ensure API key and a User-Agent are present on every request (some environments strip default headers)
            if (!req.Headers.Contains("X-Riot-Token") && !string.IsNullOrWhiteSpace(_apiKey))
                req.Headers.Add("X-Riot-Token", _apiKey);
            if (!req.Headers.Contains("User-Agent"))
                req.Headers.Add("User-Agent", "Sub2SubApi/1.0");
            try
            {
                var resp = await _http.SendAsync(req);
                var status = (int)resp.StatusCode;
                var body = await resp.Content.ReadAsStringAsync();

                if (status == 429 && attempts < maxAttempts)
                {
                    // honor Retry-After if present
                    if (resp.Headers.TryGetValues("Retry-After", out var vals))
                    {
                        if (int.TryParse(vals.FirstOrDefault(), out var sec))
                        {
                            await Task.Delay(TimeSpan.FromSeconds(sec));
                            continue;
                        }
                    }

                    // exponential backoff + jitter
                    var delay = TimeSpan.FromMilliseconds((Math.Pow(2, attempts) * 500) + rnd.Next(0, 200));
                    await Task.Delay(delay);
                    continue;
                }

                return (status, body);
            }
            catch (HttpRequestException) when (attempts < maxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds((Math.Pow(2, attempts) * 200) + rnd.Next(0, 100));
                await Task.Delay(delay);
                continue;
            }
        }
    }
}
