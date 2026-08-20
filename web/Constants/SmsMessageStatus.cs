namespace web.Constants
{
    /// <summary>
    /// Locally-controlled SMS message statuses, used before/independent of the gateway's own status string.
    /// </summary>
    public static class SmsMessageStatus
    {
        /// <summary>Outbound message queued locally, not yet handed off to the SMS gateway.</summary>
        public const string Pending = "Pending";

        /// <summary>Accepted by the SMS gateway but not sent yet.</summary>
        public const string Queued = "Queued";

        /// <summary>Confirmed sent by the SMS gateway.</summary>
        public const string Sent = "Sent";

        /// <summary>Sending to the SMS gateway failed; see FailureReason for details.</summary>
        public const string Failed = "Failed";
    }
}
