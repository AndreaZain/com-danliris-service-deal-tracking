﻿using Com.DanLiris.Service.DealTracking.Lib.Models;
using Com.DanLiris.Service.DealTracking.Lib.Utilities.BaseInterface;
using Com.DanLiris.Service.DealTracking.Lib.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.DanLiris.Service.DealTracking.Lib.BusinessLogic.Interfaces
{
    public interface IDealFacade : IBaseFacade<Deal>
    {
        Task<int> MoveActivity(MoveActivityViewModel viewModel);
    }
}
