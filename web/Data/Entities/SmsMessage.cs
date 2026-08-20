using web.Constants;

namespace web.Data.Entities
{
    /// <summary>
    /// A single SMS message sent or received through the SMS gateway service.
    /// </summary>
    public class SmsMessage
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Message ID assigned by the SMS gateway, when known.
        /// </summary>
        public Guid? GatewayMessageId { get; set; }

        /// <summary>
        /// Inbound or Outbound, see <see cref="SmsDirection"/>.
        /// </summary>
        public string Direction { get; set; } = SmsDirection.Outbound;

        /// <summary>
        /// The other party's phone number (recipient for outbound, sender for inbound).
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Message text content.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Current status. Starts at <see cref="SmsMessageStatus.Pending"/> for outbound messages
        /// not yet sent, then mirrors whatever the SMS gateway reports (e.g. Queued, Sent, Received)
        /// or <see cref="SmsMessageStatus.Failed"/> if sending failed.
        /// </summary>
        public string Status { get; set; } = SmsMessageStatus.Pending;

        public int SegmentCount { get; set; }

        public decimal? UnitPriceDkk { get; set; }

        public decimal? TotalPriceDkk { get; set; }

        /// <summary>
        /// Set when Status indicates a failure.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// UTC timestamp when this record was created locally.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when this record was last updated.
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>
        /// UTC timestamp when an outbound message was sent by the gateway.
        /// </summary>
        public DateTime? SentAtUtc { get; set; }

        /// <summary>
        /// UTC timestamp when an outbound message failed.
        /// </summary>
        public DateTime? FailedAtUtc { get; set; }

        /// <summary>
        /// UTC timestamp when an inbound message was received.
        /// </summary>
        public DateTime? ReceivedAtUtc { get; set; }
    }
}
