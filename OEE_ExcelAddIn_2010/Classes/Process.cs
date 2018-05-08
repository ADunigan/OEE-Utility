using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OEE_ExcelAddIn_2010
{
    public class Process
    {
        public ObservableCollection<IOperations> Activities = new ObservableCollection<IOperations>();
        private List<Unit_Op> OPs = new List<Unit_Op>();
        private List<Buffer> Buffers = new List<Buffer>();
        private int line_speed = int.MaxValue;

        public Process()
        {
            Activities.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ActivitiesChanged);
        }

        public void Step(int step)
        {

            //Check to see if an activity is faulted, only one may fault per time step
            foreach (Unit_Op op in OPs)
            {
                if (op.ThisUpTime <= 0)
                {
                    op.Faulted = true;
                    break;
                }
            }

            //Check to see if an activity is repaired
            foreach (Unit_Op op in OPs)
            {
                if (op.NextDownTime <= 0)
                {
                    op.Paused = true;
                    op.Sim_UpTime();
                    op.Sim_DownTime();
                }

                if(op.Time_To_Defect <= 0)
                {                    
                    op.Defect_Count++;
                    op.Sim_DefectTime();
                }
            }

            //Evaluate new buffer counts
            foreach (Buffer buff in Buffers)
            {
                int i = Activities.IndexOf(buff);
                double us = ((Unit_Op)Activities[i - 1]).ActualSpeed;
                double ds = ((Unit_Op)Activities[i + 1]).ActualSpeed;
                buff.Buffer_Count = buff.Buffer_Count + (us - ds) / (double)60.0;
            }

            //Set activity timers for this time step
            foreach (Unit_Op op in OPs)
            {
                if (op.Running)
                {
                    op.TotalUpTime++;
                    op.ThisUpTime--;
                    op.Time_To_Defect--;
                }
                else if (op.Paused)
                {
                    op.TotalUpTime++;
                }
                else if (op.Faulted)
                {
                    op.TotalDownTime++;
                    op.NextDownTime--;
                }
            }
        }

        //Subscribe each Unit Op in the Process to OpFaulted
        private void ActivitiesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if(Activities[Activities.Count - 1].IsUnitOp())
                {
                    Unit_Op op = (Unit_Op)Activities[Activities.Count - 1];
                    OPs.Add(op);
                    op.OpRepaired += OpRepaired;
                    op.OpFaulted += OpFaulted;

                    if (op.DesignSpeed < line_speed)
                    {
                        line_speed = op.DesignSpeed;
                    }
                }
                else if(Activities[Activities.Count - 1].IsBuffer())
                {
                    Buffer buffer = (Buffer)Activities[Activities.Count - 1];
                    Buffers.Add(buffer);
                    buffer.BufferHasProduct += BufferHasProduct;
                    buffer.BufferEmpty += BufferEmpty;
                    buffer.BufferFull += BufferFull;

                    if (buffer.DesignSpeed < line_speed)
                    {
                        line_speed = buffer.DesignSpeed;
                    }
                }                               
            }
        }

        private void OpRepaired(object sender, OpRepairedEventArgs e)
        {
            int i = Activities.IndexOf((Unit_Op)sender);
            //If no faults in OPs, then restart all machines
            bool no_faults = true;
            foreach (Unit_Op op in OPs)
            {
                if (op.Faulted)
                {
                    no_faults = false;
                }
            }
            if(no_faults)
            {
                foreach(Unit_Op op in OPs)
                {
                    if(!op.Running)
                    {
                        op.Running = true;
                    }                    
                }
                return;
            }

            //Get buffer indexes and group unit ops, set unit ops in groups to running if buffers are in good states
            List<int> buffer_indexes = new List<int>();
            foreach(IOperations iop in Activities)
            {
                if(iop.IsBuffer())
                {
                    Buffer buff = (Buffer)iop;
                    buffer_indexes.Add(Activities.IndexOf(iop));
                }
            }

            List<List<Unit_Op>> op_groups = new List<List<Unit_Op>>();
            bool[] group_ready = new bool[buffer_indexes.Count + 1];
            int last_buff = 0;
            for(int j = 0; j < buffer_indexes.Count; j++)
            {
                op_groups.Add(new List<Unit_Op>());
                group_ready[j] = true;
                for(int m = last_buff; m < buffer_indexes[j]; m++)
                {
                    Unit_Op op = (Unit_Op)Activities[m];
                    op_groups[j].Add(op);    
                    if(op.Faulted)
                    {
                        group_ready[j] = false;
                    }
                    if(j + 1 == buffer_indexes.Count)
                    {
                        op_groups.Add(new List<Unit_Op>());
                        group_ready[j + 1] = true;
                        for (int n = buffer_indexes[j] + 1; n < Activities.Count; n++)
                        {                            
                            Unit_Op _op = (Unit_Op)Activities[n];
                            op_groups[j + 1].Add(_op);
                            if (_op.Faulted)
                            {
                                group_ready[j + 1] = false;
                            }
                        }
                        break;
                    }
                }
                last_buff = buffer_indexes[j] + 1;
            }

            int? prod_to_run_begins_at = null;
            int? downstream_space_ends_at = null;
            int k = 0;
            for(int j = 0; j < group_ready.Length; j++)
            {
                //If this group is ready, check for start conditions
                if(group_ready[j])
                {
                    //If first group on line, then set product availability to 0 (line entrance, no upstream buffer)
                    if(j == 0)
                    {
                        prod_to_run_begins_at = 0;
                    }

                    //Until a group is not ready, continue looping and evaluating where product may fill and is available
                    k = j;
                    while(group_ready[k])
                    {
                        if(!prod_to_run_begins_at.HasValue)
                        {
                            Buffer buff = (Buffer)Activities[buffer_indexes[k - 1]];
                            if(!buff.Buffer_Empty)
                            {
                                prod_to_run_begins_at = k;
                            }
                        }

                        if(k + 1 == group_ready.Length)
                        {
                            downstream_space_ends_at = k;
                        }
                        else if(!((Buffer)Activities[buffer_indexes[k]]).Buffer_Full)
                        {
                            downstream_space_ends_at = k;
                        }
                        k++;

                        if(k == group_ready.Length)
                        {
                            break;
                        }
                    }
                }
                if (prod_to_run_begins_at.HasValue && downstream_space_ends_at.HasValue)
                {
                    for (int l = (int)prod_to_run_begins_at; l <= downstream_space_ends_at; l++)
                    {
                        foreach (Unit_Op op in op_groups[l])
                        {
                            op.Running = true;
                        }
                    }
                }

                prod_to_run_begins_at = null;
                downstream_space_ends_at = null;
            }
        }

        private void OpFaulted(object sender, OpFaultedEventArgs e)
        {
            int i = Activities.IndexOf((Unit_Op)sender);
            //Pause all downstream ops until buffer
            for(int j = i + 1; j < Activities.Count; j++)
            {
                if (Activities[j].IsUnitOp())
                {
                    ((Unit_Op)Activities[j]).Paused = true;
                }
                else if (Activities[j].IsBuffer())
                {
                    //If is buffer and buffer is empty then continue pausing ops
                    if (((Buffer)Activities[j]).Buffer_Empty)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                
            }
            //Pause all upstream ops until buffer
            for (int j = i - 1; j >= 0; j--)
            {
                if (Activities[j].IsUnitOp())
                {
                    ((Unit_Op)Activities[j]).Paused = true;
                }
                else if (Activities[j].IsBuffer())
                {
                    //If is buffer and buffer is full then continue pausing ops
                    if (((Buffer)Activities[j]).Buffer_Full)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void BufferHasProduct(object sender, BufferHasProductEventArgs e)
        {
            int i = Activities.IndexOf((Buffer)sender);
            int spd = ((Buffer)sender).DesignSpeed;
            //Set current speed of downstream ops to design speed of buffer until buffer
            for(int j = i + 1; j < Activities.Count; j++)
            {
                if(Activities[j].IsUnitOp())
                {
                    ((Unit_Op)Activities[j]).SetpointSpeed = spd;                                       
                }
                else if(Activities[j].IsBuffer())
                {
                    ((Buffer)Activities[j]).SetpointSpeed = spd;                    
                    break;                    
                }
            }
        }

        private void BufferEmpty(object sender, BufferEmptyEventArgs e)
        {
            int i = Activities.IndexOf((Buffer)sender);
            bool upstream_running = false;

            //Set downstream speeds to line speed until buffer, continue if buffer empty        
            for(int j = i + 1; j < Activities.Count; j++)
            {
                if (Activities[j].IsUnitOp())
                { 
                    ((Unit_Op)Activities[j]).SetpointSpeed = line_speed;
                }
                else if(Activities[j].IsBuffer())
                {
                    Buffer buff = (Buffer)Activities[j];
                    buff.SetpointSpeed = line_speed;
                    if(buff.Buffer_Empty)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if(Activities[i - 1].IsUnitOp())
            {
                upstream_running = ((Unit_Op)Activities[i - 1]).Running;
            }
            //Pause all downstream ops if upstream not running until buffer
            if (!upstream_running)
            {
                for (int j = i + 1; j < Activities.Count; j++)
                {
                    if (Activities[j].IsUnitOp())
                    {
                        ((Unit_Op)Activities[j]).Paused = true;
                    }
                    else if (Activities[j].IsBuffer())
                    {
                        Buffer buff = (Buffer)Activities[j];
                        if (buff.Buffer_Empty)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void BufferFull(object sender, BufferFullEventArgs e)
        {
            int i = Activities.IndexOf((Buffer)sender);
            bool downstream_running = false;
            if(Activities[i + 1].IsUnitOp())
            {
                downstream_running = ((Unit_Op)Activities[i + 1]).Running;
            }
            //Pause all upstream ops if downstream not running until buffer
            if (!downstream_running)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if(Activities[j].IsUnitOp())
                    {
                        ((Unit_Op)Activities[j]).Paused = true;
                    }
                    else if(Activities[j].IsBuffer())
                    {
                        Buffer buff = (Buffer)Activities[j];
                        if (buff.Buffer_Full)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
