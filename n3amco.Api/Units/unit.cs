namespace n3amco.Api.Units
{
    public class Unit
    {
        public int Id { get; set; }

        public string Name { get; set; } // Kg / Liter / Piece

        public string Code { get; set; } // KG / L / PCS

        public bool IsActive { get; set; } = true;
    }
}
