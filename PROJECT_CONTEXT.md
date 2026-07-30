# Projekt-Kontext: Realm x Hyperscape (für zukünftige Claude-Sessions)

Diese Datei existiert, damit eine neue Claude-Session (z.B. auf einem anderen Rechner, ohne Zugriff auf frühere Gespräche) sofort den vollen Stand kennt. Wenn du diese Datei liest: lies sie komplett, bevor du irgendwas am Projekt vorschlägst oder änderst.

## Ausgangslage
Dieses Repo (`hypeclone` auf GitHub, `JeremyPPx/hypeclone`) enthielt ursprünglich Scripts für **Hyperscape**: einen 8-Spieler-Arena-Shooter in hypermoderner Stadt-Optik (inspiriert von Ubisofts Hyper Scape), mit Verticality über Dächer, Decay-Zone statt Kreis, 4 Hack-Fähigkeiten (Dash/Slam/Invisibility/Wallhack-Ping). Siehe `SETUP.md` in diesem Ordner für die Original-Scripts (`Health.cs`, `HackSystem.cs`, `PlayerMovement.cs`, `MouseLook.cs`, `SimpleShooter.cs`) und deren Setup.

**Das Projekt wurde am 30.07.2026 konzeptionell weiterentwickelt/gepivotet.** Grund: die prozedurale Stadt-/Dächer-Generierung (siehe `FullMapGenerator.cs`, `RoadGridGenerator.cs`, `TileDemoDistrict.cs`, `DistrictMapGenerator.cs`) war der eigentliche Zeitfresser, nicht Movement/Gunplay. Der Nutzer hat außerdem ~2.000 Stunden Realm Royale gespielt (Fantasy-Battle-Royale, 2018–2025, von Hi-Rez Studios, offiziell abgeschaltet Feb. 2025) und empfand dessen Terrain als "eine eigene Fantasy-Welt, unglaublich" – das ist der Ausgangspunkt für das neue Konzept.

## Aktuelles Konzept: "Realm x Hyperscape"
Fantasy-Battle-Royale mit Realm Royales Terrain-Ansatz als Basis + ausgewählten, weiterentwickelten Hyperscape-Elementen. **Kein 1:1-Klon von Realm Royale** – bewusst eigenständig weitergedacht.

### Kernschleife
1. Drop auf Terrain-Karte (Felder, Wälder, Burgruinen, Höhenstufen statt Dächer) – Ziel: kohärente, atmosphärische Fantasy-Welt, nicht nur funktionale Gameplay-Flächen (wichtigste Lehre aus der Realm-Erfahrung des Nutzers)
2. Loot: Waffen + Rüstung + Shards
3. **Schmiede:** Shards → craftbare Ausrüstung, sichtbarer Rauch verrät Position (Risk/Reward, aus Realm übernommen)
4. **Bei Tod: Geist-Mechanik** (Weiterentwicklung von Hyperscapes eigener "Echoes"-Idee, bewusst kein Chicken-Klon von Realm). Geist muss zu einem **Schrein** (eigenes Gebäude, getrennt von der Schmiede) wandern, wo ihn ein Teamkollege befreien/wiederbeleben kann. Schrein löst denselben Risk/Reward-Signal-Effekt aus wie die Schmiede (Position wird verraten).
5. **Zone: Decay statt Kreis** (aus Hyperscape übernommen), narrativ als "Verfall/Corruption des Realms" reflavored – Geländeabschnitte/Ruinen lösen sich sichtbar auf (Material-Wechsel + Collider-Toggle, kein Destruction-System nötig)

### Klassen (bestätigt: volle Klassen, nicht klassenlos)
5 Klassen geplant (Warrior, Engineer, Assassin, Mage, Hunter – wie im Realm-Royale-Original), aber **Phase 1 startet mit nur 2 Klassen:**
- **Warrior** (Nahkampf/Tank) – mechanisch am günstigsten, keine Projektil-/Ziel-KI-Logik
- **Hunter** (Fernkampf/Sniper) – Hitscan + Fallen, spielt gezielt auf Terrain-Sichtlinien/Verticality aus
- Grund: maximal unterschiedliche Spielstile testen, ob das Klassensystem trägt. Engineer (Turm-KI), Mage (AOE/Status), Assassin (Stealth-Detection) folgen erst in späteren Phasen – mehr Subsystem-Aufwand.

### Spieleranzahl
**20 echte Spieler + ~30 Bots** (Ziel: 50-Spieler-Kartengefühl). Wichtig: 20 echte, voll netzwerk-synchronisierte Spieler sind mehr Last als Hyperscapes ursprüngliche 8 – Empfehlung: von Peer-Host-über-Relay auf einen **dedizierten Headless-Server-Build** umsteigen (Unity Netcode for GameObjects unterstützt das nativ, Deployment auf günstiger Cloud-VM), damit die Stabilität nicht an der Upload-Bandbreite eines einzelnen Spieler-Geräts hängt. Bots laufen mit reduzierter Sync-Last (geringere Update-Rate, vereinfachte KI), damit die Gesamtlast trotz 50 sichtbarer Charaktere deutlich unter der einer echten 50-Spieler-Lobby bleibt.

### Tech-Stack (Wiederverwendung aus Hyperscape)
- Unity, aber **Standard-Terrain-System statt custom City-Generator** – `FullMapGenerator.cs`/`RoadGridGenerator.cs`/`TileDemoDistrict.cs`/`DistrictMapGenerator.cs` werden für das neue Konzept **nicht mehr gebraucht**
- Netcode for GameObjects + Relay als Ausgangspunkt, aber siehe Spieleranzahl-Abschnitt oben (dedizierter Server empfohlen bei 20 echten Spielern)
- Dissolve-Shader für Decay/Corruption – 1:1 aus Hyperscape übernehmbar
- `Health.cs`, `PlayerMovement.cs`, `MouseLook.cs`, `SimpleShooter.cs` direkt wiederverwendbar. `HackSystem.cs` wird zu klassenspezifischen Fähigkeiten umgebaut (Warrior/Hunter-Fähigkeiten statt generischer Hacks).

## Explizit NICHT im Scope (Phase 1)
- Alle 5 Klassen sofort (nur Warrior + Hunter zuerst)
- Echte 100%-synchronisierte 50-Spieler-Lobby ohne Bots
- Eigenes Server-Hosting über die dedizierte Headless-Server-Empfehlung hinaus
- Ranked/Battle-Pass/Saisons

## YouTube-Reihe ("App in 30 Tagen" / Baubuch)
**Bewusst noch nicht gestartet.** Kein Video zu diesem Konzept, solange das Prototyp-Fundament (Terrain, Warrior + Hunter, Schrein-Mechanik) nicht steht. Falls du in einer neuen Session gefragt wirst, ob/wann ein Video gemacht werden soll: das ist offen, nicht automatisch "jetzt".

## Ausführlichere Historie/Recherche (falls verfügbar)
Es gibt einen ausführlicheren Obsidian-Vault ("YouTube-Brain") mit vollständiger Kanal-Strategie, Recherche zu Realm Royale und der Entscheidungs-Historie – der liegt aber nur lokal auf dem ursprünglichen Mac (`~/Documents/ObsidianVaults/YouTube-Brain`), nicht in diesem Repo, und ist daher auf einem anderen Rechner vermutlich NICHT verfügbar. Diese Datei hier ist bewusst so geschrieben, dass sie eigenständig genügt.

## Nächster sinnvoller Schritt
Phase-1-Prototyp: Terrain-Testszene bauen (Unity Terrain-System, keine gekachelte Stadt), Warrior- und Hunter-Fähigkeiten aus `HackSystem.cs` ableiten, Schrein als einfaches Trigger-Objekt (Collider + Interaktion) analog zum ursprünglichen Setup in `SETUP.md` umsetzen.
