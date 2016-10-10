using System;
using System.Linq;

namespace Portal.CodeAnalyzer.Database
{
    public static class DiagnosticIds
    {
        public const string DalManagerAccess = "DalManagerAccess";
        public const string DalCommit = "DalCommit";
        public const string DetectDbCallsInConstructor = "DetectDbCallsInConstructor";
    }
}