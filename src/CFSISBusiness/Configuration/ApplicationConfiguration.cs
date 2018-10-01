using System;
using System.Collections.Generic;
using System.Text;

namespace CFSISBusiness.Configuration
{
    /// <summary>
    /// Application Configuration class that holds application settings.
    /// This configuration can be injected with IOptions<ApplicationConfiguration>
    /// using the "Application" subkey in appSettings.json.
    /// </summary>
    public class ApplicationConfiguration
    {
        public string ApplicationName { get; set; } = "Chennai Foundation Students Information System";
        public int MaxListItems { get; set; } = 10;

        public EmailConfiguration EmailSettings { get; set; } = new EmailConfiguration();
    }

    public class EmailConfiguration
    {
        public string MailServer { get; set; } = "localhost";
        public bool UseTls { get; set; } 
        public string MailServerUsername { get; set; }
        public string MailServerPassword { get; set; }

        public string SenderAddress { get; set; } = "";
        public string SenderName { get; set; } = "Chennai Foundation Administration";
    }
}
