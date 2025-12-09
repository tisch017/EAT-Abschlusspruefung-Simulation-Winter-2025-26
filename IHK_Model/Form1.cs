using System;
using System.Drawing;
using System.Windows.Forms;
using Siemens.Simatic.Simulation.Runtime; // The correct namespace from your instructions


namespace IHK_Model
{

    public partial class Form1 : Form
    {
        // The PLC instance, using the correct interface type 'IInstance'.
        public IInstance plc;
        private Timer updateTimer;
        private bool blinkState = false; // Used for 1Hz blinking effect

        public Form1()
        {
            InitializeComponent();
            NotHalt.Checked = true;
            B8.Checked = true;
            B9.Checked = true;
            B11.Checked = true;
        }

        #region PLC Connection and Shutdown

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Suche nach der Instanz "CPU_IHK" über die korrekte Eigenschaft.
                var allInstances = SimulationRuntimeManager.RegisteredInstanceInfo; // KORREKTUR: Dies ist eine Eigenschaft, keine Methode.
                IInstance foundInstance = null;

                foreach (var instanceInfo in allInstances)
                {
                    if (instanceInfo.Name == "CPU_IHK")
                    {
                        // Instanz gefunden, erstelle eine Schnittstelle dazu.
                        foundInstance = SimulationRuntimeManager.CreateInterface(instanceInfo.Name);
                        break;
                    }
                }

                // 2. Prüfe das Ergebnis der Suche
                if (foundInstance == null)
                {
                    // Wenn keine Instanz gefunden wurde, zeige eine Fehlermeldung an und beende das Programm.
                    MessageBox.Show(
                        "Die SPS-Instanz 'CPU_IHK' wurde nicht gefunden.\n\n" +
                        "Bitte erstellen Sie die Instanz im S7-PLCSIM Advanced Control Panel und laden Sie Ihr TIA-Projekt hinein, bevor Sie dieses Programm starten.",
                        "Instanz nicht gefunden",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    this.Close();
                    return;
                }

                // 3. Verbindung erfolgreich, weise die gefundene Instanz der Klassenvariable zu.
                plc = foundInstance;

                // 3. Stelle sicher, dass die Instanz eingeschaltet ist
                if (plc.OperatingState == EOperatingState.Off)
                {
                    plc.PowerOn(10000); // Schaltet die Instanz ein (Timeout 10s)

                }

                // 4. Stelle sicher, dass die SPS läuft und nicht leer ist.
                if (plc.OperatingState != EOperatingState.Run)
                {
                    plc.Run();
                }
            }
            catch (SimulationRuntimeException sre) when (sre.HResult == (int)ERuntimeErrorCode.IsEmpty)
            {
                // Die Instanz wurde gefunden, aber es ist kein Programm geladen.
                MessageBox.Show(
                    "Die Instanz 'CPU_IHK' wurde gefunden, ist aber leer.\n\n" +
                    "Bitte laden Sie Ihr TIA-Projekt in die laufende Instanz und starten Sie dieses Programm neu.",
                    "SPS-Programm fehlt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.Close();
            }
            catch (Exception ex)
            {
                // Fange alle anderen möglichen Fehler ab.
                MessageBox.Show("Ein Fehler ist beim Verbinden mit der SPS aufgetreten:\n\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            // Initialize and start the timer for continuous I/O updates
            updateTimer = new Timer();
            updateTimer.Interval = 500; // 500ms creates a 1Hz blink cycle (on for 500ms, off for 500ms)
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Enabled = true;

            MessageBox.Show("Instance 'CPU_IHK' created and powered on successfully!", "Success");

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        #endregion

        #region Main Update Loop (Timer Tick)

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (plc == null) return;

            // Toggle the blink state for 1Hz flashing
            blinkState = !blinkState;

            try
            {
                // === 1. READ OUTPUTS from PLC and UPDATE GUI ===
                UpdateIndicatorLights();
                UpdateMotorOutputs();
                UpdateCylinderOutputs();
            }
            catch (Exception ex)
            {
                updateTimer.Stop();
                MessageBox.Show("Communication Error: " + ex.Message, "Error");
            }
        }

        #endregion

        #region GUI Update Methods (Called by Timer)

        private void UpdateIndicatorLights()
        {
            // Read all indicator light outputs from the PLC
            bool p0_On = plc.OutputArea.ReadBit(0, 4); // A0.4 - P0
            bool p3_On = plc.OutputArea.ReadBit(1, 0); // A1.0 - P3
            bool p4_On = plc.OutputArea.ReadBit(1, 1); // A1.1 - P4
            bool p6_On = plc.OutputArea.ReadBit(1, 2); // A1.2 - P6
            bool p7_On = plc.OutputArea.ReadBit(1, 3); // A1.3 - P7
            bool p8_On = plc.OutputArea.ReadBit(1, 4); // A1.4 - P8
            bool p10_On = plc.OutputArea.ReadBit(1, 5); // A1.5 - P10
            bool p11_On = plc.OutputArea.ReadBit(1, 6); // A1.6 - P11

            // P0 (Betriebsdruck): Blinks if no pressure, solid if pressure is okay
            bool B0_state = plc.InputArea.ReadBit(4, 0); // E4.0
            P0.BackColor = p0_On ? Color.White : Color.Gray;

            // P3 (Handbetrieb) & P4 (Automatik): Can blink or be solid
            S3_P3.BackColor = p3_On ? Color.White : Color.Gray;
            S4_P4.BackColor = p4_On ? Color.White : Color.Gray;

            // P6 (Anlagenstart): Can blink or be solid
            S6_P6.BackColor = p6_On ? Color.White : Color.Gray;



            // Other indicators
            S7_P7.BackColor = p7_On ? Color.White : Color.Gray;
            S8_P8.BackColor = p8_On ? Color.White : Color.Gray;
            P10.BackColor = p10_On ? Color.Yellow : Color.Gray;
            P11.BackColor = p11_On ? Color.White : Color.Gray;
        }

        private void UpdateMotorOutputs()
        {
            // Lese die Motor-Ausgänge von der SPS
            bool q1_On = plc.OutputArea.ReadBit(0, 0); // A0.0 - Band rechts langsam
            bool q2_On = plc.OutputArea.ReadBit(0, 1); // A0.1 - Band links langsam
            bool q3_On = plc.OutputArea.ReadBit(0, 2); // A0.2 - Band rechts schnell

            // Aktualisiere die Farben der Buttons
            Q1.BackColor = q1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            Q2.BackColor = q2_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            Q3.BackColor = q3_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);

            // --- Position des B20-Sensors simulieren ---

            int currentValue = B20.Value;
            int newValue = currentValue;
            bool movingRight = false; // Merker, ob Bewegung nach rechts stattfindet

            // Versuche, die Schrittweite aus der Textbox zu lesen
            if (int.TryParse(MotorMove.Text, out int moveSpeed))
            {
                // Berechne die potenzielle neue Position
                if (q1_On)
                {
                    newValue += moveSpeed;
                    movingRight = true;
                }
                if (q2_On)
                {
                    newValue -= moveSpeed;
                    movingRight = false; // Bewegung nach links
                }
                if (q3_On)
                {
                    newValue += (moveSpeed * 2);
                    movingRight = true;
                }
            }

            // --- NEU: Rampe am Ende simulieren ---
            const int rampThreshold = 27048;
            const int maxValue = 27648; // Maximum des Sliders B20

            // Wenn sich das Band nach rechts bewegt UND der aktuelle Wert unter dem Max. ist
            // UND der neue Wert den Schwellenwert erreicht/überschreitet,
            // dann setze den Wert direkt auf das Maximum.
            if (movingRight && currentValue < maxValue && newValue >= rampThreshold)
            {
                newValue = maxValue;
            }
            // --- Ende Rampe ---

            // Stelle sicher, dass der Wert innerhalb der Slider-Grenzen bleibt.
            if (newValue > B20.Maximum)
            {
                newValue = B20.Maximum;
            }
            if (newValue < B20.Minimum)
            {
                newValue = B20.Minimum;
            }

            // Setze den neuen Wert für den Schieberegler und sende ihn an die SPS,
            // falls er sich geändert hat.
            if (B20.Value != newValue)
            {
                B20.Value = newValue;
                SendB20ValueToPlc();
            }
        }

        private void UpdateCylinderOutputs()
        {
            // Read cylinder valve outputs
            bool m4_0_On = plc.OutputArea.ReadBit(4, 1); // A4.1 - M4 ausfahren
            bool m4_1_On = plc.OutputArea.ReadBit(4, 2); // A4.2 - M4 einfahren
            bool m7_0_On = plc.OutputArea.ReadBit(4, 3); // A4.3 - M7 ausfahren
            bool m7_1_On = plc.OutputArea.ReadBit(4, 4); // A4.4 - M7 einfahren
            bool m10_0_On = plc.OutputArea.ReadBit(4, 5); // A4.5 - M10 ausfahren
            bool m10_1_On = plc.OutputArea.ReadBit(4, 6); // A4.6 - M10 einfahren

            // Update button colors to show valve status
            M4_0.BackColor = m4_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M4_1.BackColor = m4_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M7_0.BackColor = m7_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M7_1.BackColor = m7_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M10_0.BackColor = m10_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M10_1.BackColor = m10_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);

            // === NEU: Zylinderausgänge steuern die Sensor-Checkboxen ===
            if (m4_0_On) B8.Checked = true;
            if (m4_1_On)
            {
                B7.Checked = true;
                B1.Checked = false;
                B20.Value = 600;
                SendB20ValueToPlc();
            }


            if (m7_0_On) B10.Checked = true;
            if (m7_1_On) B9.Checked = true;

            if (m10_0_On) B12.Checked = true;
            if (m10_1_On) B11.Checked = true;
        }

        #endregion

        #region Input Event Handlers (Writing to PLC)

        // Generic handler for all pushbutton-style inputs
        private void HandleMouseDown(uint byteAddr, byte bitAddr)
        {
            if (plc != null) plc.InputArea.WriteBit(byteAddr, bitAddr, true);
        }

        private void HandleMouseUp(uint byteAddr, byte bitAddr)
        {
            if (plc != null) plc.InputArea.WriteBit(byteAddr, bitAddr, false);
        }

        // --- S-Taster ---
        private void S0_MouseDown(object sender, MouseEventArgs e) { /* Not connected to PLC input per schematic */ }
        private void S0_MouseUp(object sender, MouseEventArgs e) { /* Not connected to PLC input per schematic */ }

        private void S2_MouseDown(object sender, MouseEventArgs e) => HandleMouseUp(0, 1); // E0.1  //Hier sind HandleMouseDown und Up getsucht da es sich um NO handelt
        private void S2_MouseUp(object sender, MouseEventArgs e) =>  HandleMouseDown(0, 1);

        private void S3_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 2); // E0.2
        private void S3_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 2);

        private void S4_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 3); // E0.3
        private void S4_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 3);

        private void S5_MouseDown(object sender, MouseEventArgs e) =>  HandleMouseUp(0, 4);// E0.4  //Hier sind HandleMouseDown und Up getsucht da es sich um NO handelt
        private void S5_MouseUp(object sender, MouseEventArgs e) => HandleMouseDown(0, 4);

        private void S6_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 5); // E0.5
        private void S6_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 5);

        private void S7_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 6); // E0.6
        private void S7_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 6);

        private void S8_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 7); // E0.7
        private void S8_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 7);

        private void S10_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(1, 1); // E1.1
        private void S10_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(1, 1);

        private void S13_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(1, 4); // E1.4
        private void S13_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(1, 4);

        // --- Not-Halt ---
        private void NotHalt_CheckedChanged(object sender, EventArgs e)
        {
            // This is a latching switch, not a momentary button
            // Note: The physical NOT-HALT is -S11, but the "Bedienerschutz quittiert" signal is -F9 at E0.0
            // We will simulate the quit button -S12 with the NotHalt GUI element for simplicity.
            if (plc != null) plc.InputArea.WriteBit(0, 0, !NotHalt.Checked); // E0.0

            // Visual Feedback
            if (NotHalt.Checked)
            {
                NotHalt.BackColor = Color.DarkRed;
                NotHalt.Text = "ENTRIEGELN";
                S12_P12.BackColor = Color.Blue;
                F9.Checked = false;

            }
            else
            {
                NotHalt.BackColor = Color.Red;
                NotHalt.Text = "NOT-HALT";
            }
        }

        // --- Simulated Sensors and States ---
        private void K0_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 5, K0.Checked); // E1.5
        private void F1_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 6, !F1.Checked); // E1.6
        private void F2_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 7, !F2.Checked); // E1.7

        private void F9_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(0, 0, F9.Checked); // E1.7
        private void B0_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(4, 0, B0.Checked); // E4.0

        // B1 CheckBox Handler ===
        private void B1_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = B1.Checked;
            plc?.InputArea.WriteBit(4, 1, isChecked); // E4.1
            B1.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        // B3 CheckBox Handler ===
        private void B3_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = B3.Checked;
            plc?.InputArea.WriteBit(4, 3, isChecked); // E4.3
            B3.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }
        // === EVENT HANDLER FÜR CHECKBOXEN MIT VERRIEGELUNG ===
        private void B7_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B8
            if (B7.Checked)
            {
                B8.Checked = false;
            }

            bool isChecked = B7.Checked;
            plc?.InputArea.WriteBit(4, 7, isChecked); // E4.7
            B7.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        private void B8_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B7
            if (B8.Checked)
            {
                B7.Checked = false;
            }

            bool isChecked = B8.Checked;
            plc?.InputArea.WriteBit(5, 0, isChecked); // E5.0
            B8.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        private void B9_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B10
            if (B9.Checked)
            {
                B10.Checked = false;
            }

            bool isChecked = B9.Checked;
            plc?.InputArea.WriteBit(5, 1, isChecked); // E5.1
            B9.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        private void B10_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B9
            if (B10.Checked)
            {
                B9.Checked = false;
            }

            bool isChecked = B10.Checked;
            plc?.InputArea.WriteBit(5, 2, isChecked); // E5.2
            B10.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        private void B11_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B12
            if (B11.Checked)
            {
                B12.Checked = false;
            }

            bool isChecked = B11.Checked;
            plc?.InputArea.WriteBit(5, 3, isChecked); // E5.3
            B11.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }

        private void B12_CheckedChanged(object sender, EventArgs e)
        {
            // Verriegelung gegen B11
            if (B12.Checked)
            {
                B11.Checked = false;
            }

            bool isChecked = B12.Checked;
            plc?.InputArea.WriteBit(5, 4, isChecked); // E5.4
            B12.BackColor = isChecked ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
        }
        #endregion

        private void S12_P12_Click(object sender, EventArgs e)
        {
            if (!NotHalt.Checked)
            {
                S12_P12.BackColor = Color.Gray;
                F9.Checked = true;
            }
        }

        private void S0_Click(object sender, EventArgs e)
        {
            K0.Checked = false;
            S1_P1.BackColor = Color.Gray;
        }

        private void S1_P1_Click(object sender, EventArgs e)
        {
            K0.Checked = true;
            S1_P1.BackColor = Color.White;
        }

        private void B21_TextChanged(object sender, EventArgs e)
        {
            // 2. Den Text aus der TextBox auslesen.
            string temperaturText = this.B21.Text;

            // 3. Den Text sicher in eine 16-Bit-Zahl (short) konvertieren.
            //    Dies verhindert Abstürze bei ungültiger Eingabe (z.B. "abc").
            if (short.TryParse(temperaturText, out short temperaturWert))
            {
                try
                {
                    // 4. Die 16-Bit-Zahl in ein Array aus 2 Bytes umwandeln.
                    byte[] bytesToSend = BitConverter.GetBytes(temperaturWert);

                    // 5. WICHTIG: Byte-Reihenfolge für die SPS anpassen (Big-Endian).
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytesToSend);
                    }

                    // 6. Die 2 Bytes an die Adresse des analogen Eingangsworts (%EW64) senden.
                    plc.InputArea.WriteBytes(6, bytesToSend);
                }
                catch (Exception ex)
                {
                    // Optional: Fehlerbehandlung, falls die Kommunikation während des Schreibens fehlschlägt.
                    // Zum Beispiel: Console.WriteLine("Fehler beim Senden von B21: " + ex.Message);
                }
            }
        }

        private void B20_Scroll(object sender, EventArgs e)
        {
            SendB20ValueToPlc(); // Ruft die Sende-Logik auf
        }

        private void SendB20ValueToPlc()
        {
            // Prüfen, ob die SPS-Verbindung besteht und die Simulation läuft.
            if (plc == null || plc.OperatingState != EOperatingState.Run)
            {
                return;
            }

            // 1. Den aktuellen Wert des Schiebereglers auslesen.
            int sensorWert = B20.Value;

            // 2. Den Wert INVERTIEREN.
            const int maxValue = 27648;
            int invertedWert = maxValue - sensorWert;

            // 3. Den invertierten Wert in das 16-Bit-Format für die SPS umwandeln.
            short plcValue = (short)invertedWert;

            try
            {
                // 4. Den 16-Bit-Wert in ein Array aus 2 Bytes umwandeln.
                byte[] bytesToSend = BitConverter.GetBytes(plcValue);

                // 5. Byte-Reihenfolge für die Siemens SPS anpassen (Big-Endian).
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytesToSend);
                }

                // 6. Die 2 Bytes an die Adresse des analogen Eingangsworts senden (Beispiel: %EW8).
                plc.InputArea.WriteBytes(8, bytesToSend);
            }
            catch (Exception)
            {
                // Fehler beim Schreiben ignorieren.
            }
        }

        private void info_B20_Click(object sender, EventArgs e)
        {
            string infoText =
        "⚠️ **Wichtiger Hinweis zur B20-Simulation:**\n" +
        "Der simulierte Abstandssensor B20 (Schieberegler) spiegelt nicht das exakte Verhalten eines realen Sensors wider. Die Werte und die Bewegung sind vereinfacht. Die Schaltschwellen im SPS-Programm (EBA, PA, PM1, PM2, EBE) müssen an der realen Hardware eingemessen und angepasst werden!\n\n" +
        "-------------------------------------\n\n" +
        "Weitere Simulationsdetails:\n\n" +
        "1. Zylinder M4_1 (Einfahren):\n" +
        "   Wenn der Ausgang A4.2 (M4_1) aktiv ist, wird der Sensor B20 (Position) automatisch auf den Wert 27048 gesetzt.\n\n" +
        "2. Förderband-Rampe:\n" +
        "   Bewegt sich das Förderband nach rechts (Q1 oder Q3 aktiv) und der Sensor B20 erreicht oder überschreitet  600, fährt der Slider automatisch bis zum Minimalwert 0."+
        "3. Diese enstspicht ca.2.00 cm\n";

            MessageBox.Show(infoText, "Simulationshinweise", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Icon auf Warning geändert
        }
    }
}