namespace HotelOS.RoomCare.Domain;

/// <summary>One of the property's service windows; it may cross midnight (chapter 03 §2.8, §7.2).</summary>
public class ServiceWindow
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public string Window { get; set; } = ServiceWindowName.Morning;

    public TimeOnly Starts { get; set; }

    public TimeOnly Ends { get; set; }

    public bool Enabled { get; set; } = true;

    public bool AllowAssignmentOutside { get; set; }

    public long Version { get; set; }

    /// <summary>Whether a local time of day falls inside <c>[Starts, Ends)</c>, crossing midnight allowed.</summary>
    /// <remarks>
    /// The reference tested <c>from &lt; now &amp;&amp; now &lt; to</c>, which a
    /// window from 18:00 to 07:00 can never satisfy (survey F2). A window whose
    /// end is not after its start wraps the day; equal ends are refused at Setup.
    /// </remarks>
    public bool Contains(TimeOnly local) =>
        Starts < Ends ? local >= Starts && local < Ends : local >= Starts || local < Ends;
}

/// <summary>What a service takes for one room type at this property — ADR 0044's row, in rows not columns.</summary>
public class ServiceStandard
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    /// <summary>Master Data's room type; null for an area's routine.</summary>
    public Guid? RoomTypeId { get; set; }

    public string Service { get; set; } = Domain.Service.DailyService;

    public int Minutes { get; set; }

    public decimal Credits { get; set; }

    public string InspectionRule { get; set; } = Domain.InspectionRule.None;

    /// <summary>The inspection app's checklist id — opaque here.</summary>
    public string? ChecklistRef { get; set; }

    public List<string> Phases { get; set; } = [];

    public long Version { get; set; }
}

/// <summary>The property's rules, one row, versioned — trigger, who leads, linen, thresholds (chapter 03 §2.8).</summary>
public class PropertyPolicy
{
    public Guid PropertyId { get; set; }

    public string TriggerMode { get; set; } = Domain.TriggerMode.Prepare;

    public string WhoLeads { get; set; } = Domain.WhoLeads.RoomCare;

    public string StaySource { get; set; } = Domain.StaySource.Pms;

    public string BoardDefaultView { get; set; } = BoardView.Map;

    public string StatesDefaultView { get; set; } = StatesView.Sheet;

    public string OnDepartureCondition { get; set; } = Condition.Dirty;

    public string LinenRuleKind { get; set; } = Domain.LinenRuleKind.EveryNDeferrable;

    public int LinenEveryDays { get; set; } = 3;

    public string Towels { get; set; } = TowelRule.Daily;

    public bool TurndownEnabled { get; set; }

    public int RefreshAfterDays { get; set; } = 3;

    public int DndRecheckMinutes { get; set; } = 60;

    public int SupervisorAfterDays { get; set; } = 2;

    /// <summary>Highest first; the bands of <see cref="PriorityBand"/>.</summary>
    public List<string> PriorityLadder { get; set; } =
        [PriorityBand.SoldTonight, PriorityBand.Departure, PriorityBand.Daily, PriorityBand.Refresh];

    public string AssignmentStrategy { get; set; } = Domain.AssignmentStrategy.SameZone;

    public string UnsoldDeparture { get; set; } = Domain.UnsoldDeparture.Today;

    /// <summary>The Housekeeping department's code in Master Data — where room tasks belong.</summary>
    public string DepartmentCode { get; set; } = "HK";

    public long Version { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public Guid? ChangedBy { get; set; }

    /// <summary>The standard a property has before anyone configured it — HosPilot's defaults (S0).</summary>
    public static PropertyPolicy DefaultFor(Guid propertyId) => new() { PropertyId = propertyId };
}

/// <summary>When a public area is cleaned — S3: Room Care's routine on Master Data's node.</summary>
public class AreaSchedule
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid LocationId { get; set; }

    /// <summary>Local times of day the routine is due.</summary>
    public List<TimeOnly> Times { get; set; } = [];

    public int Minutes { get; set; } = 20;

    public bool Enabled { get; set; } = true;

    public long Version { get; set; }
}

/// <summary>How often a room type is deep cleaned — S0.</summary>
public class DeepCleanPlan
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomTypeId { get; set; }

    public int EveryMonths { get; set; } = 6;

    public long Version { get; set; }
}

/// <summary>Which housekeeping zone a room is in — ADR 0044's aggregate, Room Care's.</summary>
public class RoomZoneAssignment
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }

    public Guid RoomId { get; set; }

    /// <summary>Master Data's zone.</summary>
    public Guid ZoneId { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveUntil { get; set; }

    public Guid? AssignedBy { get; set; }
}
