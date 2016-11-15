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

        public void LoadDependencies(EpanetMSX epa) { this.msx = epa.Network; }

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
        public EnumTypes.ErrorCodeType MSXchem_open() {
            int numWallSpecies;
            int numBulkSpecies;
            int numTankExpr;
            int numPipeExpr;

            this.hydVar = new double[(int)EnumTypes.HydVarType.MAX_HYD_VARS];
            this.lastIndex = new int[(int)EnumTypes.ObjectTypes.MAX_OBJECTS];

            this.pipeRateSpecies = null;
            this.tankRateSpecies = null;
            this.pipeEquilSpecies = null;
            this.tankEquilSpecies = null;
            this.atol = null;
            this.rtol = null;
            this.yrate = null;
            this.yequil = null;
            this.numSpecies = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES];
            int size = this.numSpecies + 1;
            this.pipeRateSpecies = new int[size];
            this.tankRateSpecies = new int[size];
            this.pipeEquilSpecies = new int[size];
            this.tankEquilSpecies = new int[size];
            this.atol = new double[size];
            this.rtol = new double[size];
            this.yrate = new double[size];
            this.yequil = new double[size];

            // Assign species to each type of chemical expression
            this.SetSpeciesChemistry();
            numPipeExpr = this.numPipeRateSpecies + this.numPipeFormulaSpecies + this.numPipeEquilSpecies;
            numTankExpr = this.numTankRateSpecies + this.numTankFormulaSpecies + this.numTankEquilSpecies;

            // Use pipe chemistry for tanks if latter was not supplied
            if (numTankExpr == 0) {
                this.SetTankChemistry();
                numTankExpr = numPipeExpr;
            }

            // Check if enough equations were specified
            numWallSpecies = 0;
            numBulkSpecies = 0;
            for (int i = 1; i <= this.numSpecies; i++) {
                if (this.msx.Species[i].Type == EnumTypes.SpeciesType.WALL) numWallSpecies++;
                if (this.msx.Species[i].Type == EnumTypes.SpeciesType.BULK) numBulkSpecies++;
            }
            if (numPipeExpr != this.numSpecies) return EnumTypes.ErrorCodeType.ERR_NUM_PIPE_EXPR;
            if (numTankExpr != numBulkSpecies) return EnumTypes.ErrorCodeType.ERR_NUM_TANK_EXPR;

            // Open the ODE solver;
            // arguments are max. number of ODE's,
            // max. number of steps to be taken,
            // 1 if automatic step sizing used (or 0 if not used)

            switch (this.msx.Solver) {
            case EnumTypes.SolverType.RK5:
                this.rk5Solver = new rk5();
                this.rk5Solver.rk5_open(this.numSpecies, 1000, 1);
                break;
            case EnumTypes.SolverType.ROS2:
                this.ros2Solver = new ros2();
                this.ros2Solver.ros2_open(this.numSpecies, 1);
                break;
            }

            // Open the algebraic eqn. solver
            int m = Math.Max(this.numPipeEquilSpecies, this.numTankEquilSpecies);
            this.newton = new Newton();
            this.newton.newton_open(m);

            // Assign entries to LastIndex array
            this.lastIndex[(int)EnumTypes.ObjectTypes.SPECIES] = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES];
            this.lastIndex[(int)EnumTypes.ObjectTypes.TERM] = this.lastIndex[(int)EnumTypes.ObjectTypes.SPECIES]
                                                              + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TERM];
            this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER] = this.lastIndex[(int)EnumTypes.ObjectTypes.TERM]
                                                                   + this.msx.Nobjects[
                                                                             (int)EnumTypes.ObjectTypes.PARAMETER];
            this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT] = this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER]
                                                                  + this.msx.Nobjects[
                                                                            (int)EnumTypes.ObjectTypes.CONSTANT];

            return 0;
        }

        ///<summary>computes reactions in all pipes and tanks.</summary>
        public EnumTypes.ErrorCodeType MSXchem_react(long dt) {

            EnumTypes.ErrorCodeType errcode = 0;

            // Save tolerances of pipe rate species
            for (int i = 1; i <= this.numPipeRateSpecies; i++) {
                int j = this.pipeRateSpecies[i];
                this.atol[i] = this.msx.Species[j].ATol;
                this.rtol[i] = this.msx.Species[j].RTol;
            }

            // Examine each link
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                // Skip non-pipe links
                if (this.msx.Link[i].Len == 0.0) continue;

                // Evaluate hydraulic variables
                this.EvalHydVariables(i);

                // Compute pipe reactions
                errcode = this.EvalPipeReactions(i, dt);
                if (errcode != 0) return errcode;
            }

            // Save tolerances of tank rate species
            for (int i = 1; i <= this.numTankRateSpecies; i++) {
                int j = this.tankRateSpecies[i];
                this.atol[i] = this.msx.Species[j].ATol;
                this.rtol[i] = this.msx.Species[j].RTol;
            }

            // Examine each tank
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++) {
                // Skip reservoirs
                if (this.msx.Tank[i].A == 0.0) continue;

                // Compute tank reactions
                errcode = this.EvalTankReactions(i, dt);
                if (errcode != 0) return errcode;
            }
            return errcode;
        }


        /// <summary>Computes equilibrium concentrations for a set of chemical species.</summary>
        public EnumTypes.ErrorCodeType MSXchem_equil(EnumTypes.ObjectTypes zone, double[] c) {
            EnumTypes.ErrorCodeType errcode = 0;
            if (zone == EnumTypes.ObjectTypes.LINK) {
                if (this.numPipeEquilSpecies > 0) errcode = this.EvalPipeEquil(c);
                this.EvalPipeFormulas(c);
            }
            if (zone == EnumTypes.ObjectTypes.NODE) {
                if (this.numTankEquilSpecies > 0) errcode = this.EvalTankEquil(c);
                this.EvalTankFormulas(c);
            }
            return errcode;
        }

        ///<summary>Determines which species are described by reaction rate expressions, equilibrium expressions, or simple formulas.</summary>
        private void SetSpeciesChemistry() {
            this.numPipeRateSpecies = 0;
            this.numPipeFormulaSpecies = 0;
            this.numPipeEquilSpecies = 0;
            this.numTankRateSpecies = 0;
            this.numTankFormulaSpecies = 0;
            this.numTankEquilSpecies = 0;
            for (int i = 1; i <= this.numSpecies; i++) {
                switch (this.msx.Species[i].PipeExprType) {
                case EnumTypes.ExpressionType.RATE:
                    this.numPipeRateSpecies++;
                    this.pipeRateSpecies[this.numPipeRateSpecies] = i;
                    break;

                case EnumTypes.ExpressionType.FORMULA:
                    this.numPipeFormulaSpecies++;
                    break;

                case EnumTypes.ExpressionType.EQUIL:
                    this.numPipeEquilSpecies++;
                    this.pipeEquilSpecies[this.numPipeEquilSpecies] = i;
                    break;
                }
                switch (this.msx.Species[i].TankExprType) {
                case EnumTypes.ExpressionType.RATE:
                    this.numTankRateSpecies++;
                    this.tankRateSpecies[this.numTankRateSpecies] = i;
                    break;

                case EnumTypes.ExpressionType.FORMULA:
                    this.numTankFormulaSpecies++;
                    break;

                case EnumTypes.ExpressionType.EQUIL:
                    this.numTankEquilSpecies++;
                    this.tankEquilSpecies[this.numTankEquilSpecies] = i;
                    break;
                }
            }
        }


        ///<summary>Assigns pipe chemistry expressions to tank chemistry for each chemical species.</summary>
        private void SetTankChemistry() {
            for (int i = 1; i <= this.numSpecies; i++) {
                this.msx.Species[i].TankExpr = this.msx.Species[i].PipeExpr;
                this.msx.Species[i].TankExprType = this.msx.Species[i].PipeExprType;
            }

            this.numTankRateSpecies = this.numPipeRateSpecies;

            for (int i = 1; i <= this.numTankRateSpecies; i++) {
                this.tankRateSpecies[i] = this.pipeRateSpecies[i];
            }

            this.numTankFormulaSpecies = this.numPipeFormulaSpecies;
            this.numTankEquilSpecies = this.numPipeEquilSpecies;

            for (int i = 1; i <= this.numTankEquilSpecies; i++) {
                this.tankEquilSpecies[i] = this.pipeEquilSpecies[i];
            }
        }

        ///<summary>Retrieves current values of hydraulic variables for the current link being analyzed.</summary>
        private void EvalHydVariables(int k) {
            double dh; // headloss in ft
            double diam = this.msx.Link[k].Diam; // diameter in ft
            double av; // area per unit volume

            //  pipe diameter in user's units (ft or m)
            this.hydVar[(int)EnumTypes.HydVarType.DIAMETER] = diam * this.msx.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS];

            //  flow rate in user's units
            this.hydVar[(int)EnumTypes.HydVarType.FLOW] = Math.Abs(this.msx.Q[k])
                                                          * this.msx.Ucf[(int)EnumTypes.UnitsType.FLOW_UNITS];

            //  flow velocity in ft/sec
            if (diam == 0.0) this.hydVar[(int)EnumTypes.HydVarType.VELOCITY] = 0.0;
            else
                this.hydVar[(int)EnumTypes.HydVarType.VELOCITY] = Math.Abs(this.msx.Q[k]) * 4.0 / Constants.PI
                                                                  / (diam * diam);

            //  Reynolds number
            this.hydVar[(int)EnumTypes.HydVarType.REYNOLDS] = this.hydVar[(int)EnumTypes.HydVarType.VELOCITY] * diam
                                                              / Constants.VISCOS;

            //  flow velocity in user's units (ft/sec or m/sec)
            this.hydVar[(int)EnumTypes.HydVarType.VELOCITY] *= this.msx.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS];

            //  Darcy Weisbach friction factor
            if (this.msx.Link[k].Len == 0.0) this.hydVar[(int)EnumTypes.HydVarType.FRICTION] = 0.0;
            else {
                dh = Math.Abs(this.msx.H[this.msx.Link[k].N1] - this.msx.H[this.msx.Link[k].N2]);
                this.hydVar[(int)EnumTypes.HydVarType.FRICTION] = 39.725 * dh * Math.Pow(diam, 5)
                                                                  / this.msx.Link[k].Len
                                                                  / (this.msx.Q[k] * this.msx.Q[k]);
            }

            // Shear velocity in user's units (ft/sec or m/sec)
            this.hydVar[(int)EnumTypes.HydVarType.SHEAR] = this.hydVar[(int)EnumTypes.HydVarType.VELOCITY] *
                                                           Math.Sqrt(
                                                               this.hydVar[(int)EnumTypes.HydVarType.FRICTION] / 8.0);

            // Pipe surface area / volume in area_units/L
            this.hydVar[(int)EnumTypes.HydVarType.AREAVOL] = 1.0;
            if (diam > 0.0) {
                av = 4.0 / diam; // ft2/ft3
                av *= this.msx.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS]; // area_units/ft3
                av /= Constants.LperFT3; // area_units/L
                this.hydVar[(int)EnumTypes.HydVarType.AREAVOL] = av;
            }

            this.hydVar[(int)EnumTypes.HydVarType.ROUGHNESS] = this.msx.Link[k].Roughness;
                //Feng Shang, Bug ID 8,  01/29/2008
        }


        ///<summary>Updates species concentrations in each WQ segment of a pipe after reactions occur over time step dt.</summary>
        private EnumTypes.ErrorCodeType EvalPipeReactions(int k, long dt) {
            EnumTypes.ErrorCodeType errcode = 0;
            int ierr;
            double tstep = (double)dt / this.msx.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS];
            double c, dc;
            double[] dh = new double[1];
            // Start with the most downstream pipe segment

            this.theLink = k;
            foreach (Pipe seg  in  this.msx.Segments[this.theLink]) {
                this.theSeg = seg;
                // Store all segment species concentrations in MSX.C1

                for (int i = 1; i <= this.numSpecies; i++) this.msx.C1[i] = this.theSeg.C[i];
                ierr = 0;

                // React each reacting species over the time step

                if (dt > 0.0) {
                    // Euler integrator
                    if (this.msx.Solver == EnumTypes.SolverType.EUL) {
                        for (int i = 1; i <= this.numPipeRateSpecies; i++) {
                            int m = this.pipeRateSpecies[i];

                            //dc = mathexpr_eval(MSX.Species[m].getPipeExpr(),
                            //        getPipeVariableValue) * tstep;
                            dc = this.msx.Species[m].PipeExpr.EvaluatePipeExp(this) * tstep;
                            //dc = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface() {
                            //    public double getValue(int id) {return getPipeVariableValue(id);}
                            //    public int getIndex(string id) {return 0;}
                            //})* tstep;

                            c = this.theSeg.C[m] + dc;
                            this.theSeg.C[m] = Math.Max(c, 0.0);
                        }
                    }

                    // Other integrators
                    else {
                        // Place current concentrations of species that react in vector Yrate

                        for (int i = 1; i <= this.numPipeRateSpecies; i++) {
                            int m = this.pipeRateSpecies[i];
                            this.yrate[i] = this.theSeg.C[m];
                        }
                        dh[0] = this.theSeg.Hstep;

                        // integrate the set of rate equations

                        // Runge-Kutta integrator
                        if (this.msx.Solver == EnumTypes.SolverType.RK5)
                            ierr = this.rk5Solver.rk5_integrate(
                                           this.yrate,
                                           this.numPipeRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           this.atol,
                                           this.rtol,
                                           this,
                                           Operation.PIPES_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction(){
                        //    public void solve(double t, double[] y, int n, double[] f){getPipeDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                        //});

                        // Rosenbrock integrator
                        if (this.msx.Solver == EnumTypes.SolverType.ROS2)
                            ierr = this.ros2Solver.ros2_integrate(
                                           this.yrate,
                                           this.numPipeRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           this.atol,
                                           this.rtol,
                                           this,
                                           Operation.PIPES_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getPipeDcDt(t, y, n, f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                        //});

                        // save new concentration values of the species that reacted

                        for (int i = 1; i <= this.numSpecies; i++)
                            this.theSeg.C[i] = this.msx.C1[i];

                        for (int i = 1; i <= this.numPipeRateSpecies; i++) {
                            int m = this.pipeRateSpecies[i];
                            this.theSeg.C[m] = Math.Max(this.yrate[i], 0.0);
                        }
                        this.theSeg.Hstep = dh[0];
                    }
                    if (ierr < 0)
                        return EnumTypes.ErrorCodeType.ERR_INTEGRATOR;
                }

                // Compute new equilibrium concentrations within segment

                errcode = this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.theSeg.C);

                if (errcode != 0)
                    return errcode;

                // Move to the segment upstream of the current one

                //TheSeg = TheSeg->prev;
            }
            return errcode;
        }

        ///<summary>Updates species concentrations in a given storage tank after reactions occur over time step dt.</summary>
        private EnumTypes.ErrorCodeType EvalTankReactions(int k, long dt) {
            EnumTypes.ErrorCodeType errcode = 0;
            double tstep = ((double)dt) / this.msx.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS];
            double c, dc;
            double[] dh = new double[1];

            // evaluate each volume segment in the tank

            this.theNode = this.msx.Tank[k].Node;
            int i = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + k;
            //TheSeg = MSX.Segments[i];
            //while ( TheSeg )
            foreach (Pipe seg  in  this.msx.Segments[i]) {
                this.theSeg = seg;

                // store all segment species concentrations in MSX.C1

                for (int j = 1; j <= this.numSpecies; j++) this.msx.C1[j] = this.theSeg.C[j];
                var ierr = 0;

                // react each reacting species over the time step
                if (dt > 0.0) {
                    if (this.msx.Solver == EnumTypes.SolverType.EUL) {
                        for (i = 1; i <= this.numTankRateSpecies; i++) {
                            int j = this.tankRateSpecies[i];
                            //dc = tstep * mathexpr_eval(MSX.Species[m].getTankExpr(),
                            //        getTankVariableValue);
                            dc = tstep * this.msx.Species[j].TankExpr.EvaluateTankExp(this);
                            //dc = tstep * MSX.Species[m].getTankExpr().evaluate(
                            //        new VariableInterface(){
                            //            public double getValue(int id) {return getTankVariableValue(id);}
                            //            public int getIndex(string id) {return 0;}
                            //        });
                            c = this.theSeg.C[j] + dc;
                            this.theSeg.C[j] = Math.Max(c, 0.0);
                        }
                    }

                    else {
                        for (i = 1; i <= this.numTankRateSpecies; i++) {
                            int j = this.tankRateSpecies[i];
                            this.yrate[i] = this.msx.Tank[k].C[j];
                        }
                        dh[0] = this.msx.Tank[k].Hstep;

                        if (this.msx.Solver == EnumTypes.SolverType.RK5)
                            ierr = this.rk5Solver.rk5_integrate(
                                           this.yrate,
                                           this.numTankRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           this.atol,
                                           this.rtol,
                                           this,
                                           Operation.TANKS_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                        //} );

                        if (this.msx.Solver == EnumTypes.SolverType.ROS2)
                            ierr = this.ros2Solver.ros2_integrate(
                                           this.yrate,
                                           this.numTankRateSpecies,
                                           0,
                                           tstep,
                                           dh,
                                           this.atol,
                                           this.rtol,
                                           this,
                                           Operation.TANKS_DC_DT_CONCENTRATIONS);
                        //new JacobianFunction() {
                        //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                        //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                        //} );

                        for (int j = 1; j <= this.numSpecies; j++) this.theSeg.C[j] = this.msx.C1[j];
                        for (i = 1; i <= this.numTankRateSpecies; i++) {
                            int j = this.tankRateSpecies[i];
                            this.theSeg.C[j] = Math.Max(this.yrate[i], 0.0);
                        }
                        this.theSeg.Hstep = dh[0];
                    }
                    if (ierr < 0)
                        return EnumTypes.ErrorCodeType.ERR_INTEGRATOR;
                }

                // compute new equilibrium concentrations within segment
                errcode = this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.theSeg.C);

                if (errcode != 0)
                    return errcode;
            }
            return errcode;
        }

        ///<summary>computes equilibrium concentrations for water in a pipe segment.</summary>
        private EnumTypes.ErrorCodeType EvalPipeEquil(double[] c) {
            for (int i = 1; i <= this.numSpecies; i++) this.msx.C1[i] = c[i];

            for (int i = 1; i <= this.numPipeEquilSpecies; i++) {
                int j = this.pipeEquilSpecies[i];
                this.yequil[i] = c[j];
            }

            int errcode = this.newton.newton_solve(
                                  this.yequil,
                                  this.numPipeEquilSpecies,
                                  MAXIT,
                                  NUMSIG,
                                  this,
                                  Operation.PIPES_EQUIL);

            if (errcode < 0) return EnumTypes.ErrorCodeType.ERR_NEWTON;
            for (int i = 1; i <= this.numPipeEquilSpecies; i++) {
                int j = this.pipeEquilSpecies[i];
                c[j] = this.yequil[i];
                this.msx.C1[j] = c[j];
            }
            return 0;
        }


        ///<summary>computes equilibrium concentrations for water in a tank.</summary>
        private EnumTypes.ErrorCodeType EvalTankEquil(double[] c) {

            for (int i = 1; i <= this.numSpecies; i++) this.msx.C1[i] = c[i];
            for (int i = 1; i <= this.numTankEquilSpecies; i++) {
                int j = this.tankEquilSpecies[i];
                this.yequil[i] = c[j];
            }
            int errcode = this.newton.newton_solve(
                                  this.yequil,
                                  this.numTankEquilSpecies,
                                  MAXIT,
                                  NUMSIG,
                                  this,
                                  Operation.TANKS_EQUIL);
            //new JacobianFunction() {
            //    public void solve(double t, double[] y, int n, double[] f) {getTankEquil(t,y,n,f);}
            //    public void solve(double t, double[] y, int n, double[] f, int off) {
            //        System.out.println("Jacobian Unused");}
            //});

            if (errcode < 0) return EnumTypes.ErrorCodeType.ERR_NEWTON;

            for (int i = 1; i <= this.numTankEquilSpecies; i++) {
                int j = this.tankEquilSpecies[i];
                c[j] = this.yequil[i];
                this.msx.C1[j] = c[j];
            }
            return 0;
        }

        ///<summary>
        /// Evaluates species concentrations in a pipe segment that are simple
        /// formulas involving other known species concentrations.
        /// </summary>
        private void EvalPipeFormulas(double[] c) {
            for (int i = 1; i <= this.numSpecies; i++) this.msx.C1[i] = c[i];

            for (int i = 1; i <= this.numSpecies; i++) {
                if (this.msx.Species[i].PipeExprType == EnumTypes.ExpressionType.FORMULA) {
                    c[i] = this.msx.Species[i].PipeExpr.EvaluatePipeExp(this);
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

            for (int i = 1; i <= this.numSpecies; i++) this.msx.C1[i] = c[i];

            for (int i = 1; i <= this.numSpecies; i++) {
                if (this.msx.Species[i].TankExprType == EnumTypes.ExpressionType.FORMULA) {
                    c[i] = this.msx.Species[i].PipeExpr.EvaluateTankExp(this);
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
            if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.SPECIES]) {
                // If species represented by a formula then evaluate it
                if (this.msx.Species[i].PipeExprType == EnumTypes.ExpressionType.FORMULA) {
                    return this.msx.Species[i].PipeExpr.EvaluatePipeExp(this);
                    //return MSX.Species[i].getPipeExpr().evaluate(new VariableInterface(){
                    //    public double getValue(int id){return getPipeVariableValue(id);}
                    //    public int getIndex(string id){return 0;}
                    //});
                }
                else // otherwise return the current concentration
                    return this.msx.C1[i];
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.TERM]) // intermediate term expressions come next
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.TERM - 1];
                return this.msx.Term[i].Expr.EvaluatePipeExp(this);
                //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id){return getPipeVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER]) // reaction parameter indexes come after that
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER - 1];
                return this.msx.Link[this.theLink].Param[i];
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT]) // followed by constants
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT - 1];
                return this.msx.Const[i].Value;
            }
            else // and finally by hydraulic variables
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT];
                if (i < (int)EnumTypes.HydVarType.MAX_HYD_VARS) return this.hydVar[i];
                else return 0.0;
            }
        }

        ///<summary>Finds the value of a species, a parameter, or a constant for the current node being analyzed.</summary>
        public double GetTankVariableValue(int i) {
            // WQ species have index i between 1 & # of species and their current values are stored in vector MSX.C1
            if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.SPECIES]) {
                // If species represented by a formula then evaluate it
                if (this.msx.Species[i].TankExprType == EnumTypes.ExpressionType.FORMULA) {
                    return this.msx.Species[i].TankExpr.EvaluateTankExp(this);
                    //return MSX.Species[i].getTankExpr().evaluate(new VariableInterface() {
                    //    public double getValue(int id) {return getTankVariableValue(id);}
                    //    public int getIndex(string id) {return 0;}});
                }
                else // Otherwise return the current concentration
                    return this.msx.C1[i];
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.TERM]) // Intermediate term expressions come next
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.TERM - 1];
                return this.msx.Term[i].Expr.EvaluateTankExp(this);
                //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id) {return getTankVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER])
                // Next come reaction parameters associated with Tank nodes
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.PARAMETER - 1];
                int j = this.msx.Node[this.theNode].Tank;
                if (j > 0) {
                    return this.msx.Tank[j].Param[i];
                }
                else
                    return 0.0;
            }
            else if (i <= this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT]) // and then come constants
            {
                i -= this.lastIndex[(int)EnumTypes.ObjectTypes.CONSTANT - 1];
                return this.msx.Const[i].Value;
            }
            else
                return 0.0;
        }


        ///<summary>finds reaction rate (dC/dt) for each reacting species in a pipe.</summary>
        private void GetPipeDcDt(double t, double[] y, int n, double[] deriv) {


            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = this.pipeRateSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (this.msx.Coupling == EnumTypes.CouplingType.FULL_COUPLING) {
                if (this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i] = 0.0;
                    return;
                }
            }

            // Evaluate each pipe reaction expression
            for (int i = 1; i <= n; i++) {
                int m = this.pipeRateSpecies[i];
                //deriv[i] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
                deriv[i] = this.msx.Species[m].PipeExpr.EvaluatePipeExp(this);
                //deriv[i] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id) {return getPipeVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        private void GetPipeDcDt(double t, double[] y, int n, double[] deriv, int off) {

            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
            for (int i = 1; i <= n; i++) {
                int m = this.pipeRateSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use

            if (this.msx.Coupling == EnumTypes.CouplingType.FULL_COUPLING) {
                if (this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i + off] = 0.0;
                    return;
                }
            }

            // evaluate each pipe reaction expression
            for (int i = 1; i <= n; i++) {
                int m = this.pipeRateSpecies[i];
                //deriv[i+off] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
                deriv[i + off] = this.msx.Species[m].PipeExpr.EvaluatePipeExp(this);
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
                int m = this.tankRateSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (this.msx.Coupling == EnumTypes.CouplingType.FULL_COUPLING) {
                if (this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i] = 0.0;
                    return;
                }
            }

            // Evaluate each tank reaction expression
            for (int i = 1; i <= n; i++) {
                int m = this.tankRateSpecies[i];
                deriv[i] = this.msx.Species[m].TankExpr.EvaluateTankExp(this);
                //deriv[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id); }
                //    public int getIndex(string id) {return 0;}
                //}); //mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
            }
        }

        private void GetTankDcDt(double t, double[] y, int n, double[] deriv, int off) {


            // Assign species concentrations to their proper positions in the global concentration vector MSX.C1

            for (int i = 1; i <= n; i++) {
                int m = this.tankRateSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Update equilibrium species if full coupling in use
            if (this.msx.Coupling == EnumTypes.CouplingType.FULL_COUPLING) {
                if (this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.C1) > 0) // check for error condition
                {
                    for (int i = 1; i <= n; i++) deriv[i + off] = 0.0;
                    return;
                }
            }

            // Evaluate each tank reaction expression
            for (int i = 1; i <= n; i++) {
                int m = this.tankRateSpecies[i];
                //deriv[i+off] = mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
                deriv[i + off] = this.msx.Species[m].TankExpr.EvaluateTankExp(this);
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
                int m = this.pipeEquilSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Evaluate each pipe equilibrium expression

            for (int i = 1; i <= n; i++) {
                int m = this.pipeEquilSpecies[i];
                f[i] = this.msx.Species[m].PipeExpr.EvaluatePipeExp(this);
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
                int m = this.tankEquilSpecies[i];
                this.msx.C1[m] = y[i];
            }

            // Evaluate each tank equilibrium expression
            for (int i = 1; i <= n; i++) {
                int m = this.tankEquilSpecies[i];
                f[i] = this.msx.Species[m].TankExpr.EvaluateTankExp(this);
                //f[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id);}
                //    public int getIndex(string id) {return 0;}
                //});
            }
        }


        public override void solve(double t, double[] y, int n, double[] f, int off, Operation op) {
            switch (op) {

            case Operation.PIPES_DC_DT_CONCENTRATIONS:
                this.GetPipeDcDt(t, y, n, f, off);
                break;
            case Operation.TANKS_DC_DT_CONCENTRATIONS:
                this.GetTankDcDt(t, y, n, f, off);
                break;
            case Operation.PIPES_EQUIL:
                this.GetPipeEquil(t, y, n, f);
                break;
            case Operation.TANKS_EQUIL:
                this.GetTankDcDt(t, y, n, f);
                break;
            }
        }
    }

}