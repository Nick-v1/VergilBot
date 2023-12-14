using Vergil.Data.Context;

namespace Vergil.Services.Repositories;

public interface ISlotRepository
{
    Task<double> UpdateSlotStats(double value);
    Task<double> JackPotWin();
    Task UpdateWageredAmount(double value);
}


public class SlotRepository : ISlotRepository
{
    private readonly VergilDbContext _context;

    public SlotRepository(VergilDbContext context)
    {
        _context = context;
    }

    public async Task<double> UpdateSlotStats(double value)
    {
        var slot = await _context.Slots.FindAsync(1);

        slot.Jackpot += value;
        await _context.SaveChangesAsync();
        return slot.Jackpot;
    }

    public async Task UpdateWageredAmount(double value)
    {
        var slot = await _context.Slots.FindAsync(1);
        slot.Wagered += value;
        await _context.SaveChangesAsync();
    }

    public async Task<double> JackPotWin()
    {
        var slot = await _context.Slots.FindAsync(1);
        var amountWon = slot.Jackpot;
        slot.Jackpot = 0;
        await _context.SaveChangesAsync();
        return amountWon;
    }
    
}