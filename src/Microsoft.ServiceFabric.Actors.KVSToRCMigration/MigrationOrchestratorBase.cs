// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.KVSToRCMigration
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Actors.Generator;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Actors.Runtime.Migration;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    /// <summary>
    /// Base class for migration orchestration.
    /// </summary>
    internal abstract class MigrationOrchestratorBase : IMigrationOrchestrator
    {
        private static readonly string TraceType = typeof(MigrationOrchestratorBase).Name;

        private StatefulServiceContext serviceContext;
        private ActorTypeInformation actorTypeInformation;
        private string traceId;
        private Action<bool> stateProviderStateChangeCallback;
        private MigrationSettings migrationSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationOrchestratorBase"/> class.
        /// </summary>
        /// <param name="actorTypeInformation">The type information of the Actor.</param>
        /// <param name="serviceContext">Service context the actor service is operating under.</param>
        public MigrationOrchestratorBase(StatefulServiceContext serviceContext, ActorTypeInformation actorTypeInformation)
        {
            this.actorTypeInformation = actorTypeInformation;
            this.serviceContext = serviceContext;
            this.traceId = this.serviceContext.TraceId;
            this.migrationSettings = new MigrationSettings();
            this.migrationSettings.LoadFrom(
                 this.StatefulServiceContext.CodePackageActivationContext,
                 ActorNameFormat.GetMigrationConfigSectionName(this.actorTypeInformation.ImplementationType));
        }

        internal ActorTypeInformation ActorTypeInformation { get => this.actorTypeInformation; }

        internal StatefulServiceContext StatefulServiceContext { get => this.serviceContext; }

        internal string TraceId { get => this.traceId; }

        internal Action<bool> StateProviderStateChangeCallback { get => this.stateProviderStateChangeCallback; }

        internal MigrationSettings MigrationSettings { get => this.migrationSettings; }

        /// <inheritdoc/>
        public abstract Task<bool> AreActorCallsAllowedAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract IActorStateProvider GetMigrationActorStateProvider();

        /// <inheritdoc/>
        public ICommunicationListener GetMigrationCommunicationListener()
        {
            var endpointName = this.GetMigrationEndpointName();

            return new KestrelCommunicationListener(this.serviceContext, endpointName, (url, listener) =>
            {
                try
                {
                    var endpoint = this.serviceContext.CodePackageActivationContext.GetEndpoint(endpointName);

                    ActorTrace.Source.WriteInfoWithId(
                        TraceType,
                        this.TraceId,
                        $"Starting Kestrel on url: {url} host: {FabricRuntime.GetNodeContext().IPAddressOrFQDN} endpointPort: {endpoint.Port}");

                    var webHostBuilder =
                        new WebHostBuilder()
                            .UseKestrel()
                            .ConfigureServices(
                                services => services
                                    .AddSingleton<IMigrationOrchestrator>(this))
                            .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                            .UseStartup<Startup>()
                            .UseUrls(url)
                            .Build();

                    return webHostBuilder;
                }
                catch (Exception ex)
                {
                    ActorTrace.Source.WriteErrorWithId(
                        TraceType,
                        this.TraceId,
                        "Error encountered while creating WebHostBuilder: " + ex);

                    throw;
                }
            });
        }

        /// <inheritdoc/>
        public virtual void RegisterStateChangeCallback(Action<bool> stateProviderStateChangeCallback)
        {
            this.stateProviderStateChangeCallback = stateProviderStateChangeCallback;
        }

        /// <inheritdoc/>
        public abstract Task StartDowntimeAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task StartMigrationAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task AbortMigrationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the migration endpoint name.
        /// </summary>
        /// <returns>Migration endpoint name.</returns>
        protected abstract string GetMigrationEndpointName();
    }
}