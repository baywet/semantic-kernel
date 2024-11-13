﻿// Copyright (c) Microsoft. All rights reserved.

using System.Web;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.MsGraph.Connectors.CredentialManagers;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel.Plugins.OpenApi.Extensions;

namespace Plugins;
public class MicrosoftManifestBasedPlugins(ITestOutputHelper output) : BaseTest(output)
{
    public static readonly IEnumerable<object[]> s_parameters =
    [
        // function names are sanitized operationIds from the OpenAPI document
        ["MessagesPlugin", "me_ListMessages", new KernelArguments { { "_top", "1" } }, "MessagesPlugin"],
        ["DriveItemPlugin", "drive_root_GetChildrenContent", new KernelArguments { { "driveItem-Id", "test.txt" } }, "DriveItemPlugin", "MessagesPlugin"],
        ["ContactsPlugin", "me_ListContacts", new KernelArguments() { { "_count", "true" } }, "ContactsPlugin", "MessagesPlugin"],
        ["CalendarPlugin", "me_calendar_ListEvents", new KernelArguments() { { "_top", "1" } }, "CalendarPlugin", "MessagesPlugin"],

        // Multiple API dependencies (multiple auth requirements) scenario within the same plugin
        // Graph API uses MSAL
        ["AstronomyPlugin", "me_ListMessages", new KernelArguments { { "_top", "1" } }, "AstronomyPlugin"],
        // Astronomy API uses API key authentication
        ["AstronomyPlugin", "apod", new KernelArguments { { "_date", "2022-02-02" } }, "AstronomyPlugin"],
    ];
    [Theory, MemberData(nameof(s_parameters))]
    public async Task RunMicrosoftManifestPluginAsync(string pluginToTest, string functionToTest, KernelArguments? arguments, params string[] pluginsToLoad)
    {
        WriteSampleHeadingToConsole(pluginToTest, functionToTest, arguments, pluginsToLoad);
        var kernel = new Kernel();
        await AddMicrosoftManifestPluginsAsync(kernel, pluginsToLoad);

        var result = await kernel.InvokeAsync(pluginToTest, functionToTest, arguments);
        Console.WriteLine("--------------------");
        Console.WriteLine($"\nResult:\n{result}\n");
        Console.WriteLine("--------------------");
    }

    private void WriteSampleHeadingToConsole(string pluginToTest, string functionToTest, KernelArguments? arguments, params string[] pluginsToLoad)
    {
        Console.WriteLine();
        Console.WriteLine("======== [MicrosoftManifest Plugins Sample] ========");
        Console.WriteLine($"======== Loading Plugins: {string.Join(" ", pluginsToLoad)} ========");
        Console.WriteLine($"======== Calling Plugin Function: {pluginToTest}.{functionToTest} with parameters {arguments?.Select(x => x.Key + " = " + x.Value).Aggregate((x, y) => x + ", " + y)} ========");
        Console.WriteLine();
    }
    private async Task AddMicrosoftManifestPluginsAsync(Kernel kernel, params string[] pluginNames)
    {
#pragma warning disable SKEXP0050
        if (TestConfiguration.MSGraph.Scopes is null)
        {
            throw new InvalidOperationException("Missing Scopes configuration for Microsoft Graph API.");
        }

        LocalUserMSALCredentialManager credentialManager = await LocalUserMSALCredentialManager.CreateAsync().ConfigureAwait(false);

        var token = await credentialManager.GetTokenAsync(
                        TestConfiguration.MSGraph.ClientId,
                        TestConfiguration.MSGraph.TenantId,
                        TestConfiguration.MSGraph.Scopes.ToArray(),
                        TestConfiguration.MSGraph.RedirectUri).ConfigureAwait(false);
#pragma warning restore SKEXP0050

        BearerAuthenticationProviderWithCancellationToken authenticationProvider = new(() => Task.FromResult(token));
#pragma warning disable SKEXP0040

        // Microsoft Graph API execution parameters
        var graphOpenApiFunctionExecutionParameters = new OpenApiFunctionExecutionParameters(
            authCallback: authenticationProvider.AuthenticateRequestAsync,
            serverUrlOverride: new Uri("https://graph.microsoft.com/v1.0"));

        // NASA API execution parameters
        var nasaOpenApiFunctionExecutionParameters = new OpenApiFunctionExecutionParameters(
            authCallback: async (request, cancellationToken) =>
            {
                var uriBuilder = new UriBuilder(request.RequestUri ?? throw new InvalidOperationException("The request URI is null."));
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["api_key"] = "DEMO_KEY";
                uriBuilder.Query = query.ToString();
                request.RequestUri = uriBuilder.Uri;
            });

        var apiManifestPluginParameters = new MicrosoftManifestPluginParameters(
            functionExecutionParameters: new()
            {
                { "https://graph.microsoft.com/v1.0", graphOpenApiFunctionExecutionParameters },
                { "https://api.nasa.gov/planetary", nasaOpenApiFunctionExecutionParameters }
            });
        var manifestLookupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Resources", "Plugins", "MicrosoftManifestPlugins");

        foreach (var pluginName in pluginNames)
        {
            try
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                await kernel.ImportPluginFromMicrosoftManifestAsync(
                    pluginName,
                    Path.Combine(manifestLookupDirectory, pluginName, $"{pluginName[..^6].ToLowerInvariant()}-apiplugin.json"),
                    apiManifestPluginParameters)
                    .ConfigureAwait(false);
#pragma warning restore CA1308 // Normalize strings to uppercase
                Console.WriteLine($">> {pluginName} is created.");
#pragma warning restore SKEXP0040
            }
            catch (Exception ex)
            {
                Console.WriteLine("Plugin creation failed. Message: {0}", ex.Message);
                throw new AggregateException($"Plugin creation failed for {pluginName}", ex);
            }
        }
    }
}
