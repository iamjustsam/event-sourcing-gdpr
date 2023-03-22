using Newtonsoft.Json.Linq;

namespace EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

public record CryptoEnvelope(string DataSubjectId, string? Data);
