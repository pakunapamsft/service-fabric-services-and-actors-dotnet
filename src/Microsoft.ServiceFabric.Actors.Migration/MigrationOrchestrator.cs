// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Migration
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors.Generator;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using static Microsoft.ServiceFabric.Actors.Migration.MigrationConstants;
    using static Microsoft.ServiceFabric.Actors.Migration.MigrationUtility;

    internal class MigrationOrchestrator : IMigrationOrchestrator
    {
        private static readonly string TraceType = typeof(MigrationOrchestrator).Name;
        private KVStoRCMigrationActorStateProvider stateProvider;
        private ActorTypeInformation actorTypeInfo;
        private IReliableDictionary2<string, string> metadataDict;
        private StatefulServiceInitializationParameters initParams;
        private ServicePartitionClient<HttpCommunicationClient> servicePartitionClient;
        private StatefulServiceContext serviceContext;
        private MigrationSettings migrationSettings;
        private string traceId;

        public MigrationOrchestrator(IActorStateProvider stateProvider, ActorTypeInformation actorTypeInfo, StatefulServiceContext serviceContext)
        {
            this.stateProvider = stateProvider as KVStoRCMigrationActorStateProvider;
            this.actorTypeInfo = actorTypeInfo;
            this.initParams = this.stateProvider.GetInitParams();
            this.migrationSettings = MigrationSettings.LoadFrom(
                this.initParams.CodePackageActivationContext,
                ActorNameFormat.GetMigrationConfigSectionName(this.actorTypeInfo.ImplementationType));
            this.serviceContext = serviceContext;
            this.traceId = ActorTrace.GetTraceIdForReplica(this.initParams.PartitionId, this.initParams.ReplicaId);

            var partitionInformation = this.stateProvider.StatefulServicePartition.PartitionInfo as Int64RangePartitionInformation;
            this.servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                    new HttpCommunicationClientFactory(null, new List<IExceptionHandler>() { new HttpExceptionHandler() }),
                    this.migrationSettings.KVSActorServiceUri,
                    new ServicePartitionKey(partitionInformation.LowKey),
                    TargetReplicaSelector.PrimaryReplica,
                    KVSMigrationListenerName);
        }

        public Data.ITransaction Transaction { get => this.stateProvider.GetStateManager().CreateTransaction(); }

        public IReliableDictionary2<string, string> MetaDataDictionary { get => this.metadataDict; }

        public ServicePartitionClient<HttpCommunicationClient> ServicePartitionClient { get => this.servicePartitionClient; }

        public StatefulServiceContext StatefulServiceContext { get => this.serviceContext; }

        public MigrationSettings MigrationSettings { get => this.migrationSettings; }

        public StatefulServiceInitializationParameters StatefulServiceInitializationParameters { get => this.initParams; }

        public KVStoRCMigrationActorStateProvider StateProvider { get => this.stateProvider; }

        public ActorTypeInformation ActorTypeInformation { get => this.actorTypeInfo; }

        public string TraceId { get => this.traceId; }

        public async Task StartMigrationAsync(CancellationToken cancellationToken)
        {
            int i = 0;
            while (i == 0)
            {
                Thread.Sleep(5000);
            }

            ActorTrace.Source.WriteInfoWithId(
                TraceType,
                this.TraceId,
                "Starting Migration.");
            await this.InitializeIfRequiredAsync(cancellationToken);
            IMigrationPhaseWorkload workloadRunner = null;

            try
            {
                workloadRunner = await this.NextWorkloadRunnerAsync(MigrationPhase.None, cancellationToken);

                PhaseResult currentResult = null;
                while (workloadRunner != null)
                {
                    currentResult = await workloadRunner.StartOrResumeMigrationAsync(cancellationToken);
                    workloadRunner = await this.NextWorkloadRunnerAsync(currentResult, cancellationToken);
                }

                if (currentResult != null)
                {
                    await this.CompleteMigrationAsync(currentResult, cancellationToken);

                    ActorTrace.Source.WriteInfoWithId(
                        TraceType,
                        this.TraceId,
                        $"Migration successfully completed - {currentResult.ToString()}");
                }
            }
            catch (Exception e)
            {
                var currentPhase = workloadRunner != null ? workloadRunner.Phase : MigrationPhase.None;
                ActorTrace.Source.WriteErrorWithId(
                    TraceType,
                    this.TraceId,
                    $"Migration {currentPhase} Phase failed with error: {e}");

                throw e;
            }
        }

        public async Task InvokeResumeWritesAsync(CancellationToken cancellationToken)
        {
            await this.ServicePartitionClient.InvokeWithRetryAsync(
                async client =>
                {
                    return await client.HttpClient.PutAsync($"{KVSMigrationControllerName}/{ResumeWritesAPIEndpoint}", null);
                },
                cancellationToken);
        }

        public async Task<MigrationResult> GetResultAsync(CancellationToken cancellationToken)
        {
            var startSN = await this.GetStartSequenceNumberAsync(cancellationToken);
            var endSN = await this.GetStartSequenceNumberAsync(cancellationToken);
            using (var tx = this.Transaction)
            {
                var status = await ParseMigrationStateAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                    tx,
                    MigrationCurrentStatus,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId);
                if (status == MigrationState.None)
                {
                    return new MigrationResult
                    {
                        CurrentPhase = MigrationPhase.None,
                        Status = MigrationState.None,
                        StartSeqNum = startSN,
                        EndSeqNum = endSN,
                    };
                }

                var result = new MigrationResult
                {
                    Status = status,
                    EndSeqNum = endSN,
                };

                result.CurrentPhase = await ParseMigrationPhaseAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                    tx,
                    MigrationCurrentPhase,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId);

                result.StartDateTimeUTC = (await ParseDateTimeAsync(
                    () => this.MetaDataDictionary.GetAsync(
                    tx,
                    MigrationStartDateTimeUTC,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId)).Value;

                result.EndDateTimeUTC = await ParseDateTimeAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                    tx,
                    MigrationEndDateTimeUTC,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId);

                result.StartSeqNum = (await ParseLongAsync(
                    () => this.MetaDataDictionary.GetAsync(
                    tx,
                    MigrationStartSeqNum,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId)).Value;

                result.LastAppliedSeqNum = await ParseLongAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                    tx,
                    MigrationLastAppliedSeqNum,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId);

                result.NoOfKeysMigrated = await ParseLongAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                    tx,
                    MigrationNoOfKeysMigrated,
                    DefaultRCTimeout,
                    cancellationToken),
                    this.traceId);

                var currentPhase = MigrationPhase.Copy;
                var phaseResults = new List<PhaseResult>();
                while (currentPhase <= result.CurrentPhase)
                {
                    var currentIteration = await ParseIntAsync(
                    () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                        tx,
                        Key(PhaseIterationCount, MigrationPhase.Catchup),
                        DefaultRCTimeout,
                        cancellationToken),
                    1,
                    this.TraceId);
                    for (int i = 1; i <= currentIteration; i++)
                    {
                        phaseResults.Add(await MigrationPhaseWorkloadBase.GetResultAsync(this.MetaDataDictionary, tx, currentPhase, i, this.traceId, cancellationToken));
                    }

                    currentPhase++;
                }

                await tx.CommitAsync();
                result.PhaseResults = phaseResults.ToArray();

                return result;
            }
        }

        private async Task CompleteMigrationAsync(PhaseResult result, CancellationToken cancellationToken)
        {
            using (var tx = this.Transaction)
            {
                await this.metadataDict.TryAddAsync(
                    tx,
                    MigrationEndDateTimeUTC,
                    DateTime.UtcNow.ToString(),
                    DefaultRCTimeout,
                    cancellationToken);

                await this.metadataDict.TryAddAsync(
                    tx,
                    MigrationEndSeqNum,
                    result.EndSeqNum.ToString(),
                    DefaultRCTimeout,
                    cancellationToken);

                await tx.CommitAsync();
            }
        }

        private async Task<long> GetEndSequenceNumberAsync(CancellationToken cancellationToken)
        {
            return (await ParseLongAsync(
                () => this.ServicePartitionClient.InvokeWithRetryAsync<string>(
                async client =>
                {
                    return await client.HttpClient.GetStringAsync($"{KVSMigrationControllerName}/{GetEndSNEndpoint}");
                },
                cancellationToken),
                this.TraceId)).Value;
        }

        private async Task<long> GetStartSequenceNumberAsync(CancellationToken cancellationToken)
        {
            return (await ParseLongAsync(
                () => this.ServicePartitionClient.InvokeWithRetryAsync<string>(
                async client =>
                {
                    return await client.HttpClient.GetStringAsync($"{KVSMigrationControllerName}/{GetStartSNEndpoint}");
                },
                cancellationToken),
                this.TraceId)).Value;
        }

        private async Task InvokeRejectWritesAsync(CancellationToken cancellationToken)
        {
            await this.ServicePartitionClient.InvokeWithRetryAsync(
                async client =>
                {
                    return await client.HttpClient.PutAsync($"{KVSMigrationControllerName}/{RejectWritesAPIEndpoint}", null);
                },
                cancellationToken);
        }

        private async Task<IMigrationPhaseWorkload> NextWorkloadRunnerAsync(PhaseResult currentResult, CancellationToken cancellationToken)
        {
            var endSN = await this.GetEndSequenceNumberAsync(cancellationToken);
            var delta = endSN - currentResult.EndSeqNum;
            if (currentResult.Phase == MigrationPhase.Catchup)
            {
                if (delta > this.MigrationSettings.DowntimeThreshold)
                {
                    return await this.NextWorkloadRunnerAsync(MigrationPhase.Catchup, cancellationToken);
                }

                await this.InvokeRejectWritesAsync(cancellationToken);
            }

            return await this.NextWorkloadRunnerAsync(currentResult.Phase + 1, cancellationToken);
        }

        private async Task<IMigrationPhaseWorkload> NextWorkloadRunnerAsync(MigrationPhase currentPhase, CancellationToken cancellationToken)
        {
            IMigrationPhaseWorkload migrationWorkload = null;
            using (var tx = this.Transaction)
            {
                if (currentPhase == MigrationPhase.None || currentPhase == MigrationPhase.Copy)
                {
                    migrationWorkload = new CopyPhaseWorkload(
                        this.StateProvider,
                        this.servicePartitionClient,
                        this.StatefulServiceContext,
                        this.MigrationSettings,
                        this.StatefulServiceInitializationParameters,
                        this.ActorTypeInformation,
                        this.TraceId);
                }
                else if (currentPhase == MigrationPhase.Catchup)
                {
                    var currentIteration = await ParseIntAsync(
                    () => this.MetaDataDictionary.GetOrAddAsync(
                        tx,
                        Key(PhaseIterationCount, MigrationPhase.Catchup),
                        "1",
                        DefaultRCTimeout,
                        cancellationToken),
                    this.TraceId);

                    var status = await ParseMigrationStateAsync(
                        () => this.MetaDataDictionary.GetValueOrDefaultAsync(
                        tx,
                        Key(PhaseCurrentStatus, MigrationPhase.Catchup, currentIteration),
                        DefaultRCTimeout,
                        cancellationToken),
                        this.traceId);
                    if (status == MigrationState.Completed)
                    {
                        currentIteration++;
                    }

                    migrationWorkload = new CatchupPhaseWorkload(
                        currentIteration,
                        this.StateProvider,
                        this.servicePartitionClient,
                        this.StatefulServiceContext,
                        this.MigrationSettings,
                        this.StatefulServiceInitializationParameters,
                        this.ActorTypeInformation,
                        this.TraceId);
                }
                else if (currentPhase == MigrationPhase.Downtime)
                {
                    migrationWorkload = new DowntimeWorkload(
                        this.StateProvider,
                        this.servicePartitionClient,
                        this.StatefulServiceContext,
                        this.MigrationSettings,
                        this.StatefulServiceInitializationParameters,
                        this.ActorTypeInformation,
                        this.TraceId);
                }

                await tx.CommitAsync();

                return migrationWorkload;
            }
        }

        private async Task InitializeIfRequiredAsync(CancellationToken cancellationToken)
        {
            this.metadataDict = await this.stateProvider.GetMetadataDictionaryAsync();
            using (var tx = this.Transaction)
            {
                await this.MetaDataDictionary.TryAddAsync(
                    tx,
                    MigrationStartDateTimeUTC,
                    DateTime.UtcNow.ToString(),
                    DefaultRCTimeout,
                    cancellationToken);

                await this.MetaDataDictionary.TryAddAsync(
                    tx,
                    MigrationCurrentStatus,
                    MigrationState.InProgress.ToString(),
                    DefaultRCTimeout,
                    cancellationToken);

                await this.MetaDataDictionary.TryAddAsync(
                    tx,
                    MigrationCurrentPhase,
                    MigrationPhase.None.ToString(),
                    DefaultRCTimeout,
                    cancellationToken);

                await tx.CommitAsync();
            }
        }
    }
}