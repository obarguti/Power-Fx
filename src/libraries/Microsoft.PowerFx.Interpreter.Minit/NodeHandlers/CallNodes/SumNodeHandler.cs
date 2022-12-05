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
    internal class SumNodeHandler : INodeHandler
    {
        public bool CanHandle(IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;
            return callNode?.Function?.Name == "Sum";
        }

        public VisitorResult Handle(MinitVisitor visitor, IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;

            string arg0 = null;
            if (callNode.Args[0] is ResolvedObjectNode node2)
            {
                if (node2.Value is ISymbolSlot slot)
                {
                    if (context._parent._slotAllEvents.Any(s => Equals(slot, s)))
                    {
                        arg0 = "ProcessEvents"; // Minit identifier. 
                    }
                }
            }

            if (arg0 == null)
            {
                throw new NotImplementedException($"Unrecognized arg {callNode.Args[0].IRContext.SourceContext}");
            }

            var arg1 = callNode.Args[1].Accept<VisitorResult, VisitorContext>(visitor, context);

            var ret = new VisitorResult
            {
                _miniExpr = $"Sum({arg0}, {arg1._miniExpr})"
            };

            return ret;
        }
    }
}
