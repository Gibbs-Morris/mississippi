// <copyright file="GlobalUsings.cs" company="Gibbs-Morris LLC">
// Licensed under the Gibbs-Morris commercial license.
// </copyright>

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Net.Http;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
global using Aspire.Hosting.Testing;

global using Azure.Storage.Blobs;

global using FluentAssertions;

global using Microsoft.Azure.Cosmos;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using Xunit;

// Type aliases to resolve ambiguity between Azure and Cosmos SDK types (alphabetical order per SA1211)
global using AzureBlobContentInfo = Azure.Response<Azure.Storage.Blobs.Models.BlobContentInfo>;
global using AzureBlobDownloadResult = Azure.Response<Azure.Storage.Blobs.Models.BlobDownloadResult>;
global using AzureBlobItem = Azure.Pageable<Azure.Storage.Blobs.Models.BlobItem>;
global using AzureResponse = Azure.Response;
global using AzureResponseBool = Azure.Response<bool>;