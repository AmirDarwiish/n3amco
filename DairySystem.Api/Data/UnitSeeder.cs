using DairySystem.Api.Units;
using Microsoft.EntityFrameworkCore;

public static class UnitSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>().HasData(
            new Unit { Id = 1, Name = "Kilogram", Code = "KG", IsActive = true },
            new Unit { Id = 2, Name = "Liter", Code = "L", IsActive = true },
            new Unit { Id = 3, Name = "Piece", Code = "PCS", IsActive = true }
        );
    }
}