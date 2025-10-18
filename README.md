# Digitaler Zwilling für IHK-Abschlussprüfung EAT (Sortieranlage)

Dieses Projekt ist eine C# WinForms-Anwendung, die als digitaler Zwilling für die Sortieranlage aus den IHK-Abschlussprüfungsunterlagen (Winter 2025/26) dient. Die Anwendung simuliert das Bedienpult sowie die Sensoren und Aktoren der Anlage und kommuniziert in Echtzeit mit einer virtuellen Siemens S7-SPS, die über **S7-PLCSIM Advanced V6.0** simuliert wird.

---

## 🖥️ Benutzeroberfläche

Die fertige Anwendung simuliert das komplette Bedienfeld und die Aktorik/Sensorik der Anlage:

![Benutzeroberfläche des Digitalen Zwillings](./Bild.png)

---

## 🔧 Voraussetzungen

Stellen Sie sicher, dass die folgende Software installiert ist:

* **Microsoft Visual Studio:** Zum Entwickeln und Ausführen der C#-Anwendung.
* **Siemens TIA Portal:** Zum Erstellen und Verwalten des SPS-Programms.
* **Siemens S7-PLCSIM Advanced V6.0:** Zur Simulation der S7-1500 CPU.

---

## ⚙️ Einrichtung des C#-Projekts

Bevor Sie das Projekt zum ersten Mal starten, sind zwei wichtige Schritte notwendig:

1.  **Plattform auf x64 umstellen:**
    * Die Siemens PLCSIM Advanced API ist eine 64-Bit-Bibliothek. Daher muss das C#-Projekt zwingend für die **x64**-Plattform kompiliert werden.
    * Gehen Sie in Visual Studio zu `Build > Configuration Manager...` und stellen Sie die "Aktive Projektmappenplattform" auf `x64` ein.

2.  **API-Referenz hinzufügen:**
    * Das Projekt benötigt einen Verweis auf die `Siemens.Simatic.Simulation.Runtime.dll`.
    * Klicken Sie im Projektmappen-Explorer mit der rechten Maustaste auf `Verweise > Verweis hinzufügen... > Durchsuchen` und navigieren Sie zum Installationspfad der API (z. B. `C:\Program Files (x86)\Common Files\Siemens\PLCSIMADVAPI\V6.0\`).

---

## 🚀 Startanleitung für die Simulation

**Die Reihenfolge der Schritte ist entscheidend, um Fehler zu vermeiden!**

1.  **PLCSIM Advanced Instanz erstellen und starten:**
    * Öffnen Sie das **S7-PLCSIM Advanced Control Panel**.
    * Erstellen Sie eine neue virtuelle SPS-Instanz mit dem exakten Namen: **`CPU_IHK`**.
    * Starten Sie diese Instanz, indem Sie auf das **"Play"-Symbol (▶️)** klicken. Der Statuskreis sollte orange (STOP) oder grün (RUN) werden.

    
2.  **TIA Portal Projekt laden:**
    * Öffnen Sie Ihr TIA Portal-Projekt.
    * Wählen Sie die CPU aus und klicken Sie auf **"Laden in Gerät"**.
    * Wählen Sie als PG/PC-Schnittstelle den **"Siemens PLCSIM Virtual Ethernet Adapter"**.
    * Suchen Sie nach erreichbaren Teilnehmern. Ihre Instanz "CPU_IHK" sollte gefunden werden.
    * Laden Sie das Programm in die Instanz und starten Sie die CPU.

3.  **C#-Anwendung (Digitaler Zwilling) starten:**
    * Starten Sie erst **jetzt** die C#-Anwendung aus Visual Studio.
    * Das Programm wird sich automatisch mit der laufenden Instanz "CPU_IHK" verbinden. Es sollten keine Fehlermeldungen mehr bezüglich der Verbindung auftreten.

---

## 💡 Funktionsweise der Simulation

⚠️ **Wichtiger Hinweis zur B20-Simulation:**
Die Simulation des Abstandssensors `B20` (Schieberegler) in dieser Anwendung spiegelt **nicht das exakte Verhalten eines realen Sensors** wider. Die Werte und die simulierte Bewegung sind vereinfacht. Die **Schaltschwellen** (Positionen wie "EBA", "PA", "PM1", "PM2", "EBE") im SPS-Programm müssen **anhand der realen Hardware eingemessen und angepasst** werden!

### Steuerung der SPS-Eingänge (GUI -> SPS)

Benutzeraktionen in der Oberfläche werden direkt auf die Eingänge der SPS geschrieben.

* **Taster (S0, S2, etc.):** Die `MouseDown`- und `MouseUp`-Ereignisse schreiben `true` und `false` auf die digitalen Eingänge (`plc.InputArea.WriteBit(...)`).
* **Schalter (F1, F2, B0, NotHalt):** Das `CheckedChanged`-Ereignis von Checkboxen schreibt den Zustand auf einen digitalen Eingang.
* **Analoge Sensoren (B20, B21):**
    * Der Wert des `B20`-Schiebereglers wird bei jeder Bewegung invertiert und an das Eingangswort **%EW8** gesendet (`plc.InputArea.WriteBytes(8, ...)`).
    * Der Wert aus der `B21`-Textbox wird bei jeder Änderung an das Eingangswort **%EW6** gesendet (`plc.InputArea.WriteBytes(6, ...)`).

### Visualisierung der SPS-Ausgänge (SPS -> GUI)

Ein `Timer` in der C#-Anwendung liest alle 500 Millisekunden die Ausgänge der SPS aus und aktualisiert die Benutzeroberfläche.

* **Leuchtmelder (P0, P3, etc.):** Die `BackColor` der Buttons wird entsprechend dem Zustand der digitalen Ausgänge (`A0.4`, `A1.0`, etc.) geändert. Die Blink-Logik wird ebenfalls hierüber gesteuert.
* **Motoren (Q1, Q2, Q3):** Die Anzeige-Buttons leuchten grün, wenn die entsprechenden Ausgänge (`A0.0` - `A0.2`) aktiv sind.
* **Simulierte Bewegung:** Wenn ein Motor-Ausgang (`Q1`, `Q2`, `Q3`) aktiv ist, wird die Position des `B20`-Schiebereglers automatisch verändert, um die Bewegung des Förderbandes zu simulieren.
* **Zylinder (M4, M7, M10):** Die Farben der Anzeige-Buttons (`M4_0`, `M4_1`, etc.) werden entsprechend der Ventil-Ausgänge (`A4.1` - `A4.6`) aktualisiert.
