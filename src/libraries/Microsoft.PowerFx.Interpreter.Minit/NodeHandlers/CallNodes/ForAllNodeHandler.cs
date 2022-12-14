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
    internal class ForAllNodeHandler : INodeHandler
    {
        // GroupBy(<Process|View|BusinessRule>, <Cases|Events|Edges>, <Max|Min|Count|...etc>(<Formula>))
        private readonly string _traslation = "forall";

        public bool CanHandle(IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;
            return callNode.Function.Name.ToLower().Equals(_traslation);
        }

        public VisitorResult Handle(MinitVisitor visitor, IntermediateNode node, VisitorContext context)
        {
            var callNode = node as CallNode;

            if (callNode.Args[0] is not CallNode arg0 || callNode.Args[1] is not CallNode arg1)
            {
                throw new ArgumentNullException($"Argument for {callNode.Function.Name} must be a Call Node");
            }

            var arg0Node = callNode.Args[0] as CallNode;

            switch (arg0Node.Function.Name)
            {
                case "groupby":
                    // handle groupby
                    break;
                default:
                    throw new ArgumentException($"First Argument for {callNode.Function.Name} is not supported");
            }

            var arg1Result = callNode.Args[1].Accept(visitor, context);

            var ret = new VisitorResult
            {
                _miniExpr = $"forall({arg1Result._miniExpr})"
            };

            return ret;
        }
    }
}
