// <copyright file="ContinuationGeneratorState.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Profiler.Managed.CallTarget.Handlers.Continuations
{
    internal readonly struct ContinuationGeneratorState<TTarget>
    {
        public readonly TTarget Target;
        public readonly CallTargetState State;

        public ContinuationGeneratorState(TTarget target, CallTargetState state)
        {
            Target = target;
            State = state;
        }
    }
}
