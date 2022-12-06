// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.IR.Nodes;
using Microsoft.PowerFx.Core.IR.Symbols;
using Microsoft.PowerFx.Interpreter.Minit.Models;
using Microsoft.PowerFx.Interpreter.Minit.NodeHandlers;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Minit
{
    public class MinitConverter
    {
        // By default, all events have 3 required fields. 
        // They can have additional custom fields ("Attributes")

        internal readonly RecordType _eventType = RecordType.Empty()            
            .Add("Duration", FormulaType.Number) // Logically, (Start-End)
            .Add("Start", FormulaType.DateTime)
            .Add("End", FormulaType.DateTime)
            .Add("Activity", FormulaType.String);
        
        internal readonly ReadOnlySymbolTable _builtinSymbols;
        internal readonly IList<ISymbolSlot> _slotAllEvents;

        public MinitConverter()
        {
            var scopes = typeof(AggregationScopes).GetFields(BindingFlags.Static | BindingFlags.Public);
            var symTable = new SymbolTable();

            _slotAllEvents = new List<ISymbolSlot>();

            foreach (var scope in scopes)
            {
                _slotAllEvents.Add(symTable.AddVariable(scope.Name, _eventType.ToTable()));
            }

            _builtinSymbols = symTable;
        }

        public string Convert(string inputFx)
        {
            var config = new PowerFxConfig();
            var engine = new Engine(config);

            // Ensure we can parse and bind the expression. 
            var check = engine.Check(inputFx, symbolTable: _builtinSymbols);
            check.ThrowOnErrors();

            (var irnode, var ruleScopeSymbol) = IRTranslator.Translate(check._binding);

            var visitor = new MinitVisitor();
            var ctx = new VisitorContext
            {
                _parent = this
            };
            var result = irnode.Accept(visitor, ctx);

            var output = result._miniExpr;
            return output;
        }

        // Add SumGroup, SumIf functions 
    }

    // Context mpassed between each visitor node. 
    public class VisitorContext
    {
        public MinitConverter _parent;        
    }

    // return result from visitor method. 
    public class VisitorResult
    {
        public string _miniExpr;
    }

    // Walk an Power Fx tree and write out in target language 
    internal class MinitVisitor : IRNodeVisitor<VisitorResult, VisitorContext>
    {
        internal readonly IList<INodeHandler> _nodeHandlers;

        public MinitVisitor()
        {
            _nodeHandlers = InitializeNodeHandlers();
        }

        private List<INodeHandler> InitializeNodeHandlers()
        {
            var nodeHandlers = new List<INodeHandler>();
            var nodeHandlerType = typeof(INodeHandler);
            var types = nodeHandlerType.Assembly.GetTypes().Where(t => nodeHandlerType.IsAssignableFrom(t) && !t.IsInterface);

            foreach (var type in types)
            {
                nodeHandlers.Add((INodeHandler)Activator.CreateInstance(type));
            }

            return nodeHandlers;
        }

        private static bool Equals(ISymbolSlot slotA, ISymbolSlot slotB)
        {
            if (slotA == null && slotB == null)
            {
                return true;
            }

            return slotA != null && !slotA.IsDisposed() &&
                slotB != null && !slotB.IsDisposed() &&
                slotA.SlotIndex == slotB.SlotIndex &&
                slotA.Owner == slotB.Owner;
        }

        public override VisitorResult Visit(TextLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(NumberLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(BooleanLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ColorLiteralNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ErrorNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(LazyEvalNode node, VisitorContext context)
        {
            // Common for all predicates, like arg1 in Sum:
            //   Sum(ProcessEvents, Duration)
            var ret = node.Child.Accept(this, context);
            return ret;
        }

        public override VisitorResult Visit(CallNode node, VisitorContext context)
        {
            var handler = _nodeHandlers.FirstOrDefault(h => h.CanHandle(node, context));

            if (handler == null)
            {
                throw new NotImplementedException($"Unimplemented node type {node}");
            }

            return handler.Handle(this, node, context);
        }

        public override VisitorResult Visit(BinaryOpNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(UnaryOpNode node, VisitorContext context)
        {
            switch (node.Op)
            {
                case UnaryOpKind.Negate:
                    // Handle Not
                    break;
                default:
                    // Other
                    break;
            }

            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ScopeAccessNode node, VisitorContext context)
        {
            if (node.Value is ScopeAccessSymbol sym)
            {
                // Refers to a field on Arg1. 
                var name = sym.Name.Value;
                if (name == "Duration")
                {
                    // In minit, duration is a function. 
                    var ret = new VisitorResult
                    {
                        _miniExpr = "Duration()"
                    };
                    return ret;
                }
            }

            throw new NotImplementedException();
        }

        public override VisitorResult Visit(RecordFieldAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ResolvedObjectNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(SingleColumnTableAccessNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(ChainingNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }

        public override VisitorResult Visit(AggregateCoercionNode node, VisitorContext context)
        {
            throw new NotImplementedException();
        }
    }
}
