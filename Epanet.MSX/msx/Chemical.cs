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
using Epanet.MSX.Solvers;
using Epanet.MSX.Structures;

namespace Epanet.MSX {

    public class Chemical:JacobianInterface, IExprVariable {

        public void LoadDependencies(EpanetMSX epa) { msx = epa.Network; }

        private Network msx;
        private rk5 rk5Solver;
        private ros2 ros2Solver;
        private Newton newton;

        //  Constants
        ///<summary>Max. number of iterations used in nonlinear equation solver</summary>
        private const int MAXIT = 20;
        ///<summary>Number of significant digits in nonlinear equation solver error</summary>
        private const int NUMSIG = 3;

        ///<summary>Current water quality segment</summary>
        private Pipe theSeg;
        ///<summary>Index of current link</summary>
        private int theLink;
        ///<summary>Index of current node</summary>
        private int theNode;
        ///<summary>Total number of species</summary>
        private int numSpecies;
        ///<summary>Number of species with pipe rates</summary>
        private int numPipeRateSpecies;
        ///<summary>Number of species with tank rates</summary>
        private int numTankRateSpecies;
        ///<summary>Number of species with pipe formulas</summary>
        private int numPipeFormulaSpecies;
        ///<summary>Number of species with tank formulas</summary>
        private int numTankFormulaSpecies;
        ///<summary>Number of species with pipe equilibria</summary>
        private int numPipeEquilSpecies;
        ///<summary>Number of species with tank equilibria</summary>
        private int numTankEquilSpecies;
        ///<summary>Species governed by pipe reactions</summary>
        private int[] pipeRateSpecies;
        ///<summary>Species governed by tank reactions</summary>
        private int[] tankRateSpecies;
        ///<summary>Species governed by pipe equilibria</summary>
        private int[] pipeEquilSpecies;
        ///<summary>Species governed by tank equilibria</summary>
        private int[] tankEquilSpecies;
        ///<summary>Last index of given type of variable</summary>
        private int[] lastIndex;
        ///<summary>Absolute concentration tolerances</summary>
        private double[] atol;
        ///<summary>Relative concentration tolerances</summary>
        private double[] rtol;
        ///<summary>Rate species concentrations</summary>
        private double[] yrate;
        ///<summary>Equilibrium species concentrations</summary>
        private double[] yequil;
        ///<summary>Values of hydraulic variables</summary>
        private double[] hydVar;


        ///<summary>opens the multi-species chemistry system.</summary>
        public ErrorCodeType MSXchem_open() {
            hydVar = new double[(int)HydVarType.MAX_HYD_VARS];
            lastIndex = new int[(int)ObjectTypes.MAX_OBJECTS];

            pipeRateSpecies = null;
            tankRateSpecies = null;
            pipeEquilSpecies = null;
            tankEquilSpecies = null;
            atol = null;
            rtol = null;
            yrate = null;
            yequil = null;
            numSpecies = msx.Nobjects[(int)ObjectTypes.SPECIES];
            int size = numSpecies + 1;
            pipeRateSpecies = new int[size];
            tankRateSpecies = new int[size];
            pipeEquilSpecies = new int[size];
            tankEquilSpecies = new int[size];
            atol = new double[size];
            rtol = new double[size];
            yrate = new double[size];
            yequil = new double[size];

            // Assign species to each type of chemical expression
            SetSpeciesChemistry();
            int numPipeExpr = numPipeRateSpecies + numPipeFormulaSpecies + numPipeEquilSpecies;
            int numTankExpr = numTankRateSpecies + numTankFormulaSpecies + numTankEquilSpecies;

            // Use pipe chemistry for tanks if latter was not supplied
            if (numTankExpr == 0) {
                SetTankChemistry();
                numTankExpr = numPipeExpr;
            }

            // Check if enough equations were specified
            var numWallSpecies = 0;
            var numBulkSpecies = 0;
            for (int i = 1; i <= numSpecies; i++) {
                if (msx.Species[i].Type == SpeciesType.WALL) numWallSpecies++;
                if (msx.Species[i].Type == SpeciesType.BULK) numBulkSpecies++;
            }
            if (numPipeExpr != numSpecies) return ErrorCodeType.ERR_NUM_PIPE_EXPR;
            if (numTankExpr != numBulkSpecies) return ErrorCodeType.ERR_NUM_TANK_EXPR;

            // Open the ODE solver;
            // arguments are max. number of ODE's,
            // max. number of steps to be taken,
            // 1 if automatic step sizing used (or 0 if not used)

            switch (msx.Solver) {
            case SolverType.RK5:
                rk5Solver = new rk5();
                rk5Solver.rk5_open(numSpecies, 1000, 1);
                break;
            case SolverType.ROS2:
                ros2Solver = new ros2();
                ros2Solver.ros2_open(numSpecies, 1);
                break;
            }

            // Open the algebraic eqn. solver
            int m = Math.Max(numPipeEquilSpecies, numTankEquilSpecies);
            newton = new Newton();
            newton.newton_open(m);

            // Assign entries to LastIndex array
            lastIndex[(int)ObjectTypes.SPECIES] = msx.Nobjects[(int)ObjectTypes.SPECIES];
            lastIndex[(int)ObjectTypes.TERM] = lastIndex[(int)ObjectTypes.SPECIES]
                                                              + msx.Nobjects[(int)ObjectTypes.TERM];
            lastIndex[(int)ObjectTypes.PARAMETER] = lastIndex[(int)ObjectTypes.TERM]
                                                                   + msx.Nobjects[
                                                                             (int)ObjectTypes.PARAMETER];
            lastIndex[(int)ObjectTypes.CONSTANT] = lastIndex[(int)ObjectTypes.PARAMETER]
                                                                  + msx.Nobjects[
                                                                            (int)ObjectTypes.CONSTANT];

            return 0;
        }

        ///<summary>computes reactions in all pipes and tanks.</summary>
        public ErrorCodeType MSXchem_react(long dt) {

            ErrorCodeType errcode = 0;

            // Save tolerances of pipe rate species
            for (int i = 1; i <= numPipeRateSpecies; i++) {
                int j = pipeRateSpecies[i];
                atol[i] = msx.Species[j].ATol;
                rtol[i] = msx.Species[j].RTol;
            }

            // Examine each link
            for (int i = 1; i <= msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                // Skip non-pipe links
                if (msx.Link[i].Len == 0.0) continue;

                // Evaluate hydraulic variables
                EvalHydVariables(i);

                // Compute pipe reactions
                errcode = EvalPipeReactions(i, dt);
                if (errcode != 0) return errcode;
            }

            // Save tolerances of tank rate species
            for (int i = 1; i <= numTankRateSpecies; i++) {
                int j = tankRateSpecies[i];
                atol[i] = msx.Species[j].ATol;
                rtol[i] = msx.Species[j].RTol;
            }

            // Examine each tank
            for (int i = 1; i <= msx.Nobjects[(int)ObjectTypes.TANK]; i++) {
                // Skip reservoirs
                if (msx.Tank[i].A == 0.0) continue;

                // Compute tank reactions
                errcode = EvalTankReactions(i, dt);
                if (errcode != 0) return errcode;
            }
            return errcode;
        }


        /// <summary>Computes equilibrium concentrations for a set of chemical species.</summary>
        public ErrorCodeType MSXchem_equil(ObjectTypes zone, double[] c) {
            ErrorCodeType errcode = 0;
            if (zone == ObjectTypes.LINK) {
                if (numPipeEquilSpecies > 0) errcode = EvalPipeEquil(c);
                EvalPipeFormulas(c);
            }
            if (zone == ObjectTypes.NODE) {
                if (numTankEquilSpecies > 0) errcode = EvalTankEquil(c);
                EvalTankFormulas(c);
            }
            return errcode;
        }

        ///<summary>Determines which species are described by reaction rate expressions, equilibrium expressions, or simple formulas.</summary>
        private void SetSpeciesChemistry() {
            numPipeRateSpecies = 0;
            numPipeFormulaSpecies = 0;
            numPipeEquilSpecies = 0;
            numTankRateSpecies = 0;
            numTankFormulaSpecies = 0;
            numTankEquilSpecies = 0;
            for (int i = 1; i <= numSpecies; i++) {
                switch (msx.Species[i].PipeExprType) {
                case ExpressionType.RATE:
                    numPipeRateSpecies++;
                    pipeRateSpecies[numPipeRateSpecies] = i;
                    break;

                case ExpressionType.FORMULA:
                    numPipeFormulaSpecies++;
                    break;

                case ExpressionType.EQUIL:
                    numPipeEquilSpecies++;
                    pipeEquilSpecies[numPipeEquilSpecies] = i;
                    break;
                }
                switch (msx.Species[i].TankExprType) {
                case ExpressionType.RATE:
                    numTankRateSpecies++;
                    tankRateSpecies[numTankRateSpecies] = i;
                    break;

                case ExpressionType.FORMULA:
                    numTankFormulaSpecies++;
                    break;

                case ExpressionType.EQUIL:
                    numTankEquilSpecies++;
                    tankEquilSpecies[numTankEquilSpecies] = i;
                    break;
                }
            }
        }


        ///<summary>Assigns pipe chemistry expressions to tank chemistry for each chemical species.</summary>
        private void SetTankChemistry() {
            for (int i = 1; i <= numSpecies; i++) {
                msx.Species[i].TankExpr = msx.Species[i].PipeExpr;
                msx.Species[i].TankExprType = msx.Species[i].PipeExprType;
            }

            numTankRateSpecies = numPipeRateSpecies;

            for (int i = 1; i <= numTankRateSpecies; i++) {
                tankRateSpecies[i] = pipeRateSpecies[i];
            }

            numTankFormulaSpecies = numPipeFormulaSpecies;
            numTankEquilSpecies = numPipeEquilSpecies;

            for (int i = 1; i <= numTankEquilSpecies; i++) {
                tankEquilSpecies[i] = pipeEquilSpecies[i];
            }
        }

        ///<summary>Retrieves current values of hydraulic variables for the current link being analyzed.</summary>
        private void EvalHydVariables(int k) {
            double dh; // headloss in ft
            double diam = msx.Link[k].Diam; // diameter in ft
            double av; // area per unit volume

            //  pipe diameter in user's units (ft or m)
            hydVar[(int)HydVarType.DIAMETER] = diam * msx.Ucf[(int)UnitsType.LENGTH_UNITS];

            //  flow rate in user's units
            hydVar[(int)HydVarType.FLOW] = Math.Abs(msx.Q[k])
                                                          * msx.Ucf[(int)UnitsType.FLOW_UNITS];

            //  flow velocity in ft/sec
            if (diam == 0.0) hydVar[(int)HydVarType.VELOCITY] = 0.0;
            else
                hydVar[(int)HydVarType.VELOCITY] = Math.Abs(msx.Q[k]) * 4.0 / Constants.PI
                                                                  / (diam * diam);

            //  Reynolds number
            hydVar[(int)HydVarType.REYNOLDS] = hydVar[(int)HydVarType.VELOCITY] * diam
                                                              / Constants.VISCOS;

            //  flow velocity in user's units (ft/sec or m/sec)
            hydVar[(int)HydVarType.VELOCITY] *= msx.Ucf[(int)UnitsType.LENGTH_UNITS];

            //  Darcy Weisbach friction factor
            if (msx.Link[k].Len == 0.0) hydVar[(int)HydVarType.FRICTION] = 0.0;
            else {
                dh = Math.Abs(msx.H[msx.Link[k].N1] - msx.H[msx.Link[k].N2]);
                hydVar[(int)HydVarType.FRICTION] = 39.725 * dh * Math.Pow(diam, 5)
                                                                  / msx.Link[k].Len
                                                                  / (msx.Q[k] * msx.Q[k]);
            }

            // Shear velocity in user's units (ft/sec or m/sec)
            hydVar[(int)HydVarType.SHEAR] = hydVar[(int)HydVarType.VELOCITY] *
                                                           Math.Sqrt(
                                                               hydVar[(int)HydVarType.FRICTION] / 8.0);

            // Pipe surface area / volume in area_units/L
            hydVar[(int)HydVarType.AREAVOL] = 1.0;
            if (diam > 0.0) {
                av = 4.0 / diam; // ft2/ft3
                av *= msx.Ucf[(int)UnitsType.AREA_UNITS]; // area_units/ft3
                av /= Constants.LperFT3; // area_units/L
                hydVar[(int)HydVarType.AREAVOL] = av;
            }

            hydVar[(int)HydVarType.ROUGHNESS] = msx.Link[k].Roughness;
                //Feng Shang, Bug ID 8,  01/29/2008
        }


        ///<summary>Updates species concentrations in each WQ segment of a pipe after reactions occur over time step dt.</summary>
        private ErrorCodeType EvalPipeReactions(int k, long dt) {
            ErrorCodeType errcode = 0;
            int ierr;
            double tstep = (double)dt / msx.Ucf[(int)UnitsType.RATE_UNITS];
            double c, dc;
            double[] dh = new double[1];
            // Start with the most downstream pipe segment

            theLink = k;
            foreach (Pipe seg  in  msx.Segments[theLink]) {
                theSeg = seg;
                // Store all segment species concentrations in MSX.C1

                for (int i = 1; i <= numSpecies; i++) msx.C1[i] = theSeg.C[i];
                ierr = 0;

                // React each reacting species over the time step

                if (dt > 0.0) {
                    // Euler integrator
                    if (msx.Solver == SolverType.EUL) {
                        for (int i = 1; i <= numPipeRateSpecies; i++) {
                            int m = pipeRateSpecies[i];

                            //dc = mathexpr_eval(MSX.Species[m].getPipeExpr(),
                            //        getPipeVariableValue) * tstep;
                            dc = msx.Species[m].PipeExpr.EvaluatePipeExp(this) * tstep;
                            //dc = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface() {
                            //    public double getValue(int id) {return getPipeVariableValue(id);}
                            //    public int getIndex(string id) {return 0;}
                            //})* tstep;

                            c = theSeg.C[m] + dc;
                            theSeg.C[m] = Math.Max(c, 0.0);
                        }
                    }

                    // Other integrators
                    else {
                        // Place current concentrations of species that react in vector Yrate

                        for (int i = 1; i <= numPipeRateSpecies; i++) {
                            int m = pipeRateSpecies[i];
                            yrate[i] = theSeg.C[m];
                        }
                        dh[0] = theSeg.Hstep;

                        // integrate the set of rate equations

                        // Runge-Kutta integrator
                        if (msx.Solver == SolverType.RK5)
                            ierr = rk5Solver.rk5_integrate(
                                           yrate,
                                           numPipeRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           atol,
                                           rtol,
                                           this,
                                           Operation.PIPES_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction(){
                        //    public void solve(double t, double[] y, int n, double[] f){getPipeDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                        //});

                        // Rosenbrock integrator
                        if (msx.Solver == SolverType.ROS2)
                            ierr = ros2Solver.ros2_integrate(
                                           yrate,
                                           numPipeRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           atol,
                                           rtol,
                                           this,
                                           Operation.PIPES_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getPipeDcDt(t, y, n, f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                        //});

                        // save new concentration values of the species that reacted

                        for (int i = 1; i <= numSpecies; i++)
                            theSeg.C[i] = msx.C1[i];

                        for (int i = 1; i <= numPipeRateSpecies; i++) {
                            int m = pipeRateSpecies[i];
                            theSeg.C[m] = Math.Max(yrate[i], 0.0);
                        }
                        theSeg.Hstep = dh[0];
                    }
                    if (ierr < 0)
                        return ErrorCodeType.ERR_INTEGRATOR;
                }

                // Compute new equilibrium concentrations within segment

                errcode = MSXchem_equil(ObjectTypes.LINK, theSeg.C);

                if (errcode != 0)
                    return errcode;

                // Move to the segment upstream of the current one

                //TheSeg = TheSeg->prev;
            }
            return errcode;
        }

        ///<summary>Updates species concentrations in a given storage tank after reactions occur over time step dt.</summary>
        private ErrorCodeType EvalTankReactions(int k, long dt) {
            ErrorCodeType errcode = 0;
            double tstep = ((double)dt) / msx.Ucf[(int)UnitsType.RATE_UNITS];
            double c, dc;
            double[] dh = new double[1];

            // evaluate each volume segment in the tank

            theNode = msx.Tank[k].Node;
            int i = msx.Nobjects[(int)ObjectTypes.LINK] + k;
            //TheSeg = MSX.Segments[i];
            //while ( TheSeg )
            foreach (Pipe seg  in  msx.Segments[i]) {
                theSeg = seg;

                // store all segment species concentrations in MSX.C1

                for (int j = 1; j <= numSpecies; j++) msx.C1[j] = theSeg.C[j];
                var ierr = 0;

                // react each reacting species over the time step
                if (dt > 0.0) {
                    if (msx.Solver == SolverType.EUL) {
                        for (i = 1; i <= numTankRateSpecies; i++) {
                            int j = tankRateSpecies[i];
                            //dc = tstep * mathexpr_eval(MSX.Species[m].getTankExpr(),
                            //        getTankVariableValue);
                            dc = tstep * msx.Species[j].TankExpr.EvaluateTankExp(this);
                            //dc = tstep * MSX.Species[m].getTankExpr().evaluate(
                            //        new VariableInterface(){
                            //            public double getValue(int id) {return getTankVariableValue(id);}
                            //            public int getIndex(string id) {return 0;}
                            //        });
                            c = theSeg.C[j] + dc;
                            theSeg.C[j] = Math.Max(c, 0.0);
                        }
                    }

                    else {
                        for (i = 1; i <= numTankRateSpecies; i++) {
                            int j = tankRateSpecies[i];
                            yrate[i] = msx.Tank[k].C[j];
                        }
                        dh[0] = msx.Tank[k].Hstep;

                        if (msx.Solver == SolverType.RK5)
                            ierr = rk5Solver.rk5_integrate(
                                           yrate,
                                           numTankRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           atol,
                                           rtol,
                                           this,
                                           Operation.TANKS_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                        //} );

                        if (msx.Solver == SolverType.ROS2)
                            ierr = ros2Solver.ros2_integrate(
                                           yrate,
                                           numTankRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           atol,
                                           rtol,
                                           this,
                                           Operation.TANKS_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                        //} );

                        for (int j = 1; j <= numSpecies; j++) theSeg.C[j] = msx.C1[j];
                        for (i = 1; i <= numTankRateSpecies; i++) {
                            int j = tankRateSpecies[i];
                            theSeg.C[j] = Math.Max(yrate[i], 0.0);
                        }
                        theSeg.Hstep = dh[0];
                    }
                    if (ierr < 0)
                        return ErrorCodeType.ERR_INTEGRATOR;
                }

                // compute new equilibrium concentrations within segment
                errcode = MSXchem_equil(ObjectTypes.NODE, theSeg.C);

                if (errcode != 0)
                    return errcode;
            }
            return errcode;
        }

        ///<summary>computes equilibrium concentrations for water in a pipe segment.</summary>
        private ErrorCodeType EvalPipeEquil(double[] c) {
            for (int i = 1; i <= numSpecies; i++) msx.C1[i] = c[i];

            for (int i = 1; i <= numPipeEquilSpecies; i++) {
                int j = pipeEquilSpecies[i];
                yequil[i] = c[j];
            }

            int errcode = newton.newton_solve(
                                  yequil,
                                  numPipeEquilSpecies,
                                  MAXIT,
                                  NUMSIG,
                                  this,
                                  Operation.PIPES_EQUIL);

            if (errcode < 0) return ErrorCodeType.ERR_NEWTON;
            for (int i = 1; i <= numPipeEquilSpecies; i++) {
                int j = pipeEquilSpecies[i];
                c[j] = yequil[i];
                msx.C1[j] = c[j];
            }
            return 0;
        }


        ///<summary>computes equilibrium concentrations for water in a tank.</summary>
        private ErrorCodeType EvalTankEquil(double[] c) {

            for (int i = 1; i <= numSpecies; i++) msx.C1[i] = c[i];
            for (int i = 1; i <= numTankEquilSpecies; i++) {
                int j = tankEquilSpecies[i];
                yequil[i] = c[j];
            }
            int errcode = newton.newton_solve(
                                  yequil,
                                  numTankEquilSpecies,
                                  MAXIT,
                                  NUMSIG,
                                  this,
                                  Operation.TANKS_EQUIL);
            //new JacobianFunction() {
            //    public void solve(double t, double[] y, int n, double[] f) {getTankEquil(t,y,n,f);}
            //    public void solve(double t, double[] y, int n, double[] f, int off) {
            //        System.out.println("Jacobian Unused");}
            //});

            if (errcode < 0) return ErrorCodeType.ERR_NEWTON;

            for (int i = 1; i <= numTankEquilSpecies; i++) {
                int j = tankEquilSpecies[i];
                c[j] = yequil[i];
                msx.C1[j] = c[j];
            }
            return 0;
        }

        ///<summary>
        /// Evaluates species concentrations in a pipe segment that are simple
        /// formulas involving other known species concentrations.
        /// </summary>
        private void EvalPipeFormulas(double[] c) {
            for (int i = 1; i <= numSpecies; i++) msx.C1[i] = c[i];

            for (int i = 1; i <= numSpecies; i++) {
                if (msx.Species[i].PipeExprType == ExpressionType.FORMULA) {
                    c[i] = msx.Species[i].PipeExpr.EvaluatePipeExp(this);
                    //c[m] = MSX.Species[m].getPipeExpr().evaluate( new VariableInterface(){
                    //    public double getValue(int id){return getPipeVariableValue(id);}
                    //    public int getIndex(string id){return 0;}
                    //});
                }
            }
        }

        ///<summary>
        /// Evaluates species concentrations in a tank that are simple
        /// formulas involving other known species concentrations. 
        /// </summary>
        private void EvalTankFormulas(double[] c) {

            for (int i = 1; i <= numSpecies; i++) msx.C1[i] = c[i];

            for (int i = 1; i <= numSpecies; i++) {
                if (msx.Species[i].TankExprType == ExpressionType.FORMULA) {
                    c[i] = msx.Species[i].PipeExpr.EvaluateTankExp(this);
                    //c[m] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
                    //    public double getValue(int id){return getTankVariableValue(id);}
                    //    public int getIndex(string id){return 0;}
                    //});
                }
            }
        }

        ///<summary>Finds the value of a species, a parameter, or a constant for the pipe link being analyzed.</summary>
        public double GetPipeVariableValue(int i) {
            // WQ species have index i between 1 & # of species
            // and their current values are stored in vector MSX.C1
            if (i <= lastIndex[(int)ObjectTypes.SPECIES]) {
                // If species represented by a formula then evaluate it
                if (msx.Species[i].PipeExprType != ExpressionType.FORMULA) return msx.C1[i];

                return msx.Species[i].PipeExpr.EvaluatePipeExp(this);
            }

            if (i <= lastIndex[(int)ObjectTypes.TERM]) // intermediate term expressions come next
            {
                i -= lastIndex[(int)ObjectTypes.TERM - 1];
                return msx.Term[i].Expr.EvaluatePipeExp(this);
                //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id){return getPipeVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }

            if (i <= lastIndex[(int)ObjectTypes.PARAMETER]) // reaction parameter indexes come after that
            {
                i -= lastIndex[(int)ObjectTypes.PARAMETER - 1];
                return msx.Link[theLink].Param[i];
            }

            if (i <= lastIndex[(int)ObjectTypes.CONSTANT]) // followed by constants
            {
                i -= lastIndex[(int)ObjectTypes.CONSTANT - 1];
                return msx.Const[i].Value;
            }

            i -= lastIndex[(int)ObjectTypes.CONSTANT];
            if (i < (int)HydVarType.MAX_HYD_VARS) return hydVar[i];

            return 0.0;
        }

        ///<summary>Finds the value of a species, a parameter, or a constant for the current node being analyzed.</summary>
        public double GetTankVariableValue(int i) {
            // WQ species have index i between 1 & # of species and their current values are stored in vector MSX.C1
            if (i <= lastIndex[(int)ObjectTypes.SPECIES]) {
                // If species represented by a formula then evaluate it
                if (msx.Species[i].TankExprType == ExpressionType.FORMULA) {
                    return msx.Species[i].TankExpr.EvaluateTankExp(this);
                    //return MSX.Species[i].getTankExpr().evaluate(new VariableInterface() {
                    //    public double getValue(int id) {return getTankVariableValue(id);}
                    //    public int getIndex(string id) {return 0;}});
                }
                else // Otherwise return the current concentration
                    return msx.C1[i];
            }
            else if (i <= lastIndex[(int)ObjectTypes.TERM]) // Intermediate term expressions come next
            {
                i -= lastIndex[(int)ObjectTypes.TERM - 1];
                return msx.Term[i].Expr.EvaluateTankExp(this);
                //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id) {return getTankVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
            else if (i <= lastIndex[(int)ObjectTypes.PARAMETER])
                // Next come reaction parameters associated with Tank nodes
            {
                i -= lastIndex[(int)ObjectTypes.PARAMETER - 1];
                int j = msx.Node[theNode].Tank;
                return j > 0 ? msx.Tank[j].Param[i] : 0.0;
            }
            else if (i <= lastIndex[(int)ObjectTypes.CONSTANT]) // and then come constants
            {
                i -= lastIndex[(int)ObjectTypes.CONSTANT - 1];
                return msx.Const[i].Value;
            }
            else
                return 0.0;
        }


        ///<summary>finds reaction rate (dC/dt) for each reacting species in a pipe.</summary>
        private void GetPipeDcDt(double t, double[] y, int n, double[] deriv) {


            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = pipeRateSpecies[i];
                msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (msx.Coupling == CouplingType.FULL_COUPLING) {
                if (MSXchem_equil(ObjectTypes.LINK, msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i] = 0.0;
                    return;
                }
            }

            // Evaluate each pipe reaction expression
            for (int i = 1; i <= n; i++) {
                int m = pipeRateSpecies[i];
                //deriv[i] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
                deriv[i] = msx.Species[m].PipeExpr.EvaluatePipeExp(this);
                //deriv[i] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id) {return getPipeVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        private void GetPipeDcDt(double t, double[] y, int n, double[] deriv, int off) {

            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = pipeRateSpecies[i];
                msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use

            if (msx.Coupling == CouplingType.FULL_COUPLING) {
                if (MSXchem_equil(ObjectTypes.LINK, msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i + off] = 0.0;
                    return;
                }
            }

            // evaluate each pipe reaction expression
            for (int i = 1; i <= n; i++) {
                int m = pipeRateSpecies[i];
                //deriv[i+off] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
                deriv[i + off] = msx.Species[m].PipeExpr.EvaluatePipeExp(this);
                //deriv[i+off] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id) {return getPipeVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        ///<summary>finds reaction rate (dC/dt) for each reacting species in a tank.</summary>
        private void GetTankDcDt(double t, double[] y, int n, double[] deriv) {

            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = tankRateSpecies[i];
                msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (msx.Coupling == CouplingType.FULL_COUPLING) {
                if (MSXchem_equil(ObjectTypes.NODE, msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i] = 0.0;
                    return;
                }
            }

            // Evaluate each tank reaction expression
            for (int i = 1; i <= n; i++) {
                int m = tankRateSpecies[i];
                deriv[i] = msx.Species[m].TankExpr.EvaluateTankExp(this);
                //deriv[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id); }
                //    public int getIndex(string id) {return 0;}
                //}); //mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
            }
        }

        private void GetTankDcDt(double t, double[] y, int n, double[] deriv, int off) {


            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1

            for (int i = 1; i <= n; i++) {
                int m = tankRateSpecies[i];
                msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (msx.Coupling == CouplingType.FULL_COUPLING) {
                if (MSXchem_equil(ObjectTypes.NODE, msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i + off] = 0.0;
                    return;
                }
            }

            // Evaluate each tank reaction expression
            for (int i = 1; i <= n; i++) {
                int m = tankRateSpecies[i];
                //deriv[i+off] = mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
                deriv[i + off] = msx.Species[m].TankExpr.EvaluateTankExp(this);
                //deriv[i+off] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id); }
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        ///<summary>Evaluates equilibrium expressions for pipe chemistry.</summary>
        private void GetPipeEquil(double t, double[] y, int n, double[] f) {
            // Assign species concentrations to their proper positions in the global
            // concentration vector MSX.C1

            for (int i = 1; i <= n; i++) {
                int m = pipeEquilSpecies[i];
                msx.C1[m] = y[i];
            }

            // Evaluate each pipe equilibrium expression

            for (int i = 1; i <= n; i++) {
                int m = pipeEquilSpecies[i];
                f[i] = msx.Species[m].PipeExpr.EvaluatePipeExp(this);
                //f[i] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id){return getPipeVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }
        }


        ///<summary>Evaluates equilibrium expressions for tank chemistry.</summary>
        private void GetTankEquil(double t, double[] y, int n, double[] f) {
            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = tankEquilSpecies[i];
                msx.C1[m] = y[i];
            }

            // Evaluate each tank equilibrium expression
            for (int i = 1; i <= n; i++) {
                int m = tankEquilSpecies[i];
                f[i] = msx.Species[m].TankExpr.EvaluateTankExp(this);
                //f[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        public override void solve(double t, double[] y, int n, double[] f, int off, Operation op) {
            switch (op) {

            case Operation.PIPES_DC_DT_CONCENTRATIONS:
                GetPipeDcDt(t, y, n, f, off);
                break;
            case Operation.TANKS_DC_DT_CONCENTRATIONS:
                GetTankDcDt(t, y, n, f, off);
                break;
            case Operation.PIPES_EQUIL:
                GetPipeEquil(t, y, n, f);
                break;
            case Operation.TANKS_EQUIL:
                GetTankDcDt(t, y, n, f);
                break;
            }
        }
    }

}