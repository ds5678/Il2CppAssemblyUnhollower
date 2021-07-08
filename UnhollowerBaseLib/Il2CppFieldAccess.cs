using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public enum TestEnum
    {
        One,
        Two,
        Three
    }
    public static class Il2CppFieldAccess
    {
        public static IntPtr GetOriginalFieldPointer<T>(string fieldName)
        {
            return IL2CPP.GetIl2CppField(Il2CppClassPointerStore<T>.NativeClassPtr, fieldName);
        }

        public static IntPtr GetObjectFieldPointer<T>(T il2CppObject, IntPtr originalFieldPointer) where T : Il2CppObjectBase
        {
            return GetObjectFieldPointer(il2CppObject, (int) IL2CPP.il2cpp_field_get_offset(originalFieldPointer));
        }
        public static IntPtr GetObjectFieldPointer<T>(T il2CppObject, int fieldOffset) where T : Il2CppObjectBase
        {
            return IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject) + fieldOffset;
        }

        //public static void SetObjectField<T,>

        public static void Test()
        {
            GetOriginalFieldPointer<int>("dfdes");
            GetStructValue<Il2CppObjectBase, int>(null, "");
            GetStructValue<Il2CppObjectBase, TestEnum>(null, "");
            GetReferenceValue<Il2CppObjectBase, Il2CppSystem.Collections.Generic.List<int>>(null, "");
        }

        public static string GetStringValue<T>(T il2CppObject, string fieldName) where T : Il2CppObjectBase
        {
            return GetStringValue<T>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static string GetStringValue<T>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase
        {
            return IL2CPP.Il2CppStringToManaged(GetObjectFieldPointer<T>(il2CppObject, fieldPointer));
        }
        public static void SetStringValue<T>(T il2CppObject, string fieldName, string value) where T : Il2CppObjectBase
        {
            SetStringValue<T>(il2CppObject, GetOriginalFieldPointer<T>(fieldName), value);
        }
        public static void SetStringValue<T>(T il2CppObject, IntPtr fieldPointer, string value) where T : Il2CppObjectBase
        {
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject);
            IL2CPP.il2cpp_gc_wbarrier_set_field(intPtr, intPtr + (int)IL2CPP.il2cpp_field_get_offset(fieldPointer), IL2CPP.ManagedStringToIl2Cpp(value));
        }

        public static S GetStructValue<T, S>(T il2CppObject, string fieldName) where T : Il2CppObjectBase where S : unmanaged
        {
            return GetStructValue<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static S GetStructValue<T, S>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase where S : unmanaged
        {
            return Marshal.PtrToStructure<S>(GetObjectFieldPointer<T>(il2CppObject, fieldPointer));
        }
        public static unsafe void SetStructValue<T, S>(T il2CppObject, string fieldName, S value) where T : Il2CppObjectBase where S : unmanaged
        {
            SetStructValue<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName), value);
        }
        public static unsafe void SetStructValue<T, S>(T il2CppObject, IntPtr fieldPointer, S value) where T : Il2CppObjectBase where S : unmanaged
        {
            S* x = (S*) GetObjectFieldPointer<T>(il2CppObject, fieldPointer);
            *x = value;
        }

        public static S GetReferenceValue<T, S>(T il2CppObject, string fieldName) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            return GetReferenceValue<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static S GetReferenceValue<T, S>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            IntPtr intPtr = GetObjectFieldPointer<T>(il2CppObject, fieldPointer);
            return (intPtr != IntPtr.Zero) ? (new Il2CppObjectBase(intPtr)).TryCast<S>() : null;
        }
        public static void SetReferenceValue<T, S>(T il2CppObject, string fieldName, S value) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            SetReferenceValue<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName), value);
        }
        public unsafe static void SetReferenceValue<T, S>(T il2CppObject, IntPtr fieldPointer, S value) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            SetReferenceValue<T, S>(il2CppObject, (int)IL2CPP.il2cpp_field_get_offset(fieldPointer), value);
        }
        public unsafe static void SetReferenceValue<T, S>(T il2CppObject, int offset, S value) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject);
            IL2CPP.il2cpp_gc_wbarrier_set_field(intPtr, intPtr + offset, IL2CPP.Il2CppObjectBaseToPtr(value));
        }
    }
}
