/*
 * Copyright (C) 2016 Vyacheslav Shevelyov (slavash at aha dot ru)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SingularSys.Jep;
using SingularSys.Jep.Functions;
using SingularSys.Jep.Types;

namespace Epanet.MSX.Structures {

    public class MathExpr {

        private readonly JepInstance jeb;
        private readonly Dictionary<Variable, int> variables;
        private static readonly Regex PatNumber = new Regex("^[-+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?$");

        private static readonly Regex SplitWords = new Regex("[^a-zA-Z_0-9]"); // "[\\W]"

        class MathExp_exp:PostfixMathCommand {
            public MathExp_exp():base(1) { }
            public double operation(double[] @params) { return Math.Exp(@params[0]); }
        }

        class MathExp_sgn:PostfixMathCommand {
            public MathExp_sgn():base(1) { }
            public double operation(double[] @params) { return Epanet.Util.Utilities.GetSignal(@params[0]); }
        }

        class MathExp_acot:PostfixMathCommand {
            public MathExp_acot():base(1) { }
            public double operation(double[] @params) { return (Math.PI / 2) - Math.Atan(@params[0]); }
        }

        class MathExp_sinh:PostfixMathCommand {
            public MathExp_sinh():base(1) { }
            public double operation(double[] @params) { return Math.Sinh(@params[0]); }
        }

        class MathExp_cosh:PostfixMathCommand {
            public MathExp_cosh():base(1) { }
            public double operation(double[] @params) { return Math.Cosh(@params[0]); }
        }

        class MathExp_tanh:PostfixMathCommand {
            public MathExp_tanh():base(1) { }
            public double operation(double[] @params) { return Math.Tanh(@params[0]); }
        }

        class MathExp_coth:PostfixMathCommand {
            public MathExp_coth():base(1) { }

            public double operation(double[] @params) {
                return (Math.Exp(@params[0]) + Math.Exp(-@params[0])) / (Math.Exp(@params[0]) - Math.Exp(-@params[0]));
            }
        }

        class MathExp_log10:PostfixMathCommand {
            public MathExp_log10():base(1) { }
            public double operation(double[] @params) { return Math.Log10(@params[0]); }
        }

        class MathExp_step:PostfixMathCommand {
            public MathExp_step():base(1) { }
            public double operation(double[] @params) { return @params[0] <= 0.0 ? 0.0 : 1.0; }
        }

        public MathExpr() {
            this.jeb = new JepInstance();
            this.jeb.AddFunction("exp", new MathExp_exp());
            this.jeb.AddFunction("sgn", new MathExp_sgn());
            this.jeb.AddFunction("acot", new MathExp_acot());
            this.jeb.AddFunction("sinh", new MathExp_sinh());
            this.jeb.AddFunction("cosh", new MathExp_cosh());
            this.jeb.AddFunction("tanh", new MathExp_tanh());
            this.jeb.AddFunction("coth", new MathExp_coth());
            this.jeb.AddFunction("log10", new MathExp_log10());
            this.jeb.AddFunction("step", new MathExp_step());
            this.jeb.AddStandardConstants();
            // jeb.addStandardFunctions();
            this.variables = new Dictionary<Variable, int>();
        }

        //public double evaluate(Chemical chem, bool pipe){
        //    double res = 0;
        //
        //    foreach ( Map.Entry<ASTVarNode,Integer> entry  in   variables.entrySet()){
        //        if(pipe)
        //            entry.getKey().setValue(chem.getPipeVariableValue(entry.getValue()) );
        //        else
        //            entry.getKey().setValue(chem.getTankVariableValue(entry.getValue()) );
        //    }
        //
        //    try {
        //        return topNode.getValue();
        //    } catch (org.cheffo.jeplite.ParseException e) {
        //        return 0;
        //    }
        //}

        public double EvaluatePipeExp(IExprVariable var) {
            foreach (var entry  in   this.variables) {
                var jd = new JepDouble(var.GetPipeVariableValue(entry.Value));
                entry.Key.SetValue(jd);
            }

            try {
                return this.jeb.EvaluateD();
            }
            catch (EvaluationException) {
                return 0;
            }

        }

        public double EvaluateTankExp(IExprVariable var) {
            foreach (var entry  in   this.variables) {
                var jd = new JepDouble(var.GetTankVariableValue(entry.Value));
                entry.Key.SetValue(jd);
            }

            try {
                return this.jeb.EvaluateD();
            }
            catch (EvaluationException) {
                return 0;
            }
        }

        public delegate int GetIndexDelegate(string id);

        public static MathExpr Create(string formula, GetIndexDelegate var) {
            MathExpr expr = new MathExpr();
            string[] colWords = SplitWords.Split(formula);

            var mathFuncs = new[] {
                "cos", "sin", "tan", "cot", "abs", "sgn",
                "sqrt", "log", "exp", "asin", "acos", "atan",
                "acot", "sinh", "cosh", "tanh", "coth", "log10",
                "step"
            };


            foreach (string word  in  colWords) {
                if (word.Trim().Length == 0) continue;

                if (!PatNumber.IsMatch(word)) { // if it isn't a number
                    // its a word
                    if (Array.IndexOf(mathFuncs, word.ToLower()) == -1) {
                        Variable variable = expr.jeb.AddVariable(word, 0.0d);
                        expr.variables[variable] = var(word);
                    }
                    else // it's a function
                    {
                        // it's an upper case function, convert to lower case
                        if (!word.Equals(word.ToLower()))
                            formula = formula.Replace(word, word.ToLower());
                    }
                }
            }

            try {
                expr.jeb.Parse(formula);
            }
            catch (ParseException) {
                return null;
            }

            return expr;
        }



    }

}