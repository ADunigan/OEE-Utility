using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OEE_ExcelAddIn_2010
{
    public class Buffer : IOperations
    {
        public bool IsUnitOp()
        {
            return false;
        }

        public bool IsBuffer()
        {
            return true;
        }

        private string name = String.Empty;
        private int designspeed = 0;
        private int actualspeed = 0;
        private int setpointspeed = 0;
        private bool buffer_full = false;
        private bool buffer_empty = true;
        private int buffer_capacity = 0;
        private double buffer_count = 0;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if(value != this.name)
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
                if(value != this.designspeed)
                {
                    this.designspeed = value;
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

        public bool Buffer_Full
        {
            get
            {
                return this.buffer_full;
            }
        }

        public bool Buffer_Empty
        {
            get
            {
                return this.buffer_empty;
            }
        }

        public int Buffer_Capacity
        {
            get
            {
                return this.buffer_capacity;
            }
            set
            {
                if(value != this.buffer_capacity && value >= 0)
                {
                    this.buffer_capacity = value;
                    if(buffer_capacity > buffer_count)
                    {
                        this.buffer_full = false;
                    }
                    else
                    {
                        BufferFullEventArgs args = new BufferFullEventArgs();
                        OnBufferFull(args);
                        this.buffer_full = true;
                        this.buffer_empty = false;
                    }
                }
            }
        }

        public double Buffer_Count
        {
            get
            {
                return this.buffer_count;
            }
            set
            {
                if(value != this.buffer_count && value >= 0 && value >= this.buffer_capacity)
                {
                    BufferFullEventArgs args = new BufferFullEventArgs();
                    OnBufferFull(args);                                      
                    this.buffer_full = true;
                    this.buffer_empty = false;
                    this.buffer_count = this.buffer_capacity;
                    
                }
                else if(value != this.buffer_count && value <= 0)
                {
                    BufferEmptyEventArgs args = new BufferEmptyEventArgs();
                    OnBufferEmpty(args);
                    this.buffer_count = 0;
                    this.buffer_empty = true;
                    this.buffer_full = false;
                }
                else if(value != this.buffer_count && value > 0 && value < this.buffer_capacity)
                {
                    BufferHasProductEventArgs args = new BufferHasProductEventArgs();
                    OnBufferHasProduct(args);
                    this.buffer_count = value;
                    this.buffer_empty = false;
                    this.buffer_full = false;
                }
            }
        }

        //Invoke Buffer Has Product event that is handled within Process class
        protected virtual void OnBufferHasProduct(BufferHasProductEventArgs e)
        {
            BufferHasProduct?.Invoke(this, e);
        }
        public event EventHandler<BufferHasProductEventArgs> BufferHasProduct;

        //Invoke Buffer Empty event that is handled within Process class
        protected virtual void OnBufferEmpty(BufferEmptyEventArgs e)
        {
            BufferEmpty?.Invoke(this, e);
        }
        public event EventHandler<BufferEmptyEventArgs> BufferEmpty;

        //Invoke Buffer Full event that is handled within Process class
        protected virtual void OnBufferFull(BufferFullEventArgs e)
        {
            BufferFull?.Invoke(this, e);
        }
        public event EventHandler<BufferFullEventArgs> BufferFull;
    }

    public class BufferHasProductEventArgs : EventArgs
    {
        //Empty
    }

    public class BufferEmptyEventArgs : EventArgs
    {
        //Empty
    }

    public class BufferFullEventArgs : EventArgs
    {
        //Empty
    }
}
