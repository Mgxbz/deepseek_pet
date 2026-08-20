namespace DeepSeekPet.Core.Balance;

public sealed class DailySpendTracker
{
    public DateOnly? Date { get; private set; }

    public decimal StartTotal { get; private set; }

    public decimal LastTotal { get; private set; }

    public decimal SpentToday { get; private set; }

    public void Restore(DateOnly? date, decimal startTotal, decimal lastTotal)
    {
        Date = date;
        StartTotal = startTotal;
        LastTotal = lastTotal;
        var today = DateOnly.FromDateTime(DateTime.Now);
        SpentToday = date == today ? Math.Max(0, startTotal - lastTotal) : 0;
    }

    public bool OnBalance(decimal total, DateOnly today)
    {
        if (Date != today)
        {
            Date = today;
            StartTotal = total;
            LastTotal = total;
            SpentToday = 0;
            return true;
        }

        var changed = false;
        if (total > LastTotal)
        {
            StartTotal += total - LastTotal;
            changed = true;
        }

        var spent = Math.Max(0, StartTotal - total);
        if (spent != SpentToday || total != LastTotal)
        {
            SpentToday = spent;
            LastTotal = total;
            changed = true;
        }

        return changed;
    }
}
