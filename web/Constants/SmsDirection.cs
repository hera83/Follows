namespace web.Constants
{
    /// <summary>
    /// Direction of an SMS message relative to this application.
    /// </summary>
    public static class SmsDirection
    {
        /// <summary>Message sent from this application to a recipient.</summary>
        public const string Outbound = "Outbound";

        /// <summary>Message received by this application from a sender.</summary>
        public const string Inbound = "Inbound";
    }
}
