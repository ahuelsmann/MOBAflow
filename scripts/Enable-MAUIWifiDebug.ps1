#!/usr/bin/env pwsh
Write-Host "=== MAUI Android WLAN-Debugging Setup ===" -ForegroundColor Cyan

# --- Prüfen, ob adb verfügbar ist ---
$adb = "adb"
try {
    $adbVersion = & $adb version 2>$null
} catch {
    Write-Host "ADB wurde nicht gefunden. Installiere Android Platform Tools und stelle sicher, dass adb im PATH liegt." -ForegroundColor Red
    exit 1
}

Write-Host "ADB gefunden: $adbVersion" -ForegroundColor Green

# --- USB-Gerät finden ---
$usbDevices = & $adb devices | Select-String "device$"

if ($usbDevices.Count -eq 0) {
    Write-Host "Kein per USB verbundenes Android-Gerät gefunden." -ForegroundColor Yellow
    exit 1
}

Write-Host "USB-Gerät erkannt." -ForegroundColor Green

# --- TCP/IP-Modus aktivieren ---
Write-Host "Aktiviere ADB TCP/IP-Modus auf Port 5555..."
& $adb tcpip 5555 | Out-Null
Start-Sleep -Seconds 1

# --- IP-Adresse des Geräts auslesen ---
Write-Host "Lese IP-Adresse des Geräts aus..."

$routes = & $adb shell ip route

# Alle IPs extrahieren
$ips = ($routes | Select-String -Pattern "src\s+([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)") `
    | ForEach-Object { $_.Matches[0].Groups[1].Value }

if (-not $ips -or $ips.Count -eq 0) {
    Write-Host "Konnte keine IP-Adresse finden." -ForegroundColor Red
    exit 1
}

Write-Host "Gefundene IPs: $($ips -join ', ')" -ForegroundColor Yellow

# WLAN-IP auswählen (private IPv4 ranges)
$wifiIp = $ips | Where-Object {
    $_ -like "192.168.*" -or
    $_ -like "10.*" -or
    ($_ -like "172.*" -and ([int]($_.Split('.')[1]) -ge 16 -and [int]($_.Split('.')[1]) -le 31))
} | Select-Object -First 1

if (-not $wifiIp) {
    Write-Host "Keine gültige WLAN-IP gefunden." -ForegroundColor Red
    exit 1
}

Write-Host "Verwende WLAN-IP: $wifiIp" -ForegroundColor Green

# --- WLAN-Verbindung herstellen ---
Write-Host "Verbinde über WLAN..."
$connectResult = & $adb connect "$wifiIp`:5555"

Write-Host $connectResult

if ($connectResult -notmatch "connected") {
    Write-Host "WLAN-Verbindung fehlgeschlagen." -ForegroundColor Red
    exit 1
}

Write-Host "WLAN-Debugging erfolgreich aktiviert!" -ForegroundColor Green
