// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.
//
// <copyright file="CallTargetState.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;

namespace Elastic.Apm.Profiler.Managed.CallTarget
{
    /// <summary>
    /// Call target execution state
    /// </summary>
    public readonly struct CallTargetState
    {
        private readonly IExecutionSegment _previousSegment;
        private readonly IExecutionSegment _segment;
        private readonly object _state;
        private readonly DateTimeOffset? _startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="segment">Scope instance</param>
        public CallTargetState(IExecutionSegment segment)
        {
            _previousSegment = null;
            _segment = segment;
            _state = null;
            _startTime = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="segment">Scope instance</param>
        /// <param name="state">Object state instance</param>
        public CallTargetState(IExecutionSegment segment, object state)
        {
            _previousSegment = null;
            _segment = segment;
            _state = state;
            _startTime = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallTargetState"/> struct.
        /// </summary>
        /// <param name="segment">Scope instance</param>
        /// <param name="state">Object state instance</param>
        /// <param name="startTime">The intended start time of the scope, intended for scopes created in the OnMethodEnd handler</param>
        public CallTargetState(IExecutionSegment segment, object state, DateTimeOffset? startTime)
        {
            _previousSegment = null;
            _segment = segment;
            _state = state;
            _startTime = startTime;
        }

        internal CallTargetState(IExecutionSegment previousSegment, CallTargetState state)
        {
            _previousSegment = previousSegment;
            _segment = state._segment;
            _state = state._state;
            _startTime = state._startTime;
        }

        /// <summary>
        /// Gets the CallTarget BeginMethod segment
        /// </summary>
        public IExecutionSegment Segment => _segment;

        /// <summary>
        /// Gets the CallTarget BeginMethod state
        /// </summary>
        public object State => _state;

        /// <summary>
        /// Gets the CallTarget state StartTime
        /// </summary>
        public DateTimeOffset? StartTime => _startTime;

        internal IExecutionSegment PreviousSegment => _previousSegment;

        /// <summary>
        /// Gets the default call target state (used by the native side to initialize the locals)
        /// </summary>
        /// <returns>Default call target state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallTargetState GetDefault() => default;

		/// <summary>
        /// ToString override
        /// </summary>
        /// <returns>String value</returns>
        public override string ToString() => $"{typeof(CallTargetState).FullName}({_previousSegment}, {_segment}, {_state})";
	}
}
