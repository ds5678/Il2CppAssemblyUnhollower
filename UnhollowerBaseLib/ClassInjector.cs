using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;

namespace UnhollowerRuntimeLib
{
    public static class ClassInjector
    {
        [Obsolete("Use UnhollowerBaseLib.Injection.MethodConversion instead.")]
        public static void ProcessNewObject(Il2CppObjectBase obj)
        {
            UnhollowerBaseLib.Injection.MethodConversion.ProcessNewObject(obj);
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static IntPtr DerivedConstructorPointer<T>()
        {
            return UnhollowerBaseLib.Injection.ClassInjector.DerivedConstructorPointer<T>();
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static void DerivedConstructorBody(Il2CppObjectBase objectBase)
        {
            UnhollowerBaseLib.Injection.ClassInjector.DerivedConstructorBody(objectBase);
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.MethodConversion instead.")]
        public static void AssignGcHandle(IntPtr pointer, GCHandle gcHandle)
        {
            UnhollowerBaseLib.Injection.MethodConversion.AssignGcHandle(pointer,gcHandle);
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static void RegisterTypeInIl2Cpp<T>() where T : class => RegisterTypeInIl2Cpp(typeof(T), true);
        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static void RegisterTypeInIl2Cpp<T>(bool logSuccess) where T : class => RegisterTypeInIl2Cpp(typeof(T), logSuccess);
        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static void RegisterTypeInIl2Cpp(Type type) => RegisterTypeInIl2Cpp(type, true);
        [Obsolete("Use UnhollowerBaseLib.Injection.ClassInjector instead.")]
        public static void RegisterTypeInIl2Cpp(Type type, bool logSuccess)
        {
            UnhollowerBaseLib.Injection.ClassInjector.RegisterTypeInIl2Cpp(type,logSuccess);
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.MethodConversion instead.")]
        public static void Finalize(IntPtr ptr)
        {
            UnhollowerBaseLib.Injection.MethodConversion.Finalize(ptr);
        }

        [Obsolete("Use UnhollowerBaseLib.Injection.NativePatches instead.")]
        public static IManagedDetour Detour;//<============================================ may need initialized

        [Obsolete("Set Detour instead")] public static Action<IntPtr, IntPtr> DoHook;
    }

    [Obsolete("Use UnhollowerBaseLib.Injection.IManagedDetour instead.")]
    public interface IManagedDetour
    {
        /// <summary>
        /// Patch the native function at address specified in `from`, replacing it with `to`, and return a delegate to call the original native function
        /// </summary>
        T Detour<T>(IntPtr from, T to) where T : Delegate;
    }
}