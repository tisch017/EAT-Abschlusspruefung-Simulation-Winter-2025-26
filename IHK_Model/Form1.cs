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

                // Initialize and start the timer for continuous I/O updates [cite: 1702, 1604]
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
            bool p0_On = plc.OutputArea.ReadBit(0, 4); // A0.4 - P0 [cite: 1047]
            bool p3_On = plc.OutputArea.ReadBit(1, 0); // A1.0 - P3 [cite: 1047]
            bool p4_On = plc.OutputArea.ReadBit(1, 1); // A1.1 - P4 [cite: 1047]
            bool p6_On = plc.OutputArea.ReadBit(1, 2); // A1.2 - P6 [cite: 1047]
            bool p7_On = plc.OutputArea.ReadBit(1, 3); // A1.3 - P7 [cite: 1047]
            bool p8_On = plc.OutputArea.ReadBit(1, 4); // A1.4 - P8 [cite: 1047]
            bool p10_On = plc.OutputArea.ReadBit(1, 5); // A1.5 - P10 [cite: 1047]
            bool p11_On = plc.OutputArea.ReadBit(1, 6); // A1.6 - P11 [cite: 1047]

            // P0 (Betriebsdruck): Blinks if no pressure, solid if pressure is okay [cite: 796, 797]
            bool B0_state = plc.InputArea.ReadBit(4, 0); // E4.0 [cite: 1054]
            P0.BackColor = p0_On ?  Color.White : Color.Gray;

            // P3 (Handbetrieb) & P4 (Automatik): Can blink or be solid [cite: 798, 810]
            S3_P3.BackColor = p3_On ? Color.White : Color.Gray;
            S4_P4.BackColor = p4_On ? Color.White : Color.Gray;

            // P6 (Anlagenstart): Can blink or be solid [cite: 811, 819]
            S6_P6.BackColor = p6_On ? Color.White : Color.Gray;

            

            // Other indicators
            S7_P7.BackColor = p7_On ? Color.White : Color.Gray;
            S8_P8.BackColor = p8_On ? Color.White : Color.Gray;
            P10.BackColor = p10_On ? Color.Yellow : Color.Gray;
            P11.BackColor = p11_On ? Color.White : Color.Gray;
        }

        private void UpdateMotorOutputs()
        {
            // Read motor outputs
            bool q1_On = plc.OutputArea.ReadBit(0, 0); // A0.0 - Band rechts langsam [cite: 1047]
            bool q2_On = plc.OutputArea.ReadBit(0, 1); // A0.1 - Band links langsam [cite: 1047]
            bool q3_On = plc.OutputArea.ReadBit(0, 2); // A0.2 - Band rechts schnell [cite: 1047]

            // Update button colors to show motor status
            Q1.BackColor = q1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            Q2.BackColor = q2_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            Q3.BackColor = q3_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);


            // --- Position des B20-Sensors simulieren ---

            // Lese den aktuellen Wert des Schiebereglers.
            int newValue = B20.Value;

            // Versuche, den Text aus der MotorMove-Textbox in eine Zahl zu konvertieren.
            if (int.TryParse(MotorMove.Text, out int moveSpeed))
            {
                // Passe den Wert an, je nachdem welcher Motor läuft.
                if (q1_On)
                {
                    // Bewegung nach rechts, einfache Geschwindigkeit
                    newValue += moveSpeed;
                }
                if (q2_On)
                {
                    // Bewegung nach links, negative Geschwindigkeit
                    newValue -= moveSpeed;
                }
                if (q3_On)
                {
                    // Bewegung nach rechts, doppelte Geschwindigkeit
                    newValue += (moveSpeed * 2);
                }
            }

            // Stelle sicher, dass der neue Wert innerhalb der Grenzen des Reglers bleibt.
            if (newValue > B20.Maximum)
            {
                newValue = B20.Maximum;
            }
            if (newValue < B20.Minimum)
            {
                newValue = B20.Minimum;
            }

            // Setze den neuen Wert für den Schieberegler, falls er sich geändert hat.
            if (B20.Value != newValue)
            {
                B20.Value = newValue;
            }

        }

        private void UpdateCylinderOutputs()
        {
            // Read cylinder valve outputs
            bool m4_0_On = plc.OutputArea.ReadBit(4, 1); // A4.1 - M4 ausfahren [cite: 1047]
            bool m4_1_On = plc.OutputArea.ReadBit(4, 2); // A4.2 - M4 einfahren [cite: 1047]
            bool m7_0_On = plc.OutputArea.ReadBit(4, 3); // A4.3 - M7 ausfahren [cite: 1047]
            bool m7_1_On = plc.OutputArea.ReadBit(4, 4); // A4.4 - M7 einfahren [cite: 1047]
            bool m10_0_On = plc.OutputArea.ReadBit(4, 5); // A4.5 - M10 ausfahren [cite: 1047]
            bool m10_1_On = plc.OutputArea.ReadBit(4, 6); // A4.6 - M10 einfahren [cite: 1047]

            // Update button colors to show valve status
            M4_0.BackColor = m4_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M4_1.BackColor = m4_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M7_0.BackColor = m7_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M7_1.BackColor = m7_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M10_0.BackColor = m10_0_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
            M10_1.BackColor = m10_1_On ? Color.LimeGreen : Color.FromKnownColor(KnownColor.Control);
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

        private void S2_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 1); // E0.1 [cite: 1054]
        private void S2_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 1);

        private void S3_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 2); // E0.2 [cite: 1054]
        private void S3_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 2);

        private void S4_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 3); // E0.3 [cite: 1054]
        private void S4_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 3);

        private void S5_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 4); // E0.4 [cite: 1054]
        private void S5_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 4);

        private void S6_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 5); // E0.5 [cite: 1054]
        private void S6_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 5);

        private void S7_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 6); // E0.6 [cite: 1054]
        private void S7_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 6);

        private void S8_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(0, 7); // E0.7 [cite: 1054]
        private void S8_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(0, 7);

        private void S10_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(1, 1); // E1.1 [cite: 1054]
        private void S10_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(1, 1);

        private void S13_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(1, 4); // E1.4 [cite: 1054]
        private void S13_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(1, 4);

        // --- Not-Halt ---
        private void NotHalt_CheckedChanged(object sender, EventArgs e)
        {
            // This is a latching switch, not a momentary button
            // Note: The physical NOT-HALT is -S11, but the "Bedienerschutz quittiert" signal is -F9 at E0.0 [cite: 1054]
            // We will simulate the quit button -S12 with the NotHalt GUI element for simplicity.
            if (plc != null) plc.InputArea.WriteBit(0, 0, !NotHalt.Checked); // E0.0 [cite: 1054]

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
        private void K0_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 6, K0.Checked); // E1.6 [cite: 1054]
        private void F1_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 6, !F1.Checked); // E1.6 [cite: 1054]
        private void F2_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(1, 7, !F2.Checked); // E1.7 [cite: 1054]

        private void F9_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(0, 0, F9.Checked); // E1.7 [cite: 1054]
        private void B0_CheckedChanged(object sender, EventArgs e) => plc?.InputArea.WriteBit(4, 0, B0.Checked); // E4.0 [cite: 1054]

        private void B1_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(4, 1); // E4.1 [cite: 1054]
        private void B1_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(4, 1);

        private void B3_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(4, 3); // E4.3 [cite: 1054]
        private void B3_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(4, 3);

        private void B7_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(4, 7); // E4.7 [cite: 1054]
        private void B7_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(4, 7);

        private void B8_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(5, 0); // E5.0 [cite: 1054]
        private void B8_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(5, 0);

        private void B9_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(5, 1); // E5.1 [cite: 1054]
        private void B9_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(5, 1);

        private void B10_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(5, 2); // E5.2 [cite: 1054]
        private void B10_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(5, 2);

        private void B11_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(5, 3); // E5.3 [cite: 1054]
        private void B11_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(5, 3);

        private void B12_MouseDown(object sender, MouseEventArgs e) => HandleMouseDown(5, 4); // E5.4 [cite: 1054]
        private void B12_MouseUp(object sender, MouseEventArgs e) => HandleMouseUp(5, 4);

        #endregion

        private void S12_P12_Click(object sender, EventArgs e)
        {
            if (!NotHalt.Checked) {
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
            // Annahme: Der Name des Schiebereglers ist "B20".
            TrackBar sensorTrackBar = (TrackBar)sender;

            // 1. Den aktuellen Wert des Schiebereglers auslesen.
            //    Wir casten ihn zu 'short', da ein SPS-Wort 16 Bit hat.
            short sensorWert = (short)sensorTrackBar.Value;

            try
            {
                // 2. Den 16-Bit-Wert in ein Array aus 2 Bytes umwandeln.
                byte[] bytesToSend = BitConverter.GetBytes(sensorWert);

                // 3. WICHTIG: Byte-Reihenfolge für die Siemens SPS anpassen (Big-Endian).
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytesToSend);
                }

                // 4. Die 2 Bytes an die Adresse des analogen Eingangsworts senden.
                //    Hier wird %EW8 als Beispiel verwendet (Start-Byte-Adresse 8).
                plc.InputArea.WriteBytes(8, bytesToSend);
            }
            catch (Exception)
            {
                // Fehler beim Schreiben ignorieren.
            }
        }
    }
}