using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Cilly
{
    public static class Cilly
    {
        public static MethodInfo GetMethod(Type type, string name) => GetMethod(type, name, new Type[0]);

        public static MethodInfo GetMethod(Type type, string name, Type[] args) => type.GetMethod(name, args) ??
            throw new Exception("No such method exists with given the given arguments");
        public static MethodInfo GetMethod(Type type, string name, Type[] args, Type[] genericArgs) {
            var method = type.GetMethod(name, genericArgs.Length, args) ??
                throw new Exception("No such method exists with given the given arguments");
            return genericArgs.Length == 0 ? method : method.MakeGenericMethod(genericArgs);
        }

        public static FieldInfo GetField(Type type, string name) => type.GetField(name) ??
            throw new Exception("No such field exists with given the given arguments");
        
        public static ConstructorInfo GetConstructor(Type type) => GetConstructor(type, new Type[0]);

        public static ConstructorInfo GetConstructor(Type type, Type[] args) => type.GetConstructor(args) ??
            throw new Exception("No such constructor exists with given the given arguments");

        public static void Using(this ILGenerator ilg, int localIndex, Action innerCallback)
        {
            ilg.BeginExceptionBlock();
            {
                innerCallback();
            }
            ilg.BeginFinallyBlock();
            {
                // Dispose of reader
                ilg.Emit(OpCodes.Ldloc_S, localIndex);
                
                ilg.Emit(OpCodes.Callvirt, GetMethod(typeof(IDisposable), "Dispose"));
            }
            ilg.EndExceptionBlock();
        }

        public static void LoadRef(this ILGenerator ilg, Type refType)
        {
            // References
            if (refType == typeof(object).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_Ref);
                return;
            }
            // Signed Integers
            if (refType == typeof(sbyte).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_I1);
                return;
            }
            if (refType == typeof(short).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_I2);
                return;
            }
            if (refType == typeof(int).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_I4);
                return;
            }
            if (refType == typeof(long).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_I8);
                return;
            }
            // Unsigned Integers
            if (refType == typeof(byte).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_U1);
                return;
            }
            if (refType == typeof(ushort).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_U2);
                return;
            }
            if (refType == typeof(uint).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_U4);
                return;
            }
            // Float types
            if (refType == typeof(float).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_R4);
                return;
            }
            if (refType == typeof(double).MakeByRefType())
            {
                ilg.Emit(OpCodes.Ldind_R8);
                return;
            }

            throw new ArgumentException($"The given reference type could not be loaded from its address ({refType})");
        }

        public static void StoreRef(this ILGenerator ilg, Type refType)
        {
            // References
            if (refType == typeof(object).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_Ref);
                return;
            }
            // Signed Integers
            if (refType == typeof(sbyte).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_I1);
                return;
            }
            if (refType == typeof(short).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_I2);
                return;
            }
            if (refType == typeof(int).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_I4);
                return;
            }
            if (refType == typeof(long).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_I8);
                return;
            }
            // Float types
            if (refType == typeof(float).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_R4);
                return;
            }
            if (refType == typeof(double).MakeByRefType())
            {
                ilg.Emit(OpCodes.Stind_R8);
                return;
            }

            throw new ArgumentException($"The given reference type could not have a value stored in its address ({refType})");
        }
    }
}