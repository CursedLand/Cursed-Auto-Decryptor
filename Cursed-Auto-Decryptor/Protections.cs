
#region Usings
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpCodes = dnlib.DotNet.Emit.OpCodes;
#endregion

namespace Cursed_Auto_Decryptor
{
    public class Protections
    {
        public List<Instruction> ToRemoveInst = new List<Instruction>();
        public Protections(Context Context)
        {
            DecryptConstants(Context);
            Context.Save();
        }
        public void DecryptConstants(Context Context)
        {
            foreach (var TypeDef in Context.Module.GetTypes().Where(x => x.HasMethods && !x.IsGlobalModuleType)) {
                foreach (var MethodDef in TypeDef.Methods.Where(x => x.HasBody)) {

                    MethodDef.Body.ExpandBody(MethodDef.Parameters);
                    IList<Instruction> IL = MethodDef.Body.Instructions;
                    for (int x = 0; x < IL.Count; x++) {

                        if (IL[x].OpCode == OpCodes.Call &&
                            IL[x].Operand.ToString().Contains("<System.String>") &&
                            IL[x].Operand is MethodSpec _spec &&
                            _spec.GenericInstMethodSig.GenericArguments.Count == 1) // TODO : Add More Check For More Accuracy
                        {
                            try {
                                object Result = null;
                                IMethod DecMethod = (IMethod)IL[x].Operand;
                                var ReturnedParams = ParseParams(IL, Context, DecMethod, x);
                                if (DecMethod.ResolveMethodDef().Body.Instructions.Any(i => i.ToString().Contains("StackTrace") || i.ToString().Contains("GetCallingAssembly") || i.ToString().Contains("GetEntryAssembly")))
                                    Result = InvokeAsDynamic(Context.Ass.ManifestModule, MethodDef, DecMethod.ResolveMethodDef(), ReturnedParams);
                                else
                                    Result = ((MethodInfo)Context.Ass.ManifestModule.ResolveMethod((int)DecMethod.MDToken.Raw)).Invoke(null, ReturnedParams);
                                Context.Log.Information($"Restored String : {Result.ToString()}");
                                var _ldstr = OpCodes.Ldstr.ToInstruction(Result.ToString());
                                IL[x].OpCode = _ldstr.OpCode;
                                IL[x].Operand = _ldstr.Operand;
                                foreach (var i in ToRemoveInst) 
                                    i.NopInstruction();
                            }
                            catch (Exception e) {
                                Context.Log.Error(e.Message);
                            }
                        }
                    }
                    MethodDef.Body.OptimzeBody();
                }
            }
        }
        public object[] ParseParams(IList<Instruction> IL,
                                    Context _ctx,
                                    IMethod DecMethod,
                                    int Index)
        {
            var pi = 0;

            var pp = 0;

            var rMethod = _ctx.Ass.ManifestModule.ResolveMethod(DecMethod.MDToken.ToInt32());

            var rMethodParams = rMethod.GetParameters();

            var C = rMethodParams.Length;

            var Parsed = new object[C];

            for (int x = (-C + Index); x < Index; x++)
            {
                object Result = null;

                if (IL[x].OpCode == OpCodes.Stsfld || IL[x].OpCode == OpCodes.Ldsfld)
                    Result = _ctx.Ass.ManifestModule.ResolveField(((IField)IL[x].Operand).MDToken.ToInt32()).GetValue(null);

                var CurrentT = rMethodParams[pi++].ParameterType;


                if (CurrentT == typeof(String) || CurrentT == typeof(string))
                    Result = (string)IL[x].Operand;
                else if (CurrentT == typeof(Int16) || CurrentT == typeof(short))
                    Result = Result == null ? (short)IL[x].GetLdcI4Value() : (short)Result;
                else if (CurrentT == typeof(Int32) || CurrentT == typeof(int))
                    Result = Result == null ? (int)IL[x].GetLdcI4Value() : (int)Result;
                else if (CurrentT == typeof(Int64) || CurrentT == typeof(long))
                    Result = Result == null ? (long)IL[x].GetLdcI4Value() : (long)Result;
                else if (CurrentT == typeof(SByte) || CurrentT == typeof(sbyte))
                    Result = Result == null ? (sbyte)IL[x].Operand : (sbyte)Result;
                else if (CurrentT == typeof(Byte) || CurrentT == typeof(byte))
                    Result = Result == null ? (byte)IL[x].Operand : (byte)Result;
                else if (CurrentT == typeof(UInt16) || CurrentT == typeof(ushort))
                    Result = Result == null ? (ushort)unchecked(IL[x].GetLdcI4Value()) : (ushort)Result;
                else if (CurrentT == typeof(UInt32) || CurrentT == typeof(uint))
                    Result = Result == null ? (uint)unchecked(IL[x].GetLdcI4Value()) : (uint)Result;
                else if (CurrentT == typeof(UInt64) || CurrentT == typeof(ulong))
                    Result = Result == null ? (ulong)unchecked(IL[x].GetLdcI4Value()) : (ulong)Result;
                else if (CurrentT == typeof(Boolean) || CurrentT == typeof(bool))
                    Result = Result == null ? (IL[x].GetLdcI4Value() == 1 ? true : false) : Convert.ToBoolean(Result);
                else if (CurrentT == typeof(Char) || CurrentT == typeof(char))
                    Result = Result == null ? Convert.ToChar(IL[x].GetLdcI4Value()) : (char)Result;
                else
                    Result = Result == null ? Convert.ChangeType(IL[x].Operand, CurrentT) : Convert.ChangeType(Result, CurrentT);

                Parsed[pp++] = Result;

                ToRemoveInst.Add(IL[x]);

            }

            return Parsed;
        }

        public object InvokeAsDynamic(Module Module,
                                      MethodDef CMethod,
                                      MethodDef DecMethod,
                                      object[] Params)
        {
            // Semi Bypass Any AntiInvoking Technique (StackTrace, GetCallingAssembly, etc.)

            var rMethod = Module.ResolveMethod(DecMethod.MDToken.ToInt32(),
                            null,
                            new Type[1] { typeof(string) });

            var rType = typeof(string);

            var pT = new List<Type>();

            foreach (var x in rMethod.GetParameters())
                pT.Add(x.ParameterType);

            var dMethod = new DynamicMethod(CMethod.Name, rType, pT.ToArray(), Module, true);

            var ILGen = dMethod.GetILGenerator();

            for (int i = 0; i < Params.Length; i++)
                ILGen.Emit(System.Reflection.Emit.OpCodes.Ldarg, i);

            ILGen.Emit(System.Reflection.Emit.OpCodes.Call, ((MethodInfo)rMethod).MakeGenericMethod(new[] { typeof(string) }));

            ILGen.Emit(System.Reflection.Emit.OpCodes.Ret);

            return dMethod.Invoke(null, Params);
        }
    }
}