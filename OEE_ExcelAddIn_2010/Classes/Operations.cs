﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OEE_ExcelAddIn_2010
{
    public interface IOperations
    {
        bool IsBuffer();
        bool IsUnitOp();
    }
}
