# Phase 1 Setup – Hyperscape Prototyp

Diese Scripts gehören in `Assets/Scripts/` in deinem Unity-Projekt (3D URP Template). Sobald das Projekt existiert, einfach die 5 `.cs`-Dateien in `Assets/Scripts/` reinziehen.

## GameObject-Setup

**Player:**
1. Neues leeres GameObject "Player" erstellen
2. Component "Character Controller" hinzufügen (Height 2, Radius 0.5, Center Y 1)
3. Scripts drauf: `PlayerMovement.cs`, `HackSystem.cs`, `SimpleShooter.cs`
4. Als Kind-Objekt eine Kamera hinzufügen, Position ca. (0, 1.6, 0) – das ist die Augenhöhe
5. Auf die Kamera: `MouseLook.cs`, im Inspector das "Player Body" Feld auf das Player-Objekt ziehen
6. In `PlayerMovement.cs` und `SimpleShooter.cs` im Inspector die Kamera ins "Player Camera"-Feld ziehen

**Test-Ziele (für Slam + Shooter):**
1. Ein paar Cubes/Capsules in die Szene stellen, Tag "Enemy" geben (Tag im Inspector oben links anlegen falls nicht vorhanden)
2. `Health.cs` draufziehen

**Boden/Arena:**
- Ein Plane oder mehrere Cubes als Plattformen für die Verticality – reicht fürs Testen von Movement/Slam völlig

## Steuerung (Prototyp)
- WASD: Bewegen
- Maus: Umschauen
- Leertaste: Springen (2x = Doppelsprung)
- Linksklick: Schießen
- Q: Dash
- E: Slam (nur in der Luft)
- F: Invisibility (kurz)
- R: Wallhack-Ping (markiert Gegner in der Nähe kurz in der Konsole/als Debug-Linie)

Play drücken und ausprobieren, wie sich Movement + Hacks anfühlen – darum geht's in Phase 1, noch kein Netcode, keine echten Gegner-KI.
