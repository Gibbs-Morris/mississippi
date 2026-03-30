global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;
global using System.Net;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

global using Aspire.Hosting;
global using Aspire.Hosting.Testing;

global using FluentAssertions;

global using Microsoft.Azure.Cosmos;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

global using Mississippi.DomainModeling.ReplicaSinks.Abstractions;
global using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions;
global using Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos;

global using Projects;

global using Xunit;
global using Xunit.Abstractions;

global using ReplicaSinkCosmosCalibrationAppHost = Projects.DomainModeling_ReplicaSinks_Runtime_Storage_Cosmos_L2Tests_AppHost;
