﻿namespace StreamMaster.Domain.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SMAPIAttribute(bool JustHub = false, bool JustController = false, bool Persist = false, bool IsTask = false, bool NoDebug = false) : Attribute
{
    public bool IsTask { get; set; } = IsTask;
    public bool JustHub { get; set; } = JustHub;
    public bool JustController { get; set; } = JustController;
    public bool Persist { get; set; } = Persist;
    public bool NoDebug { get; set; } = NoDebug;
}
