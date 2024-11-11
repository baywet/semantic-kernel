﻿// Copyright (c) Microsoft. All rights reserved.
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.SemanticKernel.Plugins.OpenApi.Extensions;

/// <summary>
/// Microsoft manifest plugin parameters.
/// </summary>
public sealed class MicrosoftManifestPluginParameters
{
    /// <summary>
    /// Gets the HTTP client to be used in plugin initialization phase.
    /// </summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>
    /// Gets the user agent to be used in plugin initialization phase.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// A map of function execution parameters, where the key is the api dependency key from api manifest
    /// and the value is OpenApiFunctionExecutionParameters specific to that dependency.
    /// </summary>
    public Dictionary<string, OpenApiFunctionExecutionParameters>? FunctionExecutionParameters { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftManifestPluginParameters"/> class.
    /// </summary>
    /// <param name="httpClient">Http client to be used in plugin initialization phase.</param>
    /// <param name="userAgent">User agent to be used in plugin initialization phase.</param>
    /// <param name="functionExecutionParameters">A map of function execution parameters.</param>
    public MicrosoftManifestPluginParameters(
        HttpClient? httpClient = default,
        string? userAgent = default,
        Dictionary<string, OpenApiFunctionExecutionParameters>? functionExecutionParameters = default
    )
    {
        this.HttpClient = httpClient;
        this.UserAgent = userAgent;
        this.FunctionExecutionParameters = functionExecutionParameters;
    }
}
