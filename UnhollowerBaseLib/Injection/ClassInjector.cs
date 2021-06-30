using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using UnhollowerBaseLib.Attributes;
using UnhollowerBaseLib.Runtime;
using UnhollowerBaseLib.Runtime.VersionSpecific.Assembly;
using UnhollowerBaseLib.Runtime.VersionSpecific.Image;

namespace UnhollowerBaseLib.Injection
{
    public static unsafe class ClassInjector
    {
        private static INativeAssemblyStruct FakeAssembly;
        private static INativeImageStruct FakeImage;

        /// <summary> type.FullName </summary>
        internal static readonly HashSet<string> InjectedTypes = new HashSet<string>();

        static void CreateFakeAssembly()
        {
            FakeAssembly = UnityVersionHandler.NewAssembly();
            FakeImage = UnityVersionHandler.NewImage();

            FakeAssembly.Name = Marshal.StringToHGlobalAnsi("InjectedMonoTypes");

            FakeImage.Assembly = FakeAssembly.AssemblyPointer;
            FakeImage.Dynamic = 1;
            FakeImage.Name = FakeAssembly.Name;
            if (FakeImage.HasNameNoExt)
                FakeImage.NameNoExt = FakeImage.Name;
        }

        public static IntPtr DerivedConstructorPointer<T>()
        {
            return IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<T>.NativeClassPtr); // todo: consider calling base constructor
        }

        public static void DerivedConstructorBody(Il2CppObjectBase objectBase)
        {
            var ownGcHandle = GCHandle.Alloc(objectBase, GCHandleType.Normal);
            MethodConversion.AssignGcHandle(objectBase.Pointer, ownGcHandle);
        }

        public static void RegisterTypeInIl2Cpp<T>() where T : class => RegisterTypeInIl2Cpp(typeof(T), true);
        public static void RegisterTypeInIl2Cpp<T>(bool logSuccess) where T : class => RegisterTypeInIl2Cpp(typeof(T), logSuccess);
        public static void RegisterTypeInIl2Cpp(Type type) => RegisterTypeInIl2Cpp(type, true);
        public static void RegisterTypeInIl2Cpp(Type type, bool logSuccess)
        {
            if (type == null)
                throw new ArgumentException($"Type argument cannot be null");

            if (type.IsGenericType || type.IsGenericTypeDefinition)
                throw new ArgumentException($"Type {type} is generic and can't be used in il2cpp");

            var currentPointer = Il2CppClassPointerStore.ReadClassPointerForType(type);
            if (currentPointer != IntPtr.Zero)
                return;//already registered in il2cpp

            var baseType = type.BaseType;
            if (baseType == null)
                throw new ArgumentException($"Class {type} does not inherit from a class registered in il2cpp");

            var baseClassPointer = UnityVersionHandler.Wrap((Il2CppClass*)Il2CppClassPointerStore.ReadClassPointerForType(baseType));
            if (baseClassPointer == null)
            {
                RegisterTypeInIl2Cpp(baseType, logSuccess);
                baseClassPointer = UnityVersionHandler.Wrap((Il2CppClass*)Il2CppClassPointerStore.ReadClassPointerForType(baseType));
            }

            if (baseClassPointer.ValueType || baseClassPointer.EnumType)
                throw new ArgumentException($"Base class {baseType} is value type and can't be inherited from");

            if (baseClassPointer.IsGeneric)
                throw new ArgumentException($"Base class {baseType} is generic and can't be inherited from");

            if ((baseClassPointer.Flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_SEALED) != 0)
                throw new ArgumentException($"Base class {baseType} is sealed and can't be inherited from");

            if ((baseClassPointer.Flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_INTERFACE) != 0)
                throw new ArgumentException($"Base class {baseType} is an interface and can't be inherited from");

            lock (InjectedTypes)
                if (!InjectedTypes.Add(type.FullName))
                    throw new ArgumentException($"Type with FullName {type.FullName} is already injected. Don't inject the same type twice, or use a different namespace");

            NativePatches.MaybeApplyHooks();
            if (FakeAssembly == null) CreateFakeAssembly();

            var classPointer = UnityVersionHandler.NewClass(baseClassPointer.VtableCount);

            classPointer.Image = FakeImage.ImagePointer;
            classPointer.Parent = baseClassPointer.ClassPointer;
            classPointer.ElementClass = classPointer.Class = classPointer.CastClass = classPointer.ClassPointer;
            classPointer.NativeSize = -1;
            classPointer.ActualSize = classPointer.InstanceSize = baseClassPointer.InstanceSize + (uint)IntPtr.Size;

            classPointer.Initialized = true;
            classPointer.InitializedAndNoError = true;
            classPointer.SizeInited = true;
            classPointer.HasFinalize = true;
            classPointer.IsVtableInitialized = true;

            classPointer.Name = Marshal.StringToHGlobalAnsi(type.Name);
            classPointer.Namespace = Marshal.StringToHGlobalAnsi(type.Namespace);

            classPointer.ThisArg.Type = classPointer.ByValArg.Type = Il2CppTypeEnum.IL2CPP_TYPE_CLASS;
            classPointer.ThisArg.ByRef = true;

            classPointer.Flags = baseClassPointer.Flags; // todo: adjust flags?

            var eligibleMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(IsMethodEligible).ToArray();
            var methodCount = 2 + eligibleMethods.Length; // 1 is the finalizer, 1 is empty ctor

            classPointer.MethodCount = (ushort)methodCount;
            var methodPointerArray = (Il2CppMethodInfo**)Marshal.AllocHGlobal(methodCount * IntPtr.Size);
            classPointer.Methods = methodPointerArray;

            methodPointerArray[0] = MethodConversion.ConvertStaticMethod(MethodConversion.FinalizeDelegate, "Finalize", classPointer);
            var finalizeMethod = UnityVersionHandler.Wrap(methodPointerArray[0]);
            methodPointerArray[1] = MethodConversion.ConvertStaticMethod(MethodConversion.CreateEmptyCtor(type), ".ctor", classPointer);
            for (var i = 0; i < eligibleMethods.Length; i++)
            {
                var methodInfo = eligibleMethods[i];
                methodPointerArray[i + 2] = MethodConversion.ConvertMethodInfo(methodInfo, classPointer);
            }

            var vTablePointer = (VirtualInvokeData*)classPointer.VTable;
            var baseVTablePointer = (VirtualInvokeData*)baseClassPointer.VTable;
            classPointer.VtableCount = baseClassPointer.VtableCount;
            for (var i = 0; i < classPointer.VtableCount; i++)
            {
                vTablePointer[i] = baseVTablePointer[i];
                var vTableMethod = UnityVersionHandler.Wrap(vTablePointer[i].method);
                if (Marshal.PtrToStringAnsi(vTableMethod.Name) == "Finalize") // slot number is not static
                {
                    vTablePointer[i].method = methodPointerArray[0];
                    vTablePointer[i].methodPtr = finalizeMethod.MethodPointer;
                }
            }

            var newCounter = Interlocked.Decrement(ref NativePatches.ourClassOverrideCounter);
            NativePatches.FakeTokenClasses[newCounter] = classPointer.Pointer;
            classPointer.ByValArg.Data = classPointer.ThisArg.Data = (IntPtr)newCounter;

            RuntimeSpecificsStore.SetClassInfo(classPointer.Pointer, true, true);
            Il2CppClassPointerStore.WriteClassPointerForType(type, classPointer.Pointer);

            AddToClassFromNameDictionary(type, classPointer.Pointer);

            if (logSuccess) LogSupport.Info($"Registered mono type {type} in il2cpp domain");
        }

        private static void AddToClassFromNameDictionary<T>(IntPtr typePointer) where T : class => AddToClassFromNameDictionary(typeof(T), typePointer);
        private static void AddToClassFromNameDictionary(Type type, IntPtr typePointer)
        {
            string klass = type.Name;
            if (klass == null) return;
            string namespaze = type.Namespace ?? string.Empty;
            var attribute = Attribute.GetCustomAttribute(type, typeof(ClassInjectionAssemblyTargetAttribute)) as ClassInjectionAssemblyTargetAttribute;

            foreach (IntPtr image in ((attribute is null) ? IL2CPP.GetIl2CppImages() : attribute.GetImagePointers()))
            {
                NativePatches.ClassFromNameDictionary.Add((namespaze, klass, image), typePointer);
            }
        }

        private static bool IsTypeSupported(Type type)
        {
            if (type.IsValueType) return type == typeof(void);
            if (typeof(Il2CppSystem.ValueType).IsAssignableFrom(type)) return false;

            return typeof(Il2CppObjectBase).IsAssignableFrom(type);
        }

        private static bool IsMethodEligible(MethodInfo method)
        {
            if (method.IsGenericMethod || method.IsGenericMethodDefinition) return false;
            if (method.Name == "Finalize") return false;
            if (method.IsStatic || method.IsAbstract) return false;
            if (method.CustomAttributes.Any(it => it.AttributeType == typeof(HideFromIl2CppAttribute))) return false;

            if (
                method.DeclaringType != null &&
                method.DeclaringType.GetProperties()
                    .Where(property => property.GetAccessors(true).Contains(method))
                    .Any(property => property.CustomAttributes.Any(it => it.AttributeType == typeof(HideFromIl2CppAttribute)))
            )
            {
                return false;
            }

            if (!IsTypeSupported(method.ReturnType))
            {
                LogSupport.Warning($"Method {method} on type {method.DeclaringType} has unsupported return type {method.ReturnType}");
                return false;
            }

            foreach (var parameter in method.GetParameters())
            {
                var parameterType = parameter.ParameterType;
                if (!IsTypeSupported(parameterType))
                {
                    LogSupport.Warning($"Method {method} on type {method.DeclaringType} has unsupported parameter {parameter} of type {parameterType}");
                    return false;
                }
            }

            return true;
        }

    }
}
