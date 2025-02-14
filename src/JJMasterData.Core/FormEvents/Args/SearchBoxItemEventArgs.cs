﻿using System;

namespace JJMasterData.Core.FormEvents.Args;

public class SearchBoxItemEventArgs : EventArgs
{
    public string IdSearch { get; set; }

    public string ResultText { get; set; }

    public SearchBoxItemEventArgs(string idSearch)
    {
        IdSearch = idSearch;
    }
}
