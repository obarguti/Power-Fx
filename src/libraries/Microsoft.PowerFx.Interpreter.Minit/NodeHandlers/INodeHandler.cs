// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.PowerFx.Core.IR.Nodes;

namespace Microsoft.PowerFx.Interpreter.Minit.NodeHandlers
{
    internal interface INodeHandler
    {
        bool CanHandle(IntermediateNode node, VisitorContext context);
        
        VisitorResult Handle(MinitVisitor visitor, IntermediateNode node, VisitorContext context);
    }
}
