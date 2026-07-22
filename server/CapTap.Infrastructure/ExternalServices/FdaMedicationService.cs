using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CapTap.Application.DTOs.Medication;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CapTap.Infrastructure.ExternalServices;

/// <summary>
/// OpenFDA drug label lookup. Failures never crash CapTap — they surface as a controlled error.
/// </summary>
public sealed class FdaMedicationService : IFdaMedicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FdaMedicationService> _logger;

    public FdaMedicationService(IHttpClientFactory httpClientFactory, ILogger<FdaMedicationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<MedicationSearchResultDto>> SearchMedicationAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            var client = _httpClientFactory.CreateClient("OpenFda");
            var term = Uri.EscapeDataString(query.Trim());
            // OpenFDA OR across brand and generic name fields.
            var url = $"drug/label.json?search=openfda.brand_name:{term}+openfda.generic_name:{term}&limit=10";

            using var response = await client.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenFDA search returned {StatusCode} for query length {Length}",
                    (int)response.StatusCode,
                    query.Length);

                throw new InvalidRequestException("Medication search is temporarily unavailable. Please try again later.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OpenFdaLabelResponse>(JsonOptions, cancellationToken);
            if (payload?.Results is null || payload.Results.Count == 0)
            {
                return [];
            }

            return payload.Results
                .Select(MapResult)
                .Where(result => !string.IsNullOrWhiteSpace(result.Name))
                .GroupBy(result => $"{result.Name}|{result.Brand}|{result.Identifier}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(10)
                .ToList();
        }
        catch (InvalidRequestException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "OpenFDA search failed");
            throw new InvalidRequestException("Medication search is temporarily unavailable. Please try again later.");
        }
    }

    private static MedicationSearchResultDto MapResult(OpenFdaLabelResult result)
    {
        var brand = FirstOrDefault(result.OpenFda?.BrandName);
        var generic = FirstOrDefault(result.OpenFda?.GenericName);
        var substance = FirstOrDefault(result.OpenFda?.SubstanceName);
        var productNdc = FirstOrDefault(result.OpenFda?.ProductNdc);
        var applicationNumber = FirstOrDefault(result.OpenFda?.ApplicationNumber);

        var name = generic ?? brand ?? substance ?? "Unknown medication";
        var identifier = productNdc ?? applicationNumber;

        return new MedicationSearchResultDto
        {
            Name = name,
            Brand = brand,
            Identifier = identifier
        };
    }

    private static string? FirstOrDefault(IReadOnlyList<string>? values) =>
        values is { Count: > 0 } ? values[0] : null;

    private sealed class OpenFdaLabelResponse
    {
        public List<OpenFdaLabelResult>? Results { get; set; }
    }

    private sealed class OpenFdaLabelResult
    {
        [JsonPropertyName("openfda")]
        public OpenFdaMetadata? OpenFda { get; set; }
    }

    private sealed class OpenFdaMetadata
    {
        [JsonPropertyName("brand_name")]
        public List<string>? BrandName { get; set; }

        [JsonPropertyName("generic_name")]
        public List<string>? GenericName { get; set; }

        [JsonPropertyName("substance_name")]
        public List<string>? SubstanceName { get; set; }

        [JsonPropertyName("product_ndc")]
        public List<string>? ProductNdc { get; set; }

        [JsonPropertyName("application_number")]
        public List<string>? ApplicationNumber { get; set; }
    }
}
