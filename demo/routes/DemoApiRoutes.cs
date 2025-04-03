/// <summary>
/// Defines API route mappings for the demo application.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Maps demo API endpoints, including both public and protected routes.
    /// </summary>
    /// <param name="routes">The route builder used to define API endpoints.</param>
    public static void MapDemoEndpoints(this IEndpointRouteBuilder routes)
    {
        /// <summary>
        /// Public endpoint that does not require an access token.
        /// Calls a protected API using the <see cref="ProtectedApiClient"/> and returns its response.
        /// </summary>
        /// <param name="apiClient">The client used to call the protected API.</param>
        /// <returns>A success response containing data from the protected API, or an error response if the call fails.</returns>
        routes.MapGet("/api/public/data", async (ProtectedApiClient apiClient) =>
        {
            try
            {
                var response = await apiClient.GetProtectedDataAsync();
                return Results.Ok($"Public API called Protected API and received: {response}");
            }
            catch (Exception ex)
            {
              Console.WriteLine("Error in public API: " + ex);
              return Results.Problem("Failed to call public API", statusCode: 500);
            }
        })
        .WithName("GetPublicData");

        /// <summary>
        /// Protected endpoint that requires an M2M access token for access.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <returns>A success response indicating access to the protected API.</returns>
        routes.MapGet("/api/protected/data", (HttpContext context) =>
        {
            return Results.Ok("Hello from Protected API!");
        })
        .WithName("GetProtectedData")
        .RequireAuthorization();
    }
}
