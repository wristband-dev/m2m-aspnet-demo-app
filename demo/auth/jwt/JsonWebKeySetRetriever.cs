using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;

namespace Wristband.AspNet.Auth.Jwt;

public class JsonWebKeySetRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        return retriever.GetDocumentAsync(address, cancel).ContinueWith(task => new JsonWebKeySet(task.Result), cancel);
    }
}
