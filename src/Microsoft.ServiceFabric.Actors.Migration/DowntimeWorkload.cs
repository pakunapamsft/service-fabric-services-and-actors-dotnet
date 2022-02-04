// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Migration
{
    using System.Fabric;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    internal class DowntimeWorkload : MigrationPhaseWorkloadBase
    {
        public DowntimeWorkload(
            KVStoRCMigrationActorStateProvider stateProvider,
            ServicePartitionClient<HttpCommunicationClient> servicePartitionClient,
            StatefulServiceContext statefulServiceContext,
            MigrationSettings migrationSettings,
            StatefulServiceInitializationParameters initParams,
            ActorTypeInformation actorTypeInfo,
            string traceId)
            : base(MigrationPhase.Downtime, 1, 1, stateProvider, servicePartitionClient, statefulServiceContext, migrationSettings, initParams, actorTypeInfo, traceId)
        {
        }
    }
}
