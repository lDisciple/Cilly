using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Cilly;
using Dapper;
using Cil = Cilly.Cilly;

namespace Procilly
{
    public class ProcedureRepositoryBuilder<TConnection> where TConnection : DbConnection
    {

        public Type _connectionType;
        private Type _commandType;
        private Type _parameterType;
        private Type _parameterCollectionType;

        public ProcedureRepositoryBuilder()
        {
            _connectionType = typeof(TConnection);
            _commandType = Cil.GetMethod(_connectionType, "CreateCommand").ReturnType;
            _parameterType = Cil.GetMethod(_commandType, "CreateParameter").ReturnType;
            _parameterCollectionType = Cil.GetMethod(_commandType, "get_Parameters").ReturnType;
        }

        public TProcedureRepository BuildType<TProcedureRepository>(string connectionString = "localhost")
            where TProcedureRepository : IProcedureRepository
        {
            var type = typeof(TProcedureRepository);

            ValidateType(type);
            var dynamicAssemblyName = new AssemblyName {Name = "ProcillyDynamicAssembly"};
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                dynamicAssemblyName,
                AssemblyBuilderAccess.Run);

            var dynamicModule = assemblyBuilder.DefineDynamicModule("Instances");

            var dynamicTypeName = GetTypeNameFromInterfaceType(type);
            var dynamicTypeBuilder = dynamicModule.DefineType(dynamicTypeName,
                TypeAttributes.Public | TypeAttributes.Class);
            dynamicTypeBuilder.AddInterfaceImplementation(type);

            var schema = dynamicTypeName.Replace("Repository", "");
            foreach (var methodInfo in type.GetMethods())
            {
                BuildProcedureMethod(dynamicTypeBuilder, methodInfo, schema, connectionString);
            }

            var dynamicType = dynamicTypeBuilder.CreateType();

            return (TProcedureRepository) dynamicType?.GetConstructor(new Type[0])?.Invoke(new object[0]);
        }

        public void BuildProcedureMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, string schema,
            string connectionString)
        {
            var returnType = methodInfo.ReturnType;
            var parameters = methodInfo.GetParameters();

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual);

            methodBuilder.SetParameters(parameters.Select(p => p.ParameterType).ToArray());
            methodBuilder.SetReturnType(returnType);


            var ilg = methodBuilder.GetILGenerator();
            // new CodeGeneration(this).GenerateConsoleTest(ilg, methodInfo);
            new CodeGeneration(this).GenerateProcedure(ilg, methodInfo, schema, connectionString);
            // ilg.Emit(OpCodes.Initobj, typeParam);
            // ilg.Emit(OpCodes.Ldloc_0);
        }

        private string GetTypeNameFromInterfaceType(Type type)
        {
            var moduleName = type.Name;
            if (moduleName.StartsWith("I"))
            {
                moduleName = moduleName[1..];
            }

            return moduleName;
        }

        private void ValidateType(Type type)
        {
            if (!type.IsInterface)
            {
                throw new Exception("The given type is not an interface.");
            }

            // if (!type.IsSubclassOf(typeof(IProcedureRepository)))
            // {
            //     throw new Exception("The given type does not extend IProcedureRepository");
            // }
        }

        private class CodeGeneration
        {
            private ProcedureRepositoryBuilder<TConnection> _prb;

            public CodeGeneration(ProcedureRepositoryBuilder<TConnection> prb)
            {
                _prb = prb;
            }

            internal void GenerateConsoleTest(ILGenerator ilg, MethodInfo methodInfo)
            {
                ilg.DeclareLocal(typeof(string));
                ilg.DeclareLocal(typeof(string));
                // Store locals
                ilg.Emit(OpCodes.Ldstr, "Generated code from: ");
                ilg.Emit(OpCodes.Stloc_0);
                ilg.Emit(OpCodes.Ldstr, methodInfo.Name);
                ilg.Emit(OpCodes.Stloc_1);

                // Concat and console write line
                ilg.Emit(OpCodes.Ldloc_0);
                ilg.Emit(OpCodes.Ldloc_1);
                ilg.Emit(OpCodes.Call, Cil.GetMethod(typeof(string), "Concat", new[] {typeof(string), typeof(string)}));
                ilg.Emit(OpCodes.Call, Cil.GetMethod(typeof(Console), "WriteLine", new[] {typeof(string)}));
                ilg.Emit(OpCodes.Nop);

                GenerateReturnSegment(ilg, methodInfo);
            }

            internal void GenerateProcedure(ILGenerator ilg, MethodInfo methodInfo, string schema,
                string connectionString)
            {
                var locConn = ilg.DeclareLocal(typeof(DbConnection)).LocalIndex; // connection
                var locComm = ilg.DeclareLocal(typeof(DbCommand)).LocalIndex; // command
                var locReader = ilg.DeclareLocal(typeof(DbDataReader)).LocalIndex; // reader
                var locRet = methodInfo.ReturnType == typeof(void)
                    ? -1
                    : ilg.DeclareLocal(methodInfo.ReturnType).LocalIndex; // return

                // Create connection
                ilg.Emit(OpCodes.Ldstr, connectionString);
                ilg.Emit(OpCodes.Newobj, Cil.GetConstructor(_prb._connectionType, new[] {typeof(string)}));
                ilg.Emit(OpCodes.Stloc_0); // connection
                ilg.Using(locConn, () =>
                {
                    // Open connection
                    ilg.Emit(OpCodes.Ldloc_0);
                    ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbConnection), "Open"));
                    ilg.Emit(OpCodes.Nop);

                    // Create command
                    ilg.Emit(OpCodes.Ldloc_0); // connection
                    ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbConnection), "CreateCommand"));
                    ilg.Emit(OpCodes.Stloc_1); // command
                    ilg.Using(locComm, () =>
                    {
                        // Set command type
                        ilg.Emit(OpCodes.Ldloc_1); // command
                        ilg.Emit(OpCodes.Ldc_I4_4);
                        ilg.Emit(OpCodes.Callvirt,
                            Cil.GetMethod(typeof(DbCommand), "set_CommandType", new[] {typeof(CommandType)}));
                        ilg.Emit(OpCodes.Nop);

                        // Set Procedure
                        ilg.Emit(OpCodes.Ldloc_1); // command
                        ilg.Emit(OpCodes.Ldstr, $"[{schema}].[{methodInfo.Name}]");
                        ilg.Emit(OpCodes.Callvirt,
                            Cil.GetMethod(typeof(DbCommand), "set_CommandText", new[] {typeof(string)}));
                        ilg.Emit(OpCodes.Nop);

                        // Add arguments
                        foreach (var parameterInfo in methodInfo.GetParameters())
                        {
                            GenerateCommandParameterSegment(ilg, locComm, parameterInfo);
                        }

                        // Create reader
                        GenerateReaderSegment(ilg, methodInfo, locReader, locRet);

                        // Populate 'out' parameters
                        GenerateOutParameterSegment(ilg, locComm,
                            methodInfo.GetParameters().Where(x => x.IsOut).ToList());
                    });
                });
                GenerateReturnSegment(ilg, methodInfo, locRet);
            }

            internal void GenerateCommandParameterSegment(ILGenerator ilg, int commandIndex,
                ParameterInfo parameterInfo)
            {
                var argName = parameterInfo.Name ?? throw new Exception("Parameter has no name");
                argName = "@" + argName[..1].ToUpper() + argName[1..];

                // Load parameters obj onto stack
                ilg.Emit(OpCodes.Ldloc_S, commandIndex);
                WriteOpCode(OpCodes.Ldloc_S, commandIndex);
                ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "get_Parameters"));
                WriteOpCode(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "get_Parameters"));

                // Create parameter
                ilg.Emit(OpCodes.Newobj, Cil.GetConstructor(_prb._parameterType));
                WriteOpCode(OpCodes.Newobj, Cil.GetConstructor(_prb._parameterType));

                // Set Name
                ilg.Emit(OpCodes.Dup);
                WriteOpCode(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldstr, argName);
                WriteOpCode(OpCodes.Ldstr, argName);
                ilg.Emit(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_ParameterName", new[] {typeof(string)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_ParameterName", new[] {typeof(string)}));
                ilg.Emit(OpCodes.Nop);
                WriteOpCode(OpCodes.Nop);

                // Set Value
                ilg.Emit(OpCodes.Dup);
                WriteOpCode(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldarg_S, parameterInfo.Position + 1);
                WriteOpCode(OpCodes.Ldarg_S, parameterInfo.Position + 1);
                if (parameterInfo.ParameterType.IsPrimitive && parameterInfo.ParameterType != typeof(object))
                {
                    ilg.Emit(OpCodes.Box, parameterInfo.ParameterType);
                    WriteOpCode(OpCodes.Box, parameterInfo.ParameterType);
                }

                if (parameterInfo.ParameterType.IsByRef)
                {
                    ilg.LoadRef(parameterInfo.ParameterType);
                    WriteOpCode(OpCodes.Ldind_Ref, "Loaded by ref");
                }

                ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbParameter), "set_Value", new[] {typeof(string)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_Value", new[] {typeof(string)}));
                ilg.Emit(OpCodes.Nop);
                WriteOpCode(OpCodes.Nop);

                // Set Direction for 'out' and 'in' parameters
                ilg.Emit(OpCodes.Dup);
                WriteOpCode(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4_S, (int) (parameterInfo.IsOut
                    ? ParameterDirection.Output
                    : ParameterDirection.Input));
                WriteOpCode(OpCodes.Ldc_I4_S, (int) (parameterInfo.IsOut
                    ? ParameterDirection.Output
                    : ParameterDirection.Input));
                ilg.Emit(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_Direction", new[] {typeof(ParameterDirection)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_Direction", new[] {typeof(ParameterDirection)}));
                ilg.Emit(OpCodes.Nop);
                WriteOpCode(OpCodes.Nop);

                // Set DbType
                ilg.Emit(OpCodes.Dup);
                WriteOpCode(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4_S, (int) ToDbType(parameterInfo.ParameterType));
                WriteOpCode(OpCodes.Ldc_I4_S, (int) ToDbType(parameterInfo.ParameterType));
                ilg.Emit(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_DbType", new[] {typeof(DbType)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_DbType", new[] {typeof(DbType)}));
                ilg.Emit(OpCodes.Nop);
                WriteOpCode(OpCodes.Nop);

                // Set Size to 0
                ilg.Emit(OpCodes.Dup);
                WriteOpCode(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldc_I4_0);
                WriteOpCode(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_Size", new[] {typeof(DbType)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameter), "set_Size", new[] {typeof(DbType)}));
                ilg.Emit(OpCodes.Nop);
                WriteOpCode(OpCodes.Nop);

                // Add parameter
                ilg.Emit(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameterCollection), "Add", new[] {typeof(DbParameter)}));
                WriteOpCode(OpCodes.Callvirt,
                    Cil.GetMethod(typeof(DbParameterCollection), "Add", new[] {typeof(DbParameter)}));

                ilg.Emit(OpCodes.Pop);
                WriteOpCode(OpCodes.Pop);
            }

            internal void GenerateOutParameterSegment(ILGenerator ilg, int commandIndex,
                ICollection<ParameterInfo> outParameters)
            {
                if (!outParameters.Any())
                {
                    return;
                }

                foreach (var parameterInfo in outParameters)
                {
                    var argName = parameterInfo.Name ?? throw new Exception("Parameter has no name");
                    argName = "@" + argName[..1].ToUpper() + argName[1..];

                    // Load param ref
                    ilg.Emit(OpCodes.Ldarg_S, parameterInfo.Position + 1);
                    WriteOpCode(OpCodes.Ldarg_S, parameterInfo.Position + 1);

                    // Load DbParameter obj onto stack
                    ilg.Emit(OpCodes.Ldloc_S, commandIndex);
                    WriteOpCode(OpCodes.Ldloc_S, commandIndex);
                    ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "get_Parameters"));
                    WriteOpCode(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "get_Parameters"));

                    // Get parameter value
                    ilg.Emit(OpCodes.Ldstr, argName);
                    WriteOpCode(OpCodes.Ldstr, argName);
                    ilg.Emit(OpCodes.Callvirt,
                        Cil.GetMethod(typeof(DbParameterCollection), "get_Item", new[] {typeof(string)}));
                    WriteOpCode(OpCodes.Callvirt,
                        Cil.GetMethod(typeof(DbParameterCollection), "get_Item", new[] {typeof(string)}));
                    ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbParameter), "get_Value"));
                    WriteOpCode(OpCodes.Callvirt,
                        Cil.GetMethod(typeof(DbParameter), "get_Value"));

                    ilg.Emit(OpCodes.Unbox_Any,
                        parameterInfo.ParameterType.GetElementType() ?? // Assumed that type is ByRef
                        throw new ArgumentException("A given parameter type is not ByRef"));
                    WriteOpCode(OpCodes.Unbox_Any, parameterInfo.ParameterType);
                    // Store value into param
                    ilg.StoreRef(parameterInfo.ParameterType);
                    WriteOpCode(OpCodes.Ldind_Ref, "Stored by ref");
                }
            }

            internal void GenerateReaderSegment(ILGenerator ilg, MethodInfo methodInfo, int readerIndex,
                int returnIndex)
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    ilg.Emit(OpCodes.Ldloc_1); // command
                    WriteOpCode(OpCodes.Ldloc_1);
                    ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "ExecuteNonQuery"));
                    WriteOpCode(OpCodes.Callvirt);
                    ilg.Emit(OpCodes.Pop);
                    WriteOpCode(OpCodes.Pop);
                    return;
                }

                ilg.Emit(OpCodes.Ldloc_1); // command
                WriteOpCode(OpCodes.Ldloc_1);
                ilg.Emit(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "ExecuteReader"));
                WriteOpCode(OpCodes.Callvirt, Cil.GetMethod(typeof(DbCommand), "ExecuteReader"));
                ilg.Emit(OpCodes.Stloc_S, readerIndex); // reader
                WriteOpCode(OpCodes.Stloc_S, readerIndex);
                ilg.Using(readerIndex, () =>
                {
                    var parseType = methodInfo.ReturnType;
                    var isEnumerable = typeof(IEnumerable).IsAssignableFrom(parseType);
                    if (isEnumerable)
                    {
                        parseType = parseType.GetGenericArguments()[0];
                    }

                    ilg.Emit(OpCodes.Ldloc_S, readerIndex); // reader
                    WriteOpCode(OpCodes.Ldloc_S, readerIndex);

                    ilg.Emit(OpCodes.Call, Cil.GetMethod(typeof(SqlMapper), "Parse",
                        new[] {typeof(IDataReader)}, new[] {parseType}));
                    WriteOpCode(OpCodes.Call, Cil.GetMethod(typeof(SqlMapper), "Parse",
                        new[] {typeof(IDataReader)}, new[] {parseType}));
                    if (!isEnumerable)
                    {
                        var firstMethod = typeof(Enumerable).GetMethods().Single(x =>
                                x.Name == "FirstOrDefault" && x.GetParameters().Length == 1 && x.IsGenericMethod)
                            .MakeGenericMethod(parseType);
                        ilg.Emit(OpCodes.Call, firstMethod);
                        WriteOpCode(OpCodes.Call, firstMethod);
                    }
                    else
                    {
                        var firstMethod = typeof(SqlMapper).GetMethods().Single(x =>
                                x.Name == "AsList" && x.GetParameters().Length == 1 && x.IsGenericMethod)
                            .MakeGenericMethod(parseType);
                        ilg.Emit(OpCodes.Call, firstMethod);
                        WriteOpCode(OpCodes.Call, firstMethod);
                    }

                    ilg.Emit(OpCodes.Stloc_S, returnIndex);
                    WriteOpCode(OpCodes.Stloc_S, returnIndex);
                });
            }

            internal void GenerateReturnSegment(ILGenerator ilg, MethodInfo methodInfo, int returnIndex = -1)
            {
                if (methodInfo.ReturnType != typeof(void))
                {
                    if (returnIndex == -1)
                    {
                        ilg.Emit(OpCodes.Ldnull);
                        WriteOpCode(OpCodes.Ldnull);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Ldloc_S, returnIndex);
                        WriteOpCode(OpCodes.Ldloc_S, returnIndex);
                    }
                }

                WriteOpCode(OpCodes.Ret);
                ilg.Emit(OpCodes.Ret);
            }
#if DEBUG
            private void WriteOpCode(OpCode opCode, params object[] args) =>
                Console.WriteLine($"Emit: {opCode.ToString()} {string.Join(" ", args)}");
#else
            private void WriteOpCode(OpCode opCode, params object[] args) {}
#endif
        }

        private static readonly Dictionary<Type, DbType> TypeToDbTypeDictionary = new Dictionary<Type, DbType>
        {
            [typeof(bool)] = DbType.Boolean,
            // signed numbers
            [typeof(sbyte)] = DbType.SByte,
            [typeof(short)] = DbType.Int16,
            [typeof(int)] = DbType.Int32,
            [typeof(long)] = DbType.Int64,
            [typeof(byte)] = DbType.Byte,
            // unsigned numbers
            [typeof(ushort)] = DbType.UInt16,
            [typeof(uint)] = DbType.UInt32,
            [typeof(ulong)] = DbType.UInt64,
            // floating numbers
            [typeof(float)] = DbType.Single,
            [typeof(double)] = DbType.Double,
            // other
            [typeof(DateTime)] = DbType.DateTime,
            [typeof(Guid)] = DbType.Guid,
            [typeof(string)] = DbType.String
        };

        public static DbType ToDbType(Type type)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType() ??
                       throw new ArgumentException("Could not get element type from ByRef type");
            }

            return TypeToDbTypeDictionary[type];
        }
    }
}