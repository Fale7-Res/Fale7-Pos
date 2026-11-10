namespace Falah.Infrastructure.BusinessDay;

using Falah.Infrastructure.Persistence.Entities;

/// <summary>
/// Which shifts belong to a business day: the ones opened under it.
///
/// Not the shifts whose calendar date matches the day's date - a business day that begins in the
/// evening runs past midnight, and every shift opened after midnight carries the next calendar date
/// while still being part of this day's trade, cash and close. Shifts record the business day they
/// were opened under (ShiftEntity.BusinessDayId, stamped by OpenShiftHandler). A shift opened before
/// that was recorded has nothing but its date, which is the only rule it was ever reported under, so
/// it is still matched by date.
/// </summary>
public static class BusinessDayShifts
{
    public static IQueryable<ShiftEntity> Of(IQueryable<ShiftEntity> shifts, BusinessDayEntity day) =>
        shifts.Where(s => s.BusinessDayId == day.Id || (s.BusinessDayId == null && s.BusinessDate == day.BusinessDate));
}
