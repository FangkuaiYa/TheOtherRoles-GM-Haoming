using System;
using System.Linq.Expressions;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace TheOtherRoles;

public static unsafe class FastDestroyableSingleton<T> where T : MonoBehaviour
{
    private static readonly IntPtr _fieldPtr;
    private static readonly Func<IntPtr, T> _createObject;

    static FastDestroyableSingleton()
    {
        _fieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DestroyableSingleton<T>>.NativeClassPtr,
            nameof(DestroyableSingleton<T>._instance));
        ConstructorInfo constructor = typeof(T).GetConstructor(new[] { typeof(IntPtr) });
        ParameterExpression ptr = Expression.Parameter(typeof(IntPtr));
        NewExpression create = Expression.New(constructor!, ptr);
        Expression<Func<IntPtr, T>> lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
        _createObject = lambda.Compile();
    }

    public static T Instance
    {
        get
        {
            IntPtr objectPointer;
            IL2CPP.il2cpp_field_static_get_value(_fieldPtr, &objectPointer);
            if (objectPointer == IntPtr.Zero) return DestroyableSingleton<T>.Instance;
            return _createObject(objectPointer);
        }
    }
}
