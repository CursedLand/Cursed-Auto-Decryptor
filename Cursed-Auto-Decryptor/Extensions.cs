
#region Usings
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace Cursed_Auto_Decryptor
{
    internal static class Extensions
    {
        public static void NopInstruction(this Instruction i) {
            i.OpCode = OpCodes.Nop;
            i.Operand = null;
        }
        public static void ExpandBody(this CilBody ParentBody, ParameterList Parameters) {
            ParentBody.SimplifyBranches();
            ParentBody.SimplifyMacros(Parameters);
        }
        public static void OptimzeBody(this CilBody ParentBody) {
            ParentBody.OptimizeBranches();
            ParentBody.OptimizeMacros();
        }
    }
}