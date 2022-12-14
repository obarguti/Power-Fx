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
    internal class AggregateNodeHandler : INodeHandler
    {
        private readonly IDictionary<string, string> _functionTranslations = new Dictionary<string, string> 
        {
            { "average", "TBD" },
            { "max", "TBD" },
            { "min", "TBD" },
            { "stdevp", "TBD" },
            { "sum", "sum" },
            { "varp", "TBD" } 
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

            string arg0 = null;
            if (callNode.Args[0] is ResolvedObjectNode node2)
            {
                if (node2.Value is ISymbolSlot slot)
                {
                    var symbolSlot = context._parent._slotAllEvents.SingleOrDefault(s => Equals(slot, s));

                    if (symbolSlot is not null and NameSymbol)
                    {
                        arg0 = ((NameSymbol)symbolSlot).Name;
                    }
                }
            }

            if (arg0 == null)
            {
                throw new NotImplementedException($"Unrecognized arg {callNode.Args[0].IRContext.SourceContext}");
            }

            var arg1 = callNode.Args[1].Accept(visitor, context);

            var ret = new VisitorResult
            {
                _miniExpr = $"{translatedFunction}({arg0}, {arg1._miniExpr})"
            };

            return ret;
        }
    }
}
