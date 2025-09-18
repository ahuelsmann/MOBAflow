namespace Moba.Backend.Model;

//KatjaNeural       (weiblich) 2. Place
//ConradNeural      (männlich)
//AmalaNeural       (weiblich)
//BerndNeural       (männlich)
//ChristophNeural   (männlich)
//ElkeNeural        (weiblich)  1. Platz
//FlorianMultilingualNeural (Männlich)
//GiselaNeural      (Weiblich, Kind) <- Witzig
//KasperNeural      (männlich)
//KillianNeural     (männlich)
//KlarissaNeural    (weiblich)  5. Platz
//KlausNeural       (männlich)
//LouisaNeural      (weiblich)  4. Platz
//MajaNeural        (weiblich)  3. Place
//RalfNeural        (männlich)
//TanjaNeural       (weiblich)  Stimme könnte passend für Ansange am Bahnsteig sein.

public class Voice
{
    public Voice()
    {
        Name = "ElkeNeural";
    }

    public string Name { get; set; }
    public decimal ProsodyRate { get; set; }
}