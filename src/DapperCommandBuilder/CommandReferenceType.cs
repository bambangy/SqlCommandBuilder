﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperCommandBuilder
{
    public enum CommandReferenceType
    {
        Join,
        InnerJoin,
        LeftJoin,
        RightJoin,
        OuterJoin
    }
}