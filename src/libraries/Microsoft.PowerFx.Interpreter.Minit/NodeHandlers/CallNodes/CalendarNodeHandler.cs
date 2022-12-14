// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;
using static Microsoft.PowerFx.Syntax.PrettyPrintVisitor;

namespace Microsoft.PowerFx.Interpreter.Minit.NodeHandlers.CallNodes
{
    internal class CalendarNodeHandler : INodeHandler
    {
        private readonly IDictionary<string, string> _functionTranslations = new Dictionary<string, string>
        {
            { "calendar", "TBD" },
            { "clock", "TBD" }
        };

        public bool CanHandle(IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;
            return _functionTranslations.Keys.Contains(callNode?.Function?.Name.ToLower());
        }

        public VisitorResult Handle(MinitVisitor visitor, IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;
            
            if (!_functionTranslations.TryGetValue(callNode.Function.Name, out var translatedFunction))
            {
                throw new NotImplementedException($"Unrecognized function {callNode.Function.Name}");
            }

            if (callNode.Args[0] == null)
            {
                throw new ArgumentNullException($"Argument 1 for {callNode.Function.Name} cannot be null");
            }

            var arg1Result = callNode.Args[1].Accept(visitor, context);

            var ret = new VisitorResult
            {
                _miniExpr = $"{translatedFunction}({arg1Result._miniExpr})"
            };

            return ret;
        }
    }
}
