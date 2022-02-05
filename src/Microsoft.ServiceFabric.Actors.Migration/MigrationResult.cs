// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Migration
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;

    [DataContract]
    internal class MigrationResult
    {
        private static DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MigrationResult), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
        });

        [DataMember]
        public DateTime StartDateTimeUTC { get; set; }

        [DataMember]
        public DateTime? EndDateTimeUTC { get; set; }

        [DataMember]
        public long StartSeqNum { get; set; }

        [DataMember]
        public long EndSeqNum { get; set; }

        [DataMember]
        public long? LastAppliedSeqNum { get; set; }

        [DataMember]
        public MigrationState Status { get; set; }

        [DataMember]
        public int WorkerCount { get; set; }

        [DataMember]
        public int IterationCount { get; set; }

        [DataMember]
        public long? NoOfKeysMigrated { get; set; }

        [DataMember]
        public MigrationPhase Phase { get; set; }

        [DataMember]
        public WorkerResult[] WorkerResults { get; set; }

        public override string ToString()
        {
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);

                var returnVal = Encoding.ASCII.GetString(stream.GetBuffer());

                return returnVal;
            }
        }

        [DataContract]
        public class WorkerResult
        {
            [DataMember]
            public int WorkerId { get; set; }

            [DataMember]
            public int Iteration { get; set; }

            [DataMember]
            public DateTime StartDateTimeUTC { get; set; }

            [DataMember]
            public DateTime? EndDateTimeUTC { get; set; }

            [DataMember]
            public long StartSeqNum { get; set; }

            [DataMember]
            public long EndSeqNum { get; set; }

            [DataMember]
            public long? LastAppliedSeqNum { get; set; }

            [DataMember]
            public MigrationPhase Phase { get; set; }

            [DataMember]
            public MigrationState Status { get; set; }

            [DataMember]
            public long? NoOfKeysMigrated { get; set; }
        }
    }
}
