﻿namespace JJMasterData.Commons.Dao.Entity;

/// <summary>
/// Command executed on the database
/// </summary>
public enum CommandType
{
    None = -1,
    Insert = 0,
    Update = 1,
    Delete = 2
}