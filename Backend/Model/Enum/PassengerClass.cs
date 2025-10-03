namespace Moba.Backend.Model.Enum;

public enum PassengerClass
{
    None,
    First,              // 1. Klasse
    Second,             // 2. Klasse
    Third,              // 3. Klasse (historisch)
    FirstSecond,        // 1./2. Klasse (Kombinationswagen)
    SecondThird,        // 2./3. Klasse (historisch)
    Dining,             // Speisewagen
    Sleeping,           // Schlafwagen
    Baggage,            // Gepäckwagen
    MailCar,            // Postwagen
}
