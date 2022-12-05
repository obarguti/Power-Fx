// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.PowerFx.Interpreter.Minit.Models;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter.Minit
{
    public class PowerFxEngineFactory
    {
        public static RecalcEngine Create(IEnumerable<MinitEvent> data)
        {
            var cache = new TypeMarshallerCache();
            var fxEvents = cache.Marshal(data);
            var engine = new RecalcEngine();

            SetAggregationScopesAsBuiltInIdentifiers(engine, fxEvents);

            return engine;
        }

        private static void SetAggregationScopesAsBuiltInIdentifiers(RecalcEngine engine, FormulaValue fxEvents)
        {
            var scopes = typeof(AggregationScopes).GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (var scope in scopes)
            {
                engine.UpdateVariable(scope.Name, fxEvents);
            }
        }
    }
}
