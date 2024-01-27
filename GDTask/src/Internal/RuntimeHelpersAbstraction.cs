﻿using Godot;
using System;
using System.Runtime.CompilerServices;

namespace GodotTask.Tasks.Internal
{
    internal static class RuntimeHelpersAbstraction
    {
        public static bool IsWellKnownNoReferenceContainsType<T>()
        {
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
    }
}

