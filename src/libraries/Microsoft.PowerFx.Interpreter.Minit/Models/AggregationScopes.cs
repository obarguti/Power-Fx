// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.PowerFx.Interpreter.Minit.Models
{
    internal static class AggregationScopes
    {
        // Builtin Identifiers
        // PowerFx can have identifiers that represent filters. 
        public const string AllIView = "AllIView";
        public const string ViewCases = "ViewCases";
        public const string ViewEvents = "ViewEvents";
        public const string ViewEdges = "ViewEdges";
        public const string CasesPerAttribute = "CasesPerAttribute";
        public const string EventsPerAttribute = "EventsPerAttribute";
        public const string EdgesPerAttribute = "EdgesPerAttribute";
        public const string CaseEvents = "CaseEvents";
        public const string CaseEdges = "CaseEdges";
        public const string AllInBusinessRule = "AllInBusinessRule";
        public const string BusinessRuleCases = "BusinessRuleCases";
        public const string BusinessRuleEvents = "BusinessRuleEvents";
        public const string AllInProcess = "AllInProcess";
        public const string ProcessCases = "ProcessCases";
        public const string ProcessEvents = "ProcessEvents";
        public const string ProcessEdges = "ProcessEdges";
    }
}
