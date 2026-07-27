# Hyperscape: Bounty Hunt (Konzept-Erweiterung)
### Hunt: Showdown trifft Mini-Battle-Royale

## Pitch
8 Spieler (später optional 4 Duos = 16) auf einer relativ großen Karte. Statt reinem "letzter Überlebender gewinnt", jagt ihr gemeinsam (aber konkurrierend) einen Boss: 3 über die Karte verteilte Hinweise finden, die seinen Standort verraten. Sobald alle 3 gefunden sind, wird der Boss-Ort für ALLE sichtbar (Hunt-Showdown-Bounty-Prinzip) – ab da wird's chaotisch. Gleichzeitig schrumpft eine Decay-Zone die Karte, damit das Mini-BR-Gefühl erhalten bleibt: am Ende bleibt nur noch ein kleiner Bereich, in dem sich die letzten übrig gebliebenen Spieler/Teams gegenüberstehen.

## Karte: 4 Stadtteile = Decay-Stufen
Die Karte ist in 4 Stadtteile aufgeteilt (2x2-Raster, siehe `DistrictMapGenerator.cs`). Idee: die Decay-Zone nutzt genau diese Stadtteil-Grenzen statt eines abstrakten schrumpfenden Kreises – pro Decay-Stufe fällt ein ganzer Stadtteil weg (Gebäude "lösen sich auf"), bis nur noch einer übrig ist. Spart eine separate Zonen-Logik und macht die Verkleinerung räumlich klar nachvollziehbar (man sieht buchstäblich einen Stadtteil verschwinden statt eines unsichtbaren Kreisrands).

## Ablauf in 4 Phasen

**1. Drop & Erkunden (0–3 Min):** Spieler droppen verteilt über die Karte, sammeln Waffen + Hacks wie im Grundsystem. Decay-Zone ist noch inaktiv.

**2. Hinweisjagd (ab Minute ~2):** 3 Hinweis-Punkte sind auf der Karte platziert, aber nicht automatisch sichtbar – aufspürbar über die **Wallhack-Ping-Fähigkeit** im Nahbereich (zweiter Nutzen für eine bereits existierende Mechanik, kein neues System nötig). Wer einen Hinweis zuerst findet und interagiert, schaltet ihn für **alle** Spieler frei (kein exklusiver Fortschritt einzelner Teams – erzeugt Wettlauf-Spannung statt stillem Einzelvorsprung).

**3. Boss-Reveal (sobald 3/3 gefunden):** Boss-Position + Icon erscheint auf der Karte für alle – der klassische Hunt-Showdown-Moment. Ab genau diesem Zeitpunkt beginnt die Decay-Zone zu schrumpfen, sodass alle gleichzeitig auf denselben Punkt zulaufen, während der Platz kleiner wird.

**4. Boss-Fight & Power Cache (kein Extraction):** Boss ist ein hochskalierter Gegner (wiederverwendete `Health.cs`, deutlich mehr HP + 1–2 einfache Angriffsmuster). Stirbt er, droppt er an Ort und Stelle einen **Power Cache** (seltene Waffe + stärkste Hack-Variante + kurzer Buff) – keine Extraction, kein Verlassen der Runde. Alle Spieler sehen den Cache-Ort auf der Karte aufleuchten, sobald der Boss fällt.

**Warum kein Extraction:** Ein Ausstieg nach dem Boss-Kill würde Spieler zu früh aus der Runde nehmen und die Lobby zu schnell ausdünnen – widerspricht dem BR-Charakter, bei dem alle bis zum Schluss im selben Match gegeneinander antreten.

## Der Boss-Kill als Machtspitze, nicht als Ausstieg
Der Anreiz, den Boss zuerst zu erledigen, ist nicht "ich gewinne dadurch", sondern "ich werde dadurch stark – muss die Beute jetzt aber gegen alle verteidigen, die den aufleuchtenden Cache-Ort auch gesehen haben". Das hält die Kernspannung von Battle Royale komplett aufrecht (jeder will am Ende jeden töten), gibt dem Boss-Kill aber einen klaren Belohnungsmoment mittendrin statt eines Abkürzungs-Sieges. Einzige Siegbedingung bleibt dadurch simpel: **letztes Team übrig, wenn die Decay-Zone alles auf einen winzigen Rest zusammengeschrumpft hat.**

## ELO – global, immer aktiv, kein separater Ranked-Modus
Kein Casual/Ranked-Split – **jedes Match zählt** für ein permanentes, spielerübergreifendes ELO-Rating (wie ein Ranked-Modus, nur eben immer). Der Power Cache ist die kurzfristige Belohnung (sofortige Stärke im laufenden Match), die ELO ist der langfristige Fortschritt über alle Matches hinweg.

**Boss-Kill gibt genauso viel ELO wie ein Match-Sieg – und beides stapelt sich.** Team gewinnt UND hat den Boss geholt = volle ELO für den Sieg + volle ELO für den Boss-Kill, addiert. Das macht den Boss zu einem eigenständigen, vollwertigen Erfolg und nicht nur zu einem Trostpreis für Verlierer.

Technisch bedeutet das: es braucht ein persistentes Spieler-Profil + globales Leaderboard-Backend (z. B. Unity Cloud Save/Leaderboards, oder wie beim Klugscheißer-Projekt eine Firebase-Anbindung) statt nur lokalem Highscore – zusätzlicher, aber überschaubarer Scope, da das Prinzip aus dem Klugscheißer-Projekt schon bekannt ist.

## Waffenverteilung
Feste Loot-Spawnpunkte über die Karte verteilt, wie im Original-Hyper-Scape (Waffen + Hacks liegen an festen Stellen, ggf. mit Seltenheitsstufen). Kein periodisches Drop-System – das Original hat mit festen Spots gut funktioniert, dabei bleiben wir.

## Team-Größe
MVP zuerst mit 8 Solo-Spielern bauen (einfacher zu synchronisieren/testen). Duos (16 Spieler, 4 Teams) als spätere Erweiterung – Team-Logik (Downed-Status/Revive, geteilte Kartenmarkierungen) ist zusätzlicher Aufwand und kein Blocker fürs erste Testen.

## Tech-Wiederverwendung (wichtig für Solo-Scope)
- **Decay-Zone**: 1:1 aus dem ursprünglichen 8-Spieler-Arena-Konzept übernehmbar
- **Wallhack-Ping**: bekommt zusätzlich Hinweis-Erkennung – kein neues Ability-System nötig
- **Health.cs**: für den Boss wiederverwendbar, nur `maxHealth` hoch + 1–2 Angriffs-Scripts ergänzen
- **Neu zu bauen**: Hinweis-Interactable (Trigger-Collider + netzwerk-synchronisiertes "gefunden"-Flag), einfache Boss-KI (State Machine: Idle → Chase → Attack), Power-Cache-Drop-Logik

## Offene Fragen für später
- Wie viele Decay-Stufen zwischen Boss-Reveal und Matchende?
- Kann der Boss selbst Spieler töten (PvE-Gefahr) oder ist er reiner Loot-Träger?
- Sollen Hinweise zufällig platziert werden (pro Match anders) oder auf festen Spots rotieren?
- Wie groß soll der Vorteil aus dem Power Cache sein, damit er sich lohnt, ohne den Rest der Runde chancenlos zu machen (Balance-Frage für später)?
