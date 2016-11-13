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
using org.addition.epanet.msx.Solvers;
using org.addition.epanet.msx.Structures;

namespace org.addition.epanet.msx
{

    public class Chemical : JacobianInterface, ExprVariable
{

    public void loadDependencies(EpanetMSX epa)
    {
        this.MSX = epa.getNetwork();
    }

    private Network MSX;
    private rk5 rk5_solver;
    private ros2 ros2_solver;
    private Newton newton;

    //  Constants
    private const int MAXIT = 20; // Max. number of iterations used in nonlinear equation solver
    private const int NUMSIG = 3; // Number of significant digits in nonlinear equation solver error

    private Pipe TheSeg; // Current water quality segment
    private int TheLink; // Index of current link
    private int TheNode; // Index of current node
    private int NumSpecies; // Total number of species
    private int NumPipeRateSpecies; // Number of species with pipe rates
    private int NumTankRateSpecies; // Number of species with tank rates
    private int NumPipeFormulaSpecies; // Number of species with pipe formulas
    private int NumTankFormulaSpecies; // Number of species with tank formulas
    private int NumPipeEquilSpecies; // Number of species with pipe equilibria
    private int NumTankEquilSpecies; // Number of species with tank equilibria
    private int[] PipeRateSpecies; // Species governed by pipe reactions
    private int[] TankRateSpecies; // Species governed by tank reactions
    private int[] PipeEquilSpecies; // Species governed by pipe equilibria
    private int[] TankEquilSpecies; // Species governed by tank equilibria
    private int[] LastIndex; // Last index of given type of variable
    private double[] Atol; // Absolute concentration tolerances
    private double[] Rtol; // Relative concentration tolerances
    private double[] Yrate; // Rate species concentrations
    private double[] Yequil; // Equilibrium species concentrations
    private double[] HydVar; // Values of hydraulic variables


    ///<summary>opens the multi-species chemistry system.</summary>
    public EnumTypes.ErrorCodeType MSXchem_open()
    {
        int m;
        int numWallSpecies;
        int numBulkSpecies;
        int numTankExpr;
        int numPipeExpr;

        this.HydVar = new double[(int) EnumTypes.HydVarType.MAX_HYD_VARS];
        this.LastIndex = new int[(int) EnumTypes.ObjectTypes.MAX_OBJECTS];

        this.PipeRateSpecies = null;
        this.TankRateSpecies = null;
        this.PipeEquilSpecies = null;
        this.TankEquilSpecies = null;
        this.Atol = null;
        this.Rtol = null;
        this.Yrate = null;
        this.Yequil = null;
        this.NumSpecies = this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.SPECIES];
        m = this.NumSpecies + 1;
        this.PipeRateSpecies = new int[m];
        this.TankRateSpecies = new int[m];
        this.PipeEquilSpecies = new int[m];
        this.TankEquilSpecies = new int[m];
        this.Atol = new double[m];
        this.Rtol = new double[m];
        this.Yrate = new double[m];
        this.Yequil = new double[m];

        // Assign species to each type of chemical expression
        this.setSpeciesChemistry();
        numPipeExpr = this.NumPipeRateSpecies + this.NumPipeFormulaSpecies + this.NumPipeEquilSpecies;
        numTankExpr = this.NumTankRateSpecies + this.NumTankFormulaSpecies + this.NumTankEquilSpecies;

        // Use pipe chemistry for tanks if latter was not supplied
        if (numTankExpr == 0)
        {
            this.setTankChemistry();
            numTankExpr = numPipeExpr;
        }

        // Check if enough equations were specified
        numWallSpecies = 0;
        numBulkSpecies = 0;
        for (m = 1; m <= this.NumSpecies; m++)
        {
            if (this.MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL) numWallSpecies++;
            if (this.MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK) numBulkSpecies++;
        }
        if (numPipeExpr != this.NumSpecies) return EnumTypes.ErrorCodeType.ERR_NUM_PIPE_EXPR;
        if (numTankExpr != numBulkSpecies) return EnumTypes.ErrorCodeType.ERR_NUM_TANK_EXPR;

        // Open the ODE solver;
        // arguments are max. number of ODE's,
        // max. number of steps to be taken,
        // 1 if automatic step sizing used (or 0 if not used)

        if (this.MSX.Solver == EnumTypes.SolverType.RK5)
        {
            this.rk5_solver = new rk5();
            this.rk5_solver.rk5_open(this.NumSpecies, 1000, 1);
        }
        if (this.MSX.Solver == EnumTypes.SolverType.ROS2)
        {
            this.ros2_solver = new ros2();
            this.ros2_solver.ros2_open(this.NumSpecies, 1);
        }

        // Open the algebraic eqn. solver
        m = Math.Max(this.NumPipeEquilSpecies, this.NumTankEquilSpecies);
        this.newton = new Newton();
        this.newton.newton_open(m);

        // Assign entries to LastIndex array
        this.LastIndex[(int) EnumTypes.ObjectTypes.SPECIES] = this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.SPECIES];
        this.LastIndex[(int) EnumTypes.ObjectTypes.TERM] = this.LastIndex[(int) EnumTypes.ObjectTypes.SPECIES] + this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.TERM];
        this.LastIndex[(int) EnumTypes.ObjectTypes.PARAMETER] = this.LastIndex[(int) EnumTypes.ObjectTypes.TERM] + this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.PARAMETER];
        this.LastIndex[(int) EnumTypes.ObjectTypes.CONSTANT] = this.LastIndex[(int) EnumTypes.ObjectTypes.PARAMETER] + this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.CONSTANT];

        return 0;
    }

    ///<summary>computes reactions in all pipes and tanks.</summary>
    public EnumTypes.ErrorCodeType MSXchem_react(long dt)
    {
        int k, m;
        EnumTypes.ErrorCodeType errcode = 0;

        // Save tolerances of pipe rate species
        for (k = 1; k <= this.NumPipeRateSpecies; k++)
        {
            m = this.PipeRateSpecies[k];
            this.Atol[k] = this.MSX.Species[m].getaTol();
            this.Rtol[k] = this.MSX.Species[m].getrTol();
        }

        // Examine each link
        for (k = 1; k <= this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.LINK]; k++)
        {
            // Skip non-pipe links
            if (this.MSX.Link[k].getLen() == 0.0) continue;

            // Evaluate hydraulic variables
            this.evalHydVariables(k);

            // Compute pipe reactions
            errcode = this.evalPipeReactions(k, dt);
            if (errcode != 0) return errcode;
        }

        // Save tolerances of tank rate species
        for (k = 1; k <= this.NumTankRateSpecies; k++)
        {
            m = this.TankRateSpecies[k];
            this.Atol[k] = this.MSX.Species[m].getaTol();
            this.Rtol[k] = this.MSX.Species[m].getrTol();
        }

        // Examine each tank
        for (k = 1; k <= this.MSX.Nobjects[(int) EnumTypes.ObjectTypes.TANK]; k++)
        {
            // Skip reservoirs
            if (this.MSX.Tank[k].getA() == 0.0) continue;

            // Compute tank reactions
            errcode = this.evalTankReactions(k, dt);
            if (errcode != 0) return errcode;
        }
        return errcode;
    }


    // Computes equilibrium concentrations for a set of chemical species.
        public EnumTypes.ErrorCodeType MSXchem_equil(EnumTypes.ObjectTypes zone, double[] c)
    {
        EnumTypes.ErrorCodeType errcode = 0;
        if (zone == EnumTypes.ObjectTypes.LINK)
        {
            if (this.NumPipeEquilSpecies > 0) errcode = this.evalPipeEquil(c);
            this.evalPipeFormulas(c);
        }
        if (zone == EnumTypes.ObjectTypes.NODE)
        {
            if (this.NumTankEquilSpecies > 0) errcode = this.evalTankEquil(c);
            this.evalTankFormulas(c);
        }
        return errcode;
    }

    ///<summary>Determines which species are described by reaction rate expressions, equilibrium expressions, or simple formulas.</summary>

    private void setSpeciesChemistry()
    {
        this.NumPipeRateSpecies = 0;
        this.NumPipeFormulaSpecies = 0;
        this.NumPipeEquilSpecies = 0;
        this.NumTankRateSpecies = 0;
        this.NumTankFormulaSpecies = 0;
        this.NumTankEquilSpecies = 0;
        for (int m = 1; m <= this.NumSpecies; m++)
        {
            switch (this.MSX.Species[m].getPipeExprType())
            {
                case EnumTypes.ExpressionType.RATE:
                this.NumPipeRateSpecies++;
                this.PipeRateSpecies[this.NumPipeRateSpecies] = m;
                    break;

                case EnumTypes.ExpressionType.FORMULA:
                this.NumPipeFormulaSpecies++;
                    break;

                case EnumTypes.ExpressionType.EQUIL:
                this.NumPipeEquilSpecies++;
                this.PipeEquilSpecies[this.NumPipeEquilSpecies] = m;
                    break;
            }
            switch (this.MSX.Species[m].getTankExprType())
            {
                case EnumTypes.ExpressionType.RATE:
                this.NumTankRateSpecies++;
                this.TankRateSpecies[this.NumTankRateSpecies] = m;
                    break;

                case EnumTypes.ExpressionType.FORMULA:
                this.NumTankFormulaSpecies++;
                    break;

                case EnumTypes.ExpressionType.EQUIL:
                this.NumTankEquilSpecies++;
                this.TankEquilSpecies[this.NumTankEquilSpecies] = m;
                    break;
            }
        }
    }


    ///<summary>Assigns pipe chemistry expressions to tank chemistry for each chemical species.</summary>

    private void setTankChemistry()
    {
        int m;
        for (m = 1; m <= this.NumSpecies; m++)
        {
            this.MSX.Species[m].setTankExpr(this.MSX.Species[m].getPipeExpr());
            this.MSX.Species[m].setTankExprType(this.MSX.Species[m].getPipeExprType());
        }
        this.NumTankRateSpecies = this.NumPipeRateSpecies;
        for (m = 1; m <= this.NumTankRateSpecies; m++)
        {
            this.TankRateSpecies[m] = this.PipeRateSpecies[m];
        }
        this.NumTankFormulaSpecies = this.NumPipeFormulaSpecies;
        this.NumTankEquilSpecies = this.NumPipeEquilSpecies;
        for (m = 1; m <= this.NumTankEquilSpecies; m++)
        {
            this.TankEquilSpecies[m] = this.PipeEquilSpecies[m];
        }
    }

    ///<summary>Retrieves current values of hydraulic variables for the current link being analyzed.</summary>

    private void evalHydVariables(int k)
    {
        double dh; // headloss in ft
        double diam = this.MSX.Link[k].getDiam(); // diameter in ft
        double av; // area per unit volume

        //  pipe diameter in user's units (ft or m)
        this.HydVar[(int)EnumTypes.HydVarType.DIAMETER] = diam* this.MSX.Ucf[(int)EnumTypes.UnitsType.LENGTH_UNITS];

        //  flow rate in user's units
        this.HydVar[(int)EnumTypes.HydVarType.FLOW] = Math.Abs(this.MSX.Q[k])* this.MSX.Ucf[(int)EnumTypes.UnitsType.FLOW_UNITS];

        //  flow velocity in ft/sec
        if (diam == 0.0) this.HydVar[(int)EnumTypes.HydVarType.VELOCITY] = 0.0;
        else this.HydVar[(int)EnumTypes.HydVarType.VELOCITY] = Math.Abs(this.MSX.Q[k])*4.0/Constants.PI/(diam*diam);

        //  Reynolds number
        this.HydVar[(int)EnumTypes.HydVarType.REYNOLDS] = this.HydVar[(int) EnumTypes.HydVarType.VELOCITY]*diam/Constants.VISCOS;

        //  flow velocity in user's units (ft/sec or m/sec)
        this.HydVar[(int)EnumTypes.HydVarType.VELOCITY] *= this.MSX.Ucf[(int) EnumTypes.UnitsType.LENGTH_UNITS];

        //  Darcy Weisbach friction factor
        if (this.MSX.Link[k].getLen() == 0.0) this.HydVar[(int) EnumTypes.HydVarType.FRICTION] = 0.0;
        else
        {
            dh = Math.Abs(this.MSX.H[this.MSX.Link[k].getN1()] - this.MSX.H[this.MSX.Link[k].getN2()]);
            this.HydVar[(int)EnumTypes.HydVarType.FRICTION] = 39.725*dh*Math.Pow(diam, 5)/ this.MSX.Link[k].getLen()/(this.MSX.Q[k]* this.MSX.Q[k]);
        }

        // Shear velocity in user's units (ft/sec or m/sec)
        this.HydVar[(int)EnumTypes.HydVarType.SHEAR] = this.HydVar[(int)EnumTypes.HydVarType.VELOCITY]*
                                                Math.Sqrt(this.HydVar[(int)EnumTypes.HydVarType.FRICTION]/8.0);

        // Pipe surface area / volume in area_units/L
        this.HydVar[(int)EnumTypes.HydVarType.AREAVOL] = 1.0;
        if (diam > 0.0)
        {
            av = 4.0/diam; // ft2/ft3
            av *= this.MSX.Ucf[(int)EnumTypes.UnitsType.AREA_UNITS]; // area_units/ft3
            av /= Constants.LperFT3; // area_units/L
            this.HydVar[(int)EnumTypes.HydVarType.AREAVOL] = av;
        }

        this.HydVar[(int)EnumTypes.HydVarType.ROUGHNESS] = this.MSX.Link[k].getRoughness(); //Feng Shang, Bug ID 8,  01/29/2008
    }


    ///<summary>Updates species concentrations in each WQ segment of a pipe after reactions occur over time step dt.</summary>

    private EnumTypes.ErrorCodeType evalPipeReactions(int k, long dt)
    {
        int i, m;
        EnumTypes.ErrorCodeType errcode = 0;
        int ierr;
        double tstep = (double) dt/ this.MSX.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS];
        double c, dc;
        double[] dh = new double[1];
        // Start with the most downstream pipe segment

        this.TheLink = k;
        foreach (Pipe seg  in  this.MSX.Segments[this.TheLink])
        {
            this.TheSeg = seg;
            // Store all segment species concentrations in MSX.C1

            for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = this.TheSeg.getC()[m];
            ierr = 0;

            // React each reacting species over the time step

            if (dt > 0.0)
            {
                // Euler integrator

                if (this.MSX.Solver == EnumTypes.SolverType.EUL)
                {
                    for (i = 1; i <= this.NumPipeRateSpecies; i++)
                    {
                        m = this.PipeRateSpecies[i];

                        //dc = mathexpr_eval(MSX.Species[m].getPipeExpr(),
                        //        getPipeVariableValue) * tstep;
                        dc = this.MSX.Species[m].getPipeExpr().evaluatePipeExp(this)*tstep;
                        //dc = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface() {
                        //    public double getValue(int id) {return getPipeVariableValue(id);}
                        //    public int getIndex(string id) {return 0;}
                        //})* tstep;

                        c = this.TheSeg.getC()[m] + dc;
                        this.TheSeg.getC()[m] = Math.Max(c, 0.0);
                    }
                }

                    // Other integrators
                else
                {
                    // Place current concentrations of species that react in vector Yrate

                    for (i = 1; i <= this.NumPipeRateSpecies; i++)
                    {
                        m = this.PipeRateSpecies[i];
                        this.Yrate[i] = this.TheSeg.getC()[m];
                    }
                    dh[0] = this.TheSeg.getHstep();

                    // integrate the set of rate equations

                    // Runge-Kutta integrator
                    if (this.MSX.Solver == EnumTypes.SolverType.RK5)
                        ierr = this.rk5_solver.rk5_integrate(this.Yrate, this.NumPipeRateSpecies, 0, tstep,
                            dh, this.Atol, this.Rtol, this, Operation.PIPES_DC_DT_CONCENTRATIONS);
                    //new JacobianFunction(){
                    //    public void solve(double t, double[] y, int n, double[] f){getPipeDcDt(t,y,n,f);}
                    //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                    //});

                    // Rosenbrock integrator
                    if (this.MSX.Solver == EnumTypes.SolverType.ROS2)
                        ierr = this.ros2_solver.ros2_integrate(this.Yrate, this.NumPipeRateSpecies, 0, tstep,
                            dh, this.Atol, this.Rtol, this, Operation.PIPES_DC_DT_CONCENTRATIONS);
                    //new JacobianFunction() {
                    //    public void solve(double t, double[] y, int n, double[] f) {getPipeDcDt(t, y, n, f);}
                    //    public void solve(double t, double[] y, int n, double[] f, int off) {getPipeDcDt(t,y,n,f,off);}
                    //});

                    // save new concentration values of the species that reacted

                    for (m = 1; m <= this.NumSpecies; m++) this.TheSeg.getC()[m] = this.MSX.C1[m];
                    for (i = 1; i <= this.NumPipeRateSpecies; i++)
                    {
                        m = this.PipeRateSpecies[i];
                        this.TheSeg.getC()[m] = Math.Max(this.Yrate[i], 0.0);
                    }
                    this.TheSeg.setHstep(dh[0]);
                }
                if (ierr < 0)
                    return EnumTypes.ErrorCodeType.ERR_INTEGRATOR;
            }

            // Compute new equilibrium concentrations within segment

            errcode = this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.TheSeg.getC());

            if (errcode != 0)
                return errcode;

            // Move to the segment upstream of the current one

            //TheSeg = TheSeg->prev;
        }
        return errcode;
    }

    ///<summary>Updates species concentrations in a given storage tank after reactions occur over time step dt.</summary>

    private EnumTypes.ErrorCodeType evalTankReactions(int k, long dt)
    {
        int i, m;
        EnumTypes.ErrorCodeType errcode = 0;
        int ierr;
        double tstep = ((double) dt)/ this.MSX.Ucf[(int)EnumTypes.UnitsType.RATE_UNITS];
        double c, dc;
        double[] dh = new double[1];

        // evaluate each volume segment in the tank

        this.TheNode = this.MSX.Tank[k].getNode();
        i = this.MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + k;
        //TheSeg = MSX.Segments[i];
        //while ( TheSeg )
        foreach (Pipe seg  in  this.MSX.Segments[i])
        {
            this.TheSeg = seg;

            // store all segment species concentrations in MSX.C1
            for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = this.TheSeg.getC()[m];
            ierr = 0;

            // react each reacting species over the time step
            if (dt > 0.0)
            {
                if (this.MSX.Solver == EnumTypes.SolverType.EUL)
                {
                    for (i = 1; i <= this.NumTankRateSpecies; i++)
                    {
                        m = this.TankRateSpecies[i];
                        //dc = tstep * mathexpr_eval(MSX.Species[m].getTankExpr(),
                        //        getTankVariableValue);
                        dc = tstep* this.MSX.Species[m].getTankExpr().evaluateTankExp(this);
                        //dc = tstep * MSX.Species[m].getTankExpr().evaluate(
                        //        new VariableInterface(){
                        //            public double getValue(int id) {return getTankVariableValue(id);}
                        //            public int getIndex(string id) {return 0;}
                        //        });
                        c = this.TheSeg.getC()[m] + dc;
                        this.TheSeg.getC()[m] = Math.Max(c, 0.0);
                    }
                }

                else
                {
                    for (i = 1; i <= this.NumTankRateSpecies; i++)
                    {
                        m = this.TankRateSpecies[i];
                        this.Yrate[i] = this.MSX.Tank[k].getC()[m];
                    }
                    dh[0] = this.MSX.Tank[k].getHstep();

                    if (this.MSX.Solver == EnumTypes.SolverType.RK5)
                        ierr = this.rk5_solver.rk5_integrate(this.Yrate, this.NumTankRateSpecies, 0, tstep,
                            dh, this.Atol, this.Rtol, this, Operation.TANKS_DC_DT_CONCENTRATIONS);
                    //new JacobianFunction() {
                    //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                    //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                    //} );

                    if (this.MSX.Solver == EnumTypes.SolverType.ROS2)
                        ierr = this.ros2_solver.ros2_integrate(this.Yrate, this.NumTankRateSpecies, 0, tstep,
                            dh, this.Atol, this.Rtol, this, Operation.TANKS_DC_DT_CONCENTRATIONS);
                    //new JacobianFunction() {
                    //    public void solve(double t, double[] y, int n, double[] f) {getTankDcDt(t,y,n,f);}
                    //    public void solve(double t, double[] y, int n, double[] f, int off) {getTankDcDt(t,y,n,f,off);}
                    //} );

                    for (m = 1; m <= this.NumSpecies; m++) this.TheSeg.getC()[m] = this.MSX.C1[m];
                    for (i = 1; i <= this.NumTankRateSpecies; i++)
                    {
                        m = this.TankRateSpecies[i];
                        this.TheSeg.getC()[m] = Math.Max(this.Yrate[i], 0.0);
                    }
                    this.TheSeg.setHstep(dh[0]);
                }
                if (ierr < 0)
                    return EnumTypes.ErrorCodeType.ERR_INTEGRATOR;
            }

            // compute new equilibrium concentrations within segment
            errcode = this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.TheSeg.getC());

            if (errcode != 0)
                return errcode;
        }
        return errcode;
    }

    ///<summary>computes equilibrium concentrations for water in a pipe segment.</summary>

    private EnumTypes.ErrorCodeType evalPipeEquil(double[] c)
    {
        int i, m;
        int errcode;
        for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = c[m];
        for (i = 1; i <= this.NumPipeEquilSpecies; i++)
        {
            m = this.PipeEquilSpecies[i];
            this.Yequil[i] = c[m];
        }
        errcode = this.newton.newton_solve(this.Yequil, this.NumPipeEquilSpecies, MAXIT, NUMSIG,
            this, Operation.PIPES_EQUIL);
        //new JacobianFunction() {
        //    public void solve(double t, double[] y, int n, double[] f) {
        //        getPipeEquil(t,y,n,f);
        //    }
        //
        //    public void solve(double t, double[] y, int n, double[] f, int off) {
        //        System.out.println("Jacobian Unused");
        //    }
        //});
        if (errcode < 0) return EnumTypes.ErrorCodeType.ERR_NEWTON;
        for (i = 1; i <= this.NumPipeEquilSpecies; i++)
        {
            m = this.PipeEquilSpecies[i];
            c[m] = this.Yequil[i];
            this.MSX.C1[m] = c[m];
        }
        return 0;
    }


    ///<summary>computes equilibrium concentrations for water in a tank.</summary>

    private EnumTypes.ErrorCodeType evalTankEquil(double[] c)
    {
        int i, m;
        int errcode;
        for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = c[m];
        for (i = 1; i <= this.NumTankEquilSpecies; i++)
        {
            m = this.TankEquilSpecies[i];
            this.Yequil[i] = c[m];
        }
        errcode = this.newton.newton_solve(this.Yequil, this.NumTankEquilSpecies, MAXIT, NUMSIG,
            this, Operation.TANKS_EQUIL);
        //new JacobianFunction() {
        //    public void solve(double t, double[] y, int n, double[] f) {getTankEquil(t,y,n,f);}
        //    public void solve(double t, double[] y, int n, double[] f, int off) {
        //        System.out.println("Jacobian Unused");}
        //});
        if (errcode < 0) return EnumTypes.ErrorCodeType.ERR_NEWTON;
        for (i = 1; i <= this.NumTankEquilSpecies; i++)
        {
            m = this.TankEquilSpecies[i];
            c[m] = this.Yequil[i];
            this.MSX.C1[m] = c[m];
        }
        return 0;
    }

    /**
     * Evaluates species concentrations in a pipe segment that are simple
     * formulas involving other known species concentrations.
      */

    private void evalPipeFormulas(double[] c)
    {
        int m;
        for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = c[m];
        for (m = 1; m <= this.NumSpecies; m++)
        {
            if (this.MSX.Species[m].getPipeExprType() == EnumTypes.ExpressionType.FORMULA)
            {
                c[m] = this.MSX.Species[m].getPipeExpr().evaluatePipeExp(this);
                //c[m] = MSX.Species[m].getPipeExpr().evaluate( new VariableInterface(){
                //    public double getValue(int id){return getPipeVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }
        }
    }

    /**
     * Evaluates species concentrations in a tank that are simple
     * formulas involving other known species concentrations.
      */

    private void evalTankFormulas(double[] c)
    {
        int m;
        for (m = 1; m <= this.NumSpecies; m++) this.MSX.C1[m] = c[m];
        for (m = 1; m <= this.NumSpecies; m++)
        {
            if (this.MSX.Species[m].getTankExprType() == EnumTypes.ExpressionType.FORMULA)
            {
                c[m] = this.MSX.Species[m].getPipeExpr().evaluateTankExp(this);
                //c[m] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id){return getTankVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }
        }
    }

    ///<summary>Finds the value of a species, a parameter, or a constant for the pipe link being analyzed.</summary>

    public double getPipeVariableValue(int i)
    {
        // WQ species have index i between 1 & # of species
        // and their current values are stored in vector MSX.C1
        if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.SPECIES])
        {
            // If species represented by a formula then evaluate it
            if (this.MSX.Species[i].getPipeExprType() == EnumTypes.ExpressionType.FORMULA)
            {
                return this.MSX.Species[i].getPipeExpr().evaluatePipeExp(this);
                //return MSX.Species[i].getPipeExpr().evaluate(new VariableInterface(){
                //    public double getValue(int id){return getPipeVariableValue(id);}
                //    public int getIndex(string id){return 0;}
                //});
            }
            else // otherwise return the current concentration
                return this.MSX.C1[i];
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.TERM]) // intermediate term expressions come next
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.TERM - 1];
            return this.MSX.Term[i].getExpr().evaluatePipeExp(this);
            //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
            //    public double getValue(int id){return getPipeVariableValue(id);}
            //    public int getIndex(string id){return 0;}
            //});
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.PARAMETER]) // reaction parameter indexes come after that
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.PARAMETER - 1];
            return this.MSX.Link[this.TheLink].getParam()[i];
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.CONSTANT]) // followed by constants
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.CONSTANT - 1];
            return this.MSX.Const[i].getValue();
        }
        else // and finally by hydraulic variables
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.CONSTANT];
            if (i < (int)EnumTypes.HydVarType.MAX_HYD_VARS) return this.HydVar[i];
            else return 0.0;
        }
    }

    ///<summary>Finds the value of a species, a parameter, or a constant for the current node being analyzed.</summary>

    public double getTankVariableValue(int i)
    {
        int j;
        // WQ species have index i between 1 & # of species and their current values are stored in vector MSX.C1
        if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.SPECIES])
        {
            // If species represented by a formula then evaluate it
            if (this.MSX.Species[i].getTankExprType() == EnumTypes.ExpressionType.FORMULA)
            {
                return this.MSX.Species[i].getTankExpr().evaluateTankExp(this);
                //return MSX.Species[i].getTankExpr().evaluate(new VariableInterface() {
                //    public double getValue(int id) {return getTankVariableValue(id);}
                //    public int getIndex(string id) {return 0;}});
            }
            else // Otherwise return the current concentration
                return this.MSX.C1[i];
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.TERM]) // Intermediate term expressions come next
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.TERM - 1];
            return this.MSX.Term[i].getExpr().evaluateTankExp(this);
            //return MSX.Term[i].getExpr().evaluate(new VariableInterface(){
            //    public double getValue(int id) {return getTankVariableValue(id);}
            //    public int getIndex(string id) {return 0;}
            //});
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.PARAMETER])
            // Next come reaction parameters associated with Tank nodes
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.PARAMETER - 1];
            j = this.MSX.Node[this.TheNode].getTank();
            if (j > 0)
            {
                return this.MSX.Tank[j].getParam()[i];
            }
            else
                return 0.0;
        }
        else if (i <= this.LastIndex[(int)EnumTypes.ObjectTypes.CONSTANT]) // and then come constants
        {
            i -= this.LastIndex[(int)EnumTypes.ObjectTypes.CONSTANT - 1];
            return this.MSX.Const[i].getValue();
        }
        else
            return 0.0;
    }


    ///<summary>finds reaction rate (dC/dt) for each reacting species in a pipe.</summary>

    private void getPipeDcDt(double t, double[] y, int n, double[] deriv)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
    for (i = 1; i <= n; i++)
    {
        m = this.PipeRateSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Update equilibrium species if full coupling in use
    if (this.MSX.Coupling == EnumTypes.CouplingType.FULL_COUPLING)
    {
        if (this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.MSX.C1) > 0) // check for error condition
        {
            for (i = 1; i <= n; i++) deriv[i] = 0.0;
            return;
        }
    }

    // Evaluate each pipe reaction expression
    for (i = 1; i <= n; i++)
    {
        m = this.PipeRateSpecies[i];
        //deriv[i] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
        deriv[i] = this.MSX.Species[m].getPipeExpr().evaluatePipeExp(this);
        //deriv[i] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
        //    public double getValue(int id) {return getPipeVariableValue(id);}
        //    public int getIndex(string id) {return 0;}
        //});
    }
}


    private void getPipeDcDt(double t, double[] y, int n, double[] deriv, int off)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
    for (i = 1; i <= n; i++)
    {
        m = this.PipeRateSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Update equilibrium species if full coupling in use

    if (this.MSX.Coupling == EnumTypes.CouplingType.FULL_COUPLING)
    {
        if (this.MSXchem_equil(EnumTypes.ObjectTypes.LINK, this.MSX.C1) > 0) // check for error condition
        {
            for (i = 1; i <= n; i++) deriv[i + off] = 0.0;
            return;
        }
    }

    // evaluate each pipe reaction expression
    for (i = 1; i <= n; i++)
    {
        m = this.PipeRateSpecies[i];
        //deriv[i+off] = mathexpr_eval(MSX.Species[m].getPipeExpr(), getPipeVariableValue);
        deriv[i + off] = this.MSX.Species[m].getPipeExpr().evaluatePipeExp(this);
        //deriv[i+off] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface(){
        //    public double getValue(int id) {return getPipeVariableValue(id);}
        //    public int getIndex(string id) {return 0;}
        //});
    }
}


    ///<summary>finds reaction rate (dC/dt) for each reacting species in a tank.</summary>
    private void getTankDcDt(double t, double[] y, int n, double[] deriv)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
    for (i = 1; i <= n; i++)
    {
        m = this.TankRateSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Update equilibrium species if full coupling in use
    if (this.MSX.Coupling == EnumTypes.CouplingType.FULL_COUPLING)
    {
        if (this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.MSX.C1) > 0) // check for error condition
        {
            for (i = 1; i <= n; i++) deriv[i] = 0.0;
            return;
        }
    }

    // Evaluate each tank reaction expression
    for (i = 1; i <= n; i++)
    {
        m = this.TankRateSpecies[i];
        deriv[i] = this.MSX.Species[m].getTankExpr().evaluateTankExp(this);
        //deriv[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
        //    public double getValue(int id) {return getTankVariableValue(id); }
        //    public int getIndex(string id) {return 0;}
        //}); //mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
    }
}

    private void getTankDcDt(double t, double[] y, int n, double[] deriv, int off)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global concentration vector MSX.C1

    for (i = 1; i <= n; i++)
    {
        m = this.TankRateSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Update equilibrium species if full coupling in use
    if (this.MSX.Coupling == EnumTypes.CouplingType.FULL_COUPLING)
    {
        if (this.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.MSX.C1) > 0) // check for error condition
        {
            for (i = 1; i <= n; i++) deriv[i + off] = 0.0;
            return;
        }
    }

    // Evaluate each tank reaction expression
    for (i = 1; i <= n; i++)
    {
        m = this.TankRateSpecies[i];
        //deriv[i+off] = mathexpr_eval(MSX.Species[m].getTankExpr(), getTankVariableValue);
        deriv[i + off] = this.MSX.Species[m].getTankExpr().evaluateTankExp(this);
        //deriv[i+off] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
        //    public double getValue(int id) {return getTankVariableValue(id); }
        //    public int getIndex(string id) {return 0;}
        //});
    }
}


    ///<summary>Evaluates equilibrium expressions for pipe chemistry.</summary>
    private void getPipeEquil(double t, double[] y, int n, double[] f)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global
    // concentration vector MSX.C1

    for (i = 1; i <= n; i++)
    {
        m = this.PipeEquilSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Evaluate each pipe equilibrium expression

    for (i = 1; i <= n; i++)
    {
        m = this.PipeEquilSpecies[i];
        f[i] = this.MSX.Species[m].getPipeExpr().evaluatePipeExp(this);
        //f[i] = MSX.Species[m].getPipeExpr().evaluate(new VariableInterface() {
        //    public double getValue(int id){return getPipeVariableValue(id);}
        //    public int getIndex(string id){return 0;}
        //});
    }
}


    ///<summary>Evaluates equilibrium expressions for tank chemistry.</summary>
    private void getTankEquil(double t, double[] y, int n, double[] f)
{
    int i, m;

    // Assign species concentrations to their proper positions in the global concentration vector MSX.C1
    for (i = 1; i <= n; i++)
    {
        m = this.TankEquilSpecies[i];
        this.MSX.C1[m] = y[i];
    }

    // Evaluate each tank equilibrium expression
    for (i = 1; i <= n; i++)
    {
        m = this.TankEquilSpecies[i];
        f[i] = this.MSX.Species[m].getTankExpr().evaluateTankExp(this);
        //f[i] = MSX.Species[m].getTankExpr().evaluate(new VariableInterface() {
        //    public double getValue(int id) {return getTankVariableValue(id);}
        //    public int getIndex(string id) {return 0;}
        //});
    }
}


    public override void solve(double t, double[] y, int n, double[] f, int off, Operation op)
    {
        switch (op)
        {

            case Operation.PIPES_DC_DT_CONCENTRATIONS:
            this.getPipeDcDt(t, y, n, f, off);
                break;
            case Operation.TANKS_DC_DT_CONCENTRATIONS:
            this.getTankDcDt(t, y, n, f, off);
                break;
            case Operation.PIPES_EQUIL:
            this.getPipeEquil(t, y, n, f);
                break;
            case Operation.TANKS_EQUIL:
            this.getTankDcDt(t, y, n, f);
                break;
        }
    }
}
}