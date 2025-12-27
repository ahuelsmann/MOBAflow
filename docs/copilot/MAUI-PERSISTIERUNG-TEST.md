# MAUI Settings Persistierung - Test Guide

## ✅ Wie du die Persistierung RICHTIG testest

### ❌ Falsch (Deployment überschreibt Daten):
```
1. App mit Debugger starten
2. Werte ändern
3. Stop-Button drücken
4. Erneut mit Debugger starten ← Deployment überschreibt Settings!
```

### ✅ Richtig (Persistierung funktioniert):

#### **Test 1: App manuell neu starten**
1. **Mit Debugger starten** (F5)
2. **Werte ändern:**
   - Tracks: 3
   - Target: 15
   - Timer: 5s
3. **Stop-Button** drücken (App beenden)
4. **WICHTIG:** Am Handy/Emulator die App **MANUELL** über das App-Icon starten
5. **Ergebnis:** Werte sollten bei 3/15/5s sein ✅

#### **Test 2: App-Neustart ohne Deployment**
1. **Mit Debugger starten** (F5)
2. **Werte ändern**
3. **Handy/Emulator in Standby** (nicht Stop drücken!)
4. **App aus Task-Switcher beenden**
5. **App manuell starten** (Icon antippen)
6. **Ergebnis:** Werte persistent ✅

#### **Test 3: Release-Build**
```bash
# Erstelle Release-Build (kein Debugger)
dotnet build -c Release -f net9.0-android

# Deploy manuell
adb install -r bin/Release/net9.0-android/com.mobaflow.mobasmart-Signed.apk
```

## 📂 Wo werden Daten gespeichert?

**Pfad:** `/data/user/0/com.mobaflow.mobasmart/files/appsettings.json`

**Prüfen per ADB:**
```bash
# Datei anzeigen
adb shell cat /data/user/0/com.mobaflow.mobasmart/files/appsettings.json

# Datei auf PC kopieren
adb pull /data/user/0/com.mobaflow.mobasmart/files/appsettings.json
```

## 🔍 Debug-Ausgaben prüfen

**Beim App-Start solltest du sehen:**
```
✅ SettingsService Initialized
   Tracks: 3          ← Deine gespeicherten Werte!
   Target: 15
   Timer Interval: 5s
```

**Beim Wert ändern:**
```
🔔 OnCountOfFeedbackPointsChanged: 4
💾 SaveSettingsAsync called
✅ Settings saved successfully
   File size: 1007 bytes
   Last modified: 27.12.2025 22:25:30
```

## 🎯 Warum Debugger-Neustart Settings überschreibt

Visual Studio macht bei jedem Debug-Start:
1. **Fast Deployment** → Kopiert nur geänderte Dateien
2. **Aber:** `appsettings.json` wird **immer** kopiert (aus Projekt-Root)
3. **Resultat:** Deine App-Daten werden überschrieben

**Workaround:**
- Nach Änderungen: App **manuell** neu starten (nicht über Debugger)
- Oder: Release-Build verwenden
