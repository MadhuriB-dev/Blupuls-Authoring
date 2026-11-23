using System;

namespace Bluepuls_Authoring_Tool.Models
{
    public class DiagnosticLogItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Direction { get; set; } = "TX";
        public string ServiceId { get; set; } = "22 F1 90";
        public string Description { get; set; } = "ReadDataByIdentifier_ECUSerialNumber";
        public string RawData { get; set; } = "22 F1 90";
        public string Status { get; set; } = "OK";
    }
}