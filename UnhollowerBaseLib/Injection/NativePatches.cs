using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using UnhollowerBaseLib.Runtime;
using UnhollowerRuntimeLib.XrefScans;

namespace UnhollowerBaseLib.Injection
{
    public interface IManagedDetour
    {
        /// <summary>
        /// Patch the native function at address specified in `from`, replacing it with `to`, and return a delegate to call the original native function
        /// </summary>
        T Detour<T>(IntPtr from, T to) where T : Delegate;
    }

    public static unsafe class NativePatches
    {
        /// <summary> (namespace, class, image) : pointer </summary>
        internal static readonly Dictionary<(string, string, IntPtr), IntPtr> ClassFromNameDictionary = new Dictionary<(string, string, IntPtr), IntPtr>();

        internal static void MaybeApplyHooks()
        {
            if (Detour == null) return;
            if (ourOriginalTypeToClassMethod == null) HookClassFromType();
            if (originalClassFromNameMethod == null) HookClassFromName();
        }

        private static void HookClassFromType()
        {
            var lib = LoadLibrary("GameAssembly.dll");
            var classFromTypeEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_class_from_il2cpp_type));
            LogSupport.Trace($"il2cpp_class_from_il2cpp_type entry address: {classFromTypeEntryPoint}");

            var targetMethod = XrefScannerLowLevel.JumpTargets(classFromTypeEntryPoint).Single();
            LogSupport.Trace($"Xref scan target: {targetMethod}");

            if (targetMethod == IntPtr.Zero)
                return;

            ourOriginalTypeToClassMethod = Detour.Detour(targetMethod, new TypeToClassDelegate(ClassFromTypePatch));
            LogSupport.Trace("il2cpp_class_from_il2cpp_type patched");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Il2CppClass* TypeToClassDelegate(Il2CppTypeStruct* type);
        
        public static IManagedDetour Detour = new DoHookDetour();
        [Obsolete("Set Detour instead")]
        public static Action<IntPtr, IntPtr> DoHook;

        internal static long ourClassOverrideCounter = -2;
        internal static readonly ConcurrentDictionary<long, IntPtr> FakeTokenClasses = new ConcurrentDictionary<long, IntPtr>();

        private static volatile TypeToClassDelegate ourOriginalTypeToClassMethod;

        private static Il2CppClass* ClassFromTypePatch(Il2CppTypeStruct* type)
        {
            var wrappedType = UnityVersionHandler.Wrap(type);
            if ((long)wrappedType.Data < 0 && (wrappedType.Type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || wrappedType.Type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE))
            {
                FakeTokenClasses.TryGetValue((long)wrappedType.Data, out var classPointer);
                return (Il2CppClass*)classPointer;
            }
            // possible race: other threads can try resolving classes after the hook is installed but before delegate field is set
            while (ourOriginalTypeToClassMethod == null) Thread.Sleep(1);
            return ourOriginalTypeToClassMethod(type);
        }

        #region Class From Name Patch
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ClassFromNameDelegate(IntPtr intPtr, IntPtr str1, IntPtr str2);

        private static ClassFromNameDelegate originalClassFromNameMethod;
        private static readonly ClassFromNameDelegate hookedClassFromName = new ClassFromNameDelegate(ClassFromNamePatch);

        private static void HookClassFromName()
        {
            var lib = LoadLibrary("GameAssembly.dll");
            var classFromNameEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_class_from_name));
            LogSupport.Trace($"il2cpp_class_from_name entry address: {classFromNameEntryPoint}");

            if (classFromNameEntryPoint == IntPtr.Zero) return;

            originalClassFromNameMethod = Detour.Detour(classFromNameEntryPoint, hookedClassFromName);
            LogSupport.Trace("il2cpp_class_from_name patched");
        }

        private static IntPtr ClassFromNamePatch(IntPtr param1, IntPtr param2, IntPtr param3)
        {
            try
            {
                // possible race: other threads can try resolving classes after the hook is installed but before delegate field is set
                while (originalClassFromNameMethod == null) Thread.Sleep(1);
                IntPtr intPtr = originalClassFromNameMethod.Invoke(param1, param2, param3);

                if (intPtr == IntPtr.Zero)
                {
                    string namespaze = Marshal.PtrToStringAnsi(param2);
                    string klass = Marshal.PtrToStringAnsi(param3);
                    ClassFromNameDictionary.TryGetValue((namespaze, klass, param1), out intPtr);
                }

                return intPtr;
            }
            catch (Exception e)
            {
                LogSupport.Error(e.Message);
                return IntPtr.Zero;
            }
        }
        #endregion

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        private class DoHookDetour : IManagedDetour
        {
            // In some cases garbage collection of delegates can release their native function pointer too - keep all of them alive to avoid that
            // ReSharper disable once CollectionNeverQueried.Local
            private static readonly List<object> PinnedDelegates = new List<object>();

            public T Detour<T>(IntPtr @from, T to) where T : Delegate
            {
                IntPtr* targetVarPointer = &from;
                PinnedDelegates.Add(to);
#pragma warning disable CS0618 // Type or member is obsolete
                DoHook((IntPtr)targetVarPointer, Marshal.GetFunctionPointerForDelegate(to));
#pragma warning restore CS0618 // Type or member is obsolete
                return Marshal.GetDelegateForFunctionPointer<T>(from);
            }
        }
    }
}
