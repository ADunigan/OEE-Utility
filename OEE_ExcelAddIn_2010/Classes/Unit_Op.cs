using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MNN = MathNet.Numerics;

namespace OEE_ExcelAddIn_2010
{
    public class Unit_Op : IOperations
    {
        public bool IsUnitOp()
        {
            return true;
        }

        public bool IsBuffer()
        {
            return false;
        }

        private string name = String.Empty;
        private int designspeed = 0;
        private int speedloss = 0;
        private int actualspeed = 0;
        private int setpointspeed = 0;
        private int mttr = 0;
        private int mtbf = 0;
        private double qualityloss = 0;
        private int timeto_defect = 0;
        private int defect_count = 0;
        private bool running = false;
        private bool paused = false;
        private bool faulted = false;
        private int thisup_duration = 0;
        private int nextdown_duration = 0;
        private int totaluptime = 0;
        private int totaldowntime = 0;
        private MNN.Distributions.Exponential mtbf_dist;
        private MNN.Distributions.Gamma mttr_dist;
        private MNN.Distributions.Exponential mtbql_dist;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (value != this.Name)
                {
                    this.name = value;
                }
            }
        }

        public int DesignSpeed
        {
            get
            {
                return this.designspeed;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.designspeed)
                    {
                        this.designspeed = value;
                    }
                }
            }
        }

        public int SpeedLoss
        {
            get
            {
                return this.speedloss;                
            }
            set
            {
                if (value != this.speedloss)
                {
                    this.speedloss = value;
                }               
            }
        }

        public int SetpointSpeed
        {
            get
            {
                return this.setpointspeed;
            }
            set
            {
                if(value != this.setpointspeed)
                {
                    this.setpointspeed = value;
                    if(this.Running)
                    {
                        this.ActualSpeed = this.setpointspeed;
                    }
                }
            }
        }

        public int ActualSpeed
        {
            get
            {
                return this.actualspeed;               
            }
            set
            {
                if(value != this.actualspeed)
                {
                    if(value < 0)
                    {
                        this.actualspeed = 0;
                    }
                    else
                    {
                        this.actualspeed = value;
                    }
                }
            }
        }

        public int MTTR
        {
            get
            {
                return this.mttr;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.mttr)
                    {
                        this.mttr = value;
                        this.mttr_dist = new MNN.Distributions.Gamma(2.3, 2.0 / (double)this.mttr);
                    }
                }
            }
        }

        public int MTBF
        {
            get
            {
                return this.mtbf;
            }
            set
            {
                if (value >= 0)
                {
                    if (value != this.mtbf)
                    {
                        this.mtbf = value;
                        this.mtbf_dist = new MNN.Distributions.Exponential(1.0 / (double)this.mtbf);
                    }
                }
            }
        }

        public int Time_To_Defect
        {
            get
            {
                return this.timeto_defect;
            }
            set
            {
                this.timeto_defect = value;
            }
        }

        public int ThisUpTime
        {
            get
            {
                return thisup_duration;
            }
            set
            {
                this.thisup_duration = value;                
            }
        }

        public int NextDownTime
        {
            get
            {
                return nextdown_duration;
            }
            set
            {
                this.nextdown_duration = value;
            }
        }

        public double? Availability
        {
            get
            {
                if (this.mtbf > 0 && this.mttr > 0)
                {
                    return this.mtbf / (this.mttr + this.mtbf);
                }
                else
                {
                    return null;
                }
            }
        }

        public double QualityLoss
        {
            get
            {
                return this.qualityloss;
            }
            set
            {
                if (value != this.qualityloss && value > 0)
                {
                    this.qualityloss = (double)value;
                    double mtbql = 1 / ((double)this.designspeed * (double)this.qualityloss) * 60;
                    this.mtbql_dist = new MNN.Distributions.Exponential(1.0 / mtbql);
                }
                else if(value <= 0)
                {
                    this.qualityloss = 0.0;

                }
            }
        }

        public int Defect_Count
        {
            get
            {
                return this.defect_count;
            }
            set
            {
                if(value != this.defect_count && value >= 0)
                {
                    this.defect_count = value;
                }
            }
        }

        public bool Running
        {
            get
            {
                return this.running;
            }
            set
            {
                if (value != this.running)
                {
                    this.running = value;
                    if (value)
                    {
                        this.Paused = false;
                        this.Faulted = false;
                        this.ActualSpeed = this.SetpointSpeed;
                    }
                }                
            }
        }

        public bool Paused
        {
            get
            {
                return this.paused;
            }
            set
            {
                if (value != this.paused)
                {
                    this.paused = value; 
                    if(value)
                    {
                        this.ActualSpeed = 0;
                        if (this.Faulted)
                        {
                            this.Faulted = false;
                            this.Running = false;
                            OpRepairedEventArgs args = new OpRepairedEventArgs();
                            OnOpRepaired(args);
                        }
                        else
                        {
                            this.Running = false;
                        }                        
                    }
                }
            }
        }

        public bool Faulted
        {
            get
            {
                return this.faulted;
            }
            set
            {
                if (value != this.faulted)
                {
                    this.faulted = value;
                    if(value)
                    {
                        OpFaultedEventArgs args = new OpFaultedEventArgs();
                        OnOpFaulted(args);
                        this.Running = false;
                        this.Paused = false;
                        this.ActualSpeed = 0;
                    }
                }
            }
        }

        public int TotalUpTime
        {
            get
            {
                return this.totaluptime;
            }
            set
            {
                if(value >= 0 && value != this.totaluptime)
                {
                    this.totaluptime = value;
                }
            }
        }

        public int TotalDownTime
        {
            get
            {
                return this.totaldowntime;
            }
            set
            {
                if (value >= 0 && value != this.totaldowntime)
                {
                    this.totaldowntime = value;
                }
            }
        }

        public void Sim_DefectTime()
        {
            if(this.qualityloss <= 0)
            {
                Time_To_Defect = int.MaxValue;
            }
            else
            {
                int holder = (int)mtbql_dist.Sample();
                this.Time_To_Defect = holder;
            }
        }

        public void Sim_UpTime()
        {
            if(this.mtbf <= 0)
            {
                this.ThisUpTime = int.MaxValue;
            }
            else
            {
                int holder = (int)mtbf_dist.Sample();
                if (holder <= 10)
                {
                    holder = 10;
                }
                else if(holder >= 86400)
                {
                    holder = 86400;
                }
                this.ThisUpTime = holder;
            }            
        }

        public void Sim_DownTime()
        {
            if (this.mttr <= 0)
            {
                this.NextDownTime = 0;
            }
            else
            {
                int holder = (int)mttr_dist.Sample();
                if (holder <= 30)
                {
                    holder = 30;
                }
                else if (holder >= 86400)
                {
                    holder = 86400;
                }
                this.NextDownTime = holder;
            }
        }

        protected virtual void OnOpRepaired(OpRepairedEventArgs e)
        {
            OpRepaired?.Invoke(this, e);
        }

        public event EventHandler<OpRepairedEventArgs> OpRepaired;

        protected virtual void OnOpFaulted(OpFaultedEventArgs e)
        {
            OpFaulted?.Invoke(this, e);
        }

        public event EventHandler<OpFaultedEventArgs> OpFaulted;
    }

    public class OpRepairedEventArgs : EventArgs
    {
        //Empty
    }

    public class OpFaultedEventArgs : EventArgs
    {
        //Empty
    }
}
