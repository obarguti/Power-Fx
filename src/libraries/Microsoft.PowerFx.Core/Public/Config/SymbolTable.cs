﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Binding.BindInfo;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Provides symbols to the engine. This includes variables (locals, globals), enums, options sets, and functions.
    /// SymbolTables are mutable to support sessionful scenarios and can be chained together. 
    /// This is a publicly facing class around a <see cref="INameResolver"/>.
    /// </summary>
    [DebuggerDisplay("{DebugName}")]
    public class SymbolTable : ReadOnlySymbolTable
    {
        private readonly SlotMap<NameLookupInfo?> _slots = new SlotMap<NameLookupInfo?>();

        // Expose public setters
        // https://github.com/microsoft/Power-Fx/issues/828
        [Obsolete("Use Composition instead of Parent Pointer")]
        public new ReadOnlySymbolTable Parent
        {
            get => _parent;
            init
            {
                Inc();
                _parent = value;
            }
        }

        private void ValidateName(string name)
        {
            if (!DName.IsValidDName(name))
            {
                throw new ArgumentException("Invalid name: ${name}");
            }
        }

        public override FormulaType GetTypeFromSlot(ISymbolSlot slot)
        {
            if (_slots.TryGet(slot.SlotIndex, out var nameInfo))
            {
                return FormulaType.Build(nameInfo.Value.Type);
            }

            throw NewBadSlotException(slot); 
        }

        // Ensure that newType can be assigned to the given slot. 
        internal void ValidateAccepts(ISymbolSlot slot, FormulaType newType)
        {
            if (_slots.TryGet(slot.SlotIndex, out var nameInfo))
            {
                var srcType = nameInfo.Value.Type;
                
                if (newType is RecordType)
                {
                    // Lazy RecordTypes don't validate. 
                    // https://github.com/microsoft/Power-Fx/issues/833
                    return;
                }

                var ok = srcType.Accepts(newType._type);

                if (ok)
                {
                    return;
                }

                var name = (nameInfo.Value.Data as NameSymbol)?.Name;

                throw new InvalidOperationException($"Can't change '{name}' from {srcType} to {newType._type}.");
            }

            throw NewBadSlotException(slot);
        }

        /// <summary>
        /// Provide variable for binding only.
        /// Value must be provided at runtime.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="mutable"></param>
        /// <param name="displayName"></param>
        public ISymbolSlot AddVariable(string name, FormulaType type, bool mutable = false, string displayName = null)
        {
            if (displayName != null)
            {
                // Include parameter so that it's not a breaking change when we enable.
                // https://github.com/microsoft/Power-Fx/issues/779
                throw new NotImplementedException("DisplayName support for variables not implemented yet");
            }

            Inc();
            ValidateName(name);

            if (_variables.ContainsKey(name))
            {
                throw new InvalidOperationException($"{name} is already defined");
            }

            var slotIndex = _slots.Add(null);
            var data = new NameSymbol(name, mutable)
            {
                Owner = this,
                SlotIndex = slotIndex
            };
            
            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data: data);

            _slots.Set(slotIndex, info);
            _variables.Add(name, info); // can't exist

            return data;
        }

        /// <summary>
        /// Add a constant.  This is like a variable, but the value is known at bind time. 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public void AddConstant(string name, FormulaValue data)
        {
            var type = data.Type;

            Inc();
            ValidateName(name);

            var info = new NameLookupInfo(
                BindKind.PowerFxResolvedObject,
                type._type,
                DPath.Root,
                0,
                data);

            _variables.Add(name, info); // can't exist
        }

        /// <summary>
        /// Remove variable of given name. 
        /// </summary>
        /// <param name="name"></param>
        public void RemoveVariable(string name)
        {
            Inc();

            if (_variables.TryGetValue(name, out var info))
            {
                if (info.Data is NameSymbol info2)
                {
                    _slots.Remove(info2.SlotIndex);
                    info2.DisposeSlot();
                }
            }

            // Ok to remove if missing. 
            _variables.Remove(name);
        }

        /// <summary>
        /// Remove function of given name. 
        /// </summary>
        /// <param name="name"></param>
        public void RemoveFunction(string name)
        {
            Inc();

            _functions.RemoveAll(func => func.Name == name);
        }

        internal void RemoveFunction(TexlFunction function)
        {
            Inc();

            _functions.RemoveAll(func => func == function);
        }

        internal void AddFunction(TexlFunction function)
        {
            Inc();
            _functions.Add(function);

            // Add any associated enums 
            EnumStoreBuilder?.WithRequiredEnums(new List<TexlFunction>() { function });
        }

        internal EnumStoreBuilder EnumStoreBuilder
        {
            get => _enumStoreBuilder;
            set
            {
                Inc();
                _enumStoreBuilder = value;
            }
        }

        internal void AddEntity(IExternalEntity entity, DName displayName = default)
        {
            Inc();

            // Attempt to update display name provider before symbol table,
            // since it can throw on collision and we want to leave the config in a good state.
            // For entities without a display name, add (logical, logical) pair to still be included in collision checks.
            if (_environmentSymbolDisplayNameProvider is SingleSourceDisplayNameProvider ssDnp)
            {
                _environmentSymbolDisplayNameProvider = ssDnp.AddField(entity.EntityName, displayName != default ? displayName : entity.EntityName);
            }

            _environmentSymbols.Add(entity.EntityName, entity);
        }
    }
}
