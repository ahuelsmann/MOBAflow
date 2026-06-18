$ErrorActionPreference = 'Stop'

function Update-MasterDataFile {
    param([string]$Path)

    $content = Get-Content $Path -Raw -Encoding UTF8

    # Categories
    $content = $content -replace 'Dampflokomotiven', 'Steam locomotives'
    $content = $content -replace 'Elektrolokomotiven', 'Electric locomotives'
    $content = $content -replace 'Diesellokomotiven', 'Diesel locomotives'
    $content = $content -replace 'ICE-Züge', 'ICE trains'
    $content = $content -replace 'Elektrotriebwagen', 'Electric railcars'
    $content = $content -replace 'Dieseltriebwagen', 'Diesel railcars'
    $content = $content -replace 'Historische Triebwagen', 'Historic railcars'

    # Types
    $content = $content -replace '"Type": "Dampflok"', '"Type": "Steam loco"'
    $content = $content -replace '"Type": "Elektrolok"', '"Type": "Electric loco"'
    $content = $content -replace '"Type": "Diesellok"', '"Type": "Diesel loco"'
    $content = $content -replace '"Type": "Triebzug"', '"Type": "Railcar"'
    $content = $content -replace '"Type": "Elektrotriebwagen"', '"Type": "Electric railcar"'
    $content = $content -replace '"Type": "Dieseltriebwagen"', '"Type": "Diesel railcar"'

    # Stations/platforms
    $content = $content -replace 'Hauptbahnhof', 'Central Station'
    $content = $content -replace 'Gleis ', 'Track '
    $content = $content -replace ' Bahnhof', ' Station'

    $descMap = [ordered]@{
        'Schnellzug-Dampflok' = 'Express steam locomotive'
        'Stromlinie Schnellzuglok' = 'Streamlined express steam locomotive'
        'Schnellzuglok DB' = 'DB express steam locomotive'
        'Stromlinien-Schnellzuglok' = 'Streamlined express steam locomotive'
        'Schnellzuglok' = 'Express steam locomotive'
        'Neubau-Personenzuglok' = 'Rebuilt passenger steam locomotive'
        'Güterzug-/Personenzuglok' = 'Freight/passenger steam locomotive'
        'Personenzuglok' = 'Passenger steam locomotive'
        'Güterzuglok' = 'Freight steam locomotive'
        'Preußische P 8' = 'Prussian P 8'
        'Preußische P 10' = 'Prussian P 10'
        'Bayerische S 3/6' = 'Bavarian S 3/6'
        'Badische IV h' = 'Baden IV h'
        'Tenderlok' = 'Tank locomotive'
        'Rangierlok' = 'Shunting locomotive'
        'Elektrolok' = 'Electric locomotive'
        'Diesellok' = 'Diesel locomotive'
        'Triebwagen' = 'Railcar'
        'Triebzug' = 'Railcar trainset'
        'Elektrotriebwagen' = 'Electric railcar'
        'Dieseltriebwagen' = 'Diesel railcar'
        'Hochleistungslok' = 'High-performance locomotive'
        'Universal-Lok' = 'Universal locomotive'
        'Universal-Diesellok' = 'Universal diesel locomotive'
        'Schwere Güterzuglok' = 'Heavy freight locomotive'
        'Schwere Güterzug-Diesellok' = 'Heavy freight diesel locomotive'
        'Mittelstarke Diesellok' = 'Medium-power diesel locomotive'
        'Leichte Diesellok' = 'Light diesel locomotive'
        'Kleinlok' = 'Small locomotive'
        'ICE 1' = 'ICE 1'
        'ICE 2' = 'ICE 2'
        'ICE 3' = 'ICE 3'
        'ICE 4' = 'ICE 4'
        'ICE T' = 'ICE T'
        'ICE TD' = 'ICE TD'
        'ICE Sprinter' = 'ICE Sprinter'
        'Regionaltriebwagen' = 'Regional railcar'
        'Stadttriebwagen' = 'City railcar'
        'Nahverkehrstriebwagen' = 'Commuter railcar'
        'S-Bahn-Triebwagen' = 'Suburban railcar'
        'U-Bahn' = 'Metro'
        'Historischer Triebwagen' = 'Historic railcar'
    }

    foreach ($entry in $descMap.GetEnumerator()) {
        $content = $content.Replace($entry.Key, $entry.Value)
    }

    $descMap2 = [ordered]@{
        'Preußische' = 'Prussian'
        'Güterzuglok' = 'Freight locomotive'
        'Schwere Güterzuglok' = 'Heavy freight locomotive'
        'Güterzug-/Passenger steam locomotive' = 'Freight/passenger steam locomotive'
        'Kriegslok' = 'War locomotive'
        'Einheits-Passenger steam locomotive' = 'Standard passenger steam locomotive'
        'Neubau-Tank locomotive' = 'Rebuilt tank locomotive'
        'Einheits-Tank locomotive' = 'Standard tank locomotive'
        'Tank locomotive Gebirge' = 'Mountain tank locomotive'
        'Bergbahn-Tank locomotive' = 'Mountain railway tank locomotive'
        'Lokalbahnlok' = 'Branch line locomotive'
        'Vorserien-Express steam locomotive' = 'Pre-production express steam locomotive'
        'Einheitslok' = 'Standard locomotive'
        'Einheitslok Aerodynamik' = 'Streamlined standard locomotive'
        'Nahverkehrslok' = 'Commuter locomotive'
        'Universallok' = 'Universal locomotive'
        'Lokalbahn-Ellok' = 'Branch line electric locomotive'
        'Krokodil' = 'Crocodile'
        'Deutsches Krokodil' = 'German crocodile'
        'IC/EC-Lok' = 'IC/EC locomotive'
        'S-Bahn/Nahverkehr' = 'Suburban/commuter service'
        'S-Bahn München' = 'Munich suburban service'
        'DR-Express steam locomotive' = 'DR express steam locomotive'
        'ex DR 112 umgebaut' = 'rebuilt ex DR 112'
        'Drehstromlok IC' = 'AC locomotive IC'
        'DR-Universallok' = 'DR universal locomotive'
        'TRAXX-Güterlok' = 'TRAXX freight locomotive'
        'TRAXX-Personenverkehr' = 'TRAXX passenger service'
        'DR 250 Güterlok' = 'DR 250 freight locomotive'
        'Zweisystemlok' = 'Dual-system locomotive'
        'Zweisystem CZ/D' = 'Dual-system CZ/D'
        'Zweisystem D/F' = 'Dual-system D/F'
        'Wehrmachtslok' = 'Wehrmacht locomotive'
        'Streckenlok' = 'Main line locomotive'
        'Shunting locomotive Funk' = 'Radio-controlled shunting locomotive'
        'Gasturbine' = 'Gas turbine'
        'Express steam locomotive verstärkt' = 'Reinforced express steam locomotive'
        'ex V 163 umgebaut' = 'rebuilt ex V 163'
        'DR 119 Umbau' = 'DR 119 rebuild'
        'DR Großdiesel' = 'DR large diesel'
        'Sowjetische Großdiesel' = 'Soviet large diesel'
        'Ludmilla Umbau' = 'Ludmilla rebuild'
        'V 60 ex DR' = 'V 60 ex DR'
    }
    foreach ($entry in $descMap2.GetEnumerator()) {
        $content = $content.Replace($entry.Key, $entry.Value)
    }

    Set-Content -Path $Path -Value $content -Encoding UTF8 -NoNewline
}

Update-MasterDataFile -Path (Join-Path $PSScriptRoot '..\MOBAflow\data.json')
Update-MasterDataFile -Path (Join-Path $PSScriptRoot '..\MOBAflow\solution.json')

Write-Host 'Master data files updated.'
