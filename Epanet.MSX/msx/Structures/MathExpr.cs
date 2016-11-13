/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using SingularSys.Jep.Parser;
using SingularSys.Jep.Types;

namespace org.addition.epanet.msx.Structures {

public class MathExpr {

    private readonly JepInstance jeb;
    private readonly Dictionary<Variable, int> variables;
    private SimpleNode topNode;
    private static readonly Regex PAT_NUMBER = new Regex("^[-+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?$");

    private static readonly Regex splitWords = new Regex("[^a-zA-Z_0-9]"); // "[\\W]"
    
    class MathExp_exp : PostfixMathCommand{
        public MathExp_exp():base(1) {}
        public double operation(double[] @params) { return Math.Exp(@params[0]); }
    }

    class MathExp_sgn : PostfixMathCommand{
        public MathExp_sgn():base(1) {}
        public double operation(double[] @params) {
            return util.Utilities.getSignal(@params[0]);
        }
    }

    class MathExp_acot : PostfixMathCommand{
        public MathExp_acot():base(1) {}
        public double operation(double[] @params) {
            return  (Math.PI / 2) - Math.Atan(@params[0]);
        }
    }

    class MathExp_sinh : PostfixMathCommand{
        public MathExp_sinh():base(1) {}
        public double operation(double[] @params) {
            return Math.Sinh(@params[0]);
        }
    }

    class MathExp_cosh : PostfixMathCommand{
        public MathExp_cosh():base(1) {}
        public double operation(double[] @params) {
           return Math.Cosh(@params[0]);
        }
    }

    class MathExp_tanh : PostfixMathCommand{
        public MathExp_tanh():base(1) {}
        public double operation(double[] @params) {
            return Math.Tanh(@params[0]);
        }
    }

    class MathExp_coth : PostfixMathCommand{
        public MathExp_coth():base(1) {}
        public double operation(double[] @params) {
            return  (Math.Exp(@params[0])+Math.Exp(-@params[0]))/(Math.Exp(@params[0])-Math.Exp(-@params[0]));
        }
    }

    class MathExp_log10 : PostfixMathCommand{
        public MathExp_log10():base(1){}
        public double operation(double[] @params) {
            return  Math.Log10(@params[0]);
        }
    }

    class MathExp_step : PostfixMathCommand{
        public MathExp_step():base(1) {}
        public double operation(double[] @params)
        {
            return @params[0] <= 0.0 ? 0.0 : 1.0;
        }
    }

    public MathExpr(){
        jeb = new JepInstance();
        jeb.AddFunction("exp",new MathExp_exp());
        jeb.AddFunction("sgn",new MathExp_sgn());
        jeb.AddFunction("acot",new  MathExp_acot());
        jeb.AddFunction("sinh",new  MathExp_sinh());
        jeb.AddFunction("cosh",new  MathExp_cosh());
        jeb.AddFunction("tanh",new  MathExp_tanh());
        jeb.AddFunction("coth",new  MathExp_coth());
        jeb.AddFunction("log10",new  MathExp_log10());
        jeb.AddFunction("step",new  MathExp_step());
        jeb.AddStandardConstants();
        // jeb.addStandardFunctions();
        variables = new Dictionary<Variable, int>();
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

    public double evaluatePipeExp(ExprVariable var){
        foreach ( var entry  in   variables) {
            var jd = new JepDouble(var.getPipeVariableValue(entry.Value));
            entry.Key.SetValue(jd);
        }

        try
        {
            return this.jeb.EvaluateD();
        }
        catch (EvaluationException)
        {
            return 0;
        }

    }

    public double evaluateTankExp(ExprVariable var){
        foreach ( var entry  in   variables){
            var jd = new JepDouble(var.getTankVariableValue(entry.Value));
            entry.Key.SetValue(jd);
        }

        try
        {
            return this.jeb.EvaluateD();
        }
        catch (EvaluationException)
        {
            return 0;
        }
    }



    public static MathExpr create(string formula, IVariable var)
    {
        MathExpr expr = new MathExpr();
        string[] colWords = splitWords.Split(formula);

        var mathFuncs = new[]{"cos", "sin", "tan", "cot", "abs", "sgn",
                "sqrt", "log", "exp", "asin", "acos", "atan",
                "acot", "sinh", "cosh", "tanh", "coth", "log10",
                "step"};


        foreach (string word  in  colWords) {
            if (word.Trim().Length == 0) continue;

            if(!PAT_NUMBER.IsMatch(word)){ // if it isn't a number
                // its a word
                if(Array.IndexOf(mathFuncs, (word.ToLower())) == -1)
                {
                    Variable variable = expr.jeb.AddVariable(word, 0.0d);
                    expr.variables[variable] = var.GetIndex(word);
                }
                else  // it's a function
                {
                    // it's an upper case function, convert to lower case
                    if(!word.Equals(word.ToLower()))
                        formula = formula.Replace(word,word.ToLower());
                }
            }
        }

        try
        {
            expr.jeb.Parse(formula);
        }
        catch (SingularSys.Jep.ParseException)
        {
            return null;
        }

        return expr;
    }



}
}