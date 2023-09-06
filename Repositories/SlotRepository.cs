using Microsoft.EntityFrameworkCore;
using VergilBot.Services.Context;

namespace VergilBot.Repositories;

public interface ISlotRepository
{
    Task<double> UpdateSlotStats(double value);
    Task<double> JackPotWin();
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

    public async Task<double> JackPotWin()
    {
        var slot = await _context.Slots.FindAsync(1);
        var amountWon = slot.Jackpot;
        slot.Jackpot = 0;
        await _context.SaveChangesAsync();
        return amountWon;
    }
    
}

