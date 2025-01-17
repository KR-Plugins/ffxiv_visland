﻿using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets2;
using System;
using visland.Helpers;
using AtkValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace visland;

public static unsafe class WorkshopUtils
{
    public static (long index, DateTime startTime) CurrentWeek()
    {
        var cycleData = Service.LuminaRow<CycleTime>(2)!;
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var index = (now - cycleData.FirstCycle) / cycleData.Cycle;
        var startTime = cycleData.FirstCycle + cycleData.Cycle * index;
        return (index, DateTime.UnixEpoch.AddSeconds(startTime));
    }

    public static bool CurrentCycleIsEmpty()
    {
        var agent = AgentMJICraftSchedule.Instance();
        if (agent == null || agent->Data == null)
            return false;
        foreach (ref var w in agent->Data->WorkshopSchedulesSpan)
            if (w.NumScheduleEntries != 0)
                return false;
        return true;
    }

    public static void ClearCurrentCycleSchedule()
    {
        Service.Log.Info($"Clearing current cycle schedule");
        Utils.SynthesizeEvent(&AgentMJICraftSchedule.Instance()->AgentInterface, 6, new AtkValue[] { new() { Type = AtkValueType.Int, Int = 0 } });
    }

    public static void ScheduleItemToWorkshop(uint objId, int startingHour, int cycle, int workshop)
    {
        Service.Log.Info($"Adding schedule: {objId} @ {startingHour}/{cycle}/{workshop}");
        MJIManager.Instance()->ScheduleCraft((ushort)objId, (byte)((startingHour + 17) % 24), (byte)cycle, (byte)workshop);
    }

    // this is what the game uses to refresh the ui after adding schedules
    public static void ResetCurrentCycleToRefreshUI()
    {
        Service.Log.Info($"Resetting current cycle");
        var agent = AgentMJICraftSchedule.Instance();
        agent->SetDisplayedCycle(agent->Data->CycleDisplayed);
        agent->Data->Flags1 |= AgentMJICraftSchedule.DataFlags1.MaterialsUpdated; // ensure material assignment addon is updated
    }

    public static void SetCurrentCycle(int cycle)
    {
        Service.Log.Info($"Setting cycle: {cycle}");
        AgentMJICraftSchedule.Instance()->SetDisplayedCycle(cycle);
    }

    public static void SetRestCycles(uint mask)
    {
        Service.Log.Info($"Setting rest: {mask:X}");
        var agent = AgentMJICraftSchedule.Instance();
        agent->Data->NewRestCycles = mask;
        Utils.SynthesizeEvent(&agent->AgentInterface, 5, new AtkValue[] { new() { Type = AtkValueType.Int, Int = 0 } });
    }

    public static void RequestDemandFavors()
    {
        Service.Log.Info("Fetching demand & favors");
        MJIManager.Instance()->RequestDemandFull();
        MJIManager.Instance()->RequestFavorData();
    }

    public static int GetMaxWorkshops()
    {
        var mji = MJIManager.Instance();
        return mji == null ? 0 : mji->IslandState.CurrentRank switch
        {
            < 3 => 0,
            < 6 => 1,
            < 8 => 2,
            < 14 => 3,
            _ => 4,
        };
    }
}
