// Copyright(c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.PowerFx.Interpreter.Minit.Models;

namespace Microsoft.PowerFx.Interpreter.Tests.Minit
{
    public class MinitDataFixture : IDisposable
    {
        public readonly IEnumerable<MinitEvent> Events;

        public MinitDataFixture()
        {
            Events = new List<MinitEvent> {
                new MinitEvent(1, 1, "A", MinitUser.Peter, 10),
                new MinitEvent(1, 1, "B", MinitUser.Michal, 20),
                new MinitEvent(1, 1, "C", MinitUser.Michal, 60),
                new MinitEvent(1, 2, "A", MinitUser.Peter, 40),
                new MinitEvent(1, 2, "B", MinitUser.Denis, 20),
                new MinitEvent(1, 2, "C", MinitUser.Denis, 60),
                new MinitEvent(1, 2, "C", MinitUser.Michal, 60),
                new MinitEvent(2, 3, "A", MinitUser.Denis, 10),
                new MinitEvent(2, 3, "B", MinitUser.Peter, 20),
                new MinitEvent(2, 3, "C", MinitUser.Michal, 180)
            };
        }

        public void Dispose()
        {
        }
    }
}
