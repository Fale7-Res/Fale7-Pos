using System;
using System.Runtime.Serialization;

namespace Fale7_POS.Infrastructure.Sync
{
    public enum SyncEntityType
    {
        Order,
        Inventory,
        Address
    }

    public enum SyncOperationType
    {
        Insert,
        Update,
        Delete
    }

    /// <summary>
    /// Immutable data wrapper containing the entity and its exact idempotency state.
    /// Ensures conflict-free sync for all data types across multiple devices.
    /// </summary>
    [DataContract]
    public class SyncPayloadContract<T>
    {
        [DataMember(Name = "idempotency_key")]
        public string IdempotencyKey { get; private set; }

        [DataMember(Name = "entity_type")]
        public SyncEntityType EntityType { get; private set; }

        [DataMember(Name = "operation_type")]
        public SyncOperationType OperationType { get; private set; }

        [DataMember(Name = "payload")]
        public T Payload { get; private set; }

        [DataMember(Name = "created_at_utc")]
        public DateTime CreatedAtUtc { get; private set; }

        public SyncPayloadContract(string idempotencyKey, SyncEntityType entityType, SyncOperationType operationType, T payload)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key is required to guarantee conflict-free synchronization.", nameof(idempotencyKey));

            IdempotencyKey = idempotencyKey;
            EntityType = entityType;
            OperationType = operationType;
            Payload = payload;
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
