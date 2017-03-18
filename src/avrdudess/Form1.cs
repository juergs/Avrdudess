﻿/*
 * Project: AVRDUDESS - A GUI for AVRDUDE
 * Author: Zak Kemble, contact@zakkemble.co.uk
 * Copyright: (C) 2013 by Zak Kemble
 * License: GNU GPL v3 (see License.txt)
 * Web: http://blog.zakkemble.co.uk/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Windows.Forms;

namespace avrdudess
{
    public partial class Form1 : Form
    {
        private const string WEB_ADDR_FUSE_SETTINGS = "http://www.engbedded.com/fusecalc";

        private ToolTip ToolTips;
        private Avrdude avrdude;
        private Avrsize avrsize;
        private Presets presets;
        private CmdLine cmdLine;
        public bool ready               = false;
        private string flashOperation   = "w";
        private string EEPROMOperation  = "w";
        private string presetToLoad;
        private bool drag               = false;
        private Point dragStart;
        private string oldBitClock;
        private Avrdude.UsbAspFreq oldUsbAspFreq;
        private MemTypeFile fileEEPROM;
        private MemTypeFile fileFlash;

        #region Control getters and setters

        public Programmer prog
        {
            get
            {
                Programmer item = ((Programmer)cmbProg.SelectedItem);
                if (item == null || item.name == "")
                    return null;
                return item;
            }
            set
            {
                Programmer p = ((List<Programmer>)avrdude.programmers).Find(s => s.name == value.name);
                if (p != null)
                    cmbProg.SelectedItem = p;
                else
                    MsgBox.warning("Programmer not found (" + value.name + ")");
            }
        }
        
        public MCU mcu
        {
            get
            {
                MCU item = ((MCU)cmbMCU.SelectedItem);
                if (item == null || item.name == "")
                    return null;
                return item;
            }
            set
            {
                MCU m = avrdude.mcus.Find(s => s.name == value.name);
                if (m != null)
                    cmbMCU.SelectedItem = m;
                else
                    MsgBox.warning("MCU not found (" + value.name + ")");
            }
        }

        public string port
        {
            get { return cmbPort.Text.Trim(); }
            set { cmbPort.Text = value; }
        }

        public string baudRate
        {
            get { return txtBaudRate.Text.Trim(); }
            set { txtBaudRate.Text = value; }
        }

        public string bitClock
        {
            get { return txtBitClock.Text.Trim(); }
            set { txtBitClock.Text = value; }
        }

        public bool force
        {
            get { return cbForce.Checked; }
            set { cbForce.Checked = value; }
        }

        public bool disableVerify
        {
            get { return cbNoVerify.Checked; }
            set { cbNoVerify.Checked = value; }
        }

        public bool disableFlashErase
        {
            get { return cbDisableFlashErase.Checked; }
            set { cbDisableFlashErase.Checked = value; }
        }

        public bool eraseFlashAndEEPROM
        {
            get { return cbEraseFlashEEPROM.Checked; }
            set { cbEraseFlashEEPROM.Checked = value; }
        }

        public bool doNotWrite
        {
            get { return cbDoNotWrite.Checked; }
            set { cbDoNotWrite.Checked = value; }
        }

        public string cmdBox
        {
            set { txtCmdLine.Text = value; }
        }

        public string flashFile
        {
            get { return txtFlashFile.Text.Trim(); }
            set { txtFlashFile.Text = value; }
        }

        public string flashFileFormat
        {
            get { return ((FileFormat)cmbFlashFormat.SelectedItem).name; }
            set
            {
                FileFormat f = Avrdude.fileFormats.Find(s => s.name == value);
                if (f != null)
                    cmbFlashFormat.SelectedItem = f;
            }
        }

        public string flashFileOperation
        {
            get { return flashOperation; }
            set
            {
                if (value == "w")
                    rbFlashOpWrite.Checked = true;
                else if (value == "r")
                    rbFlashOpRead.Checked = true;
                else
                    rbFlashOpVerify.Checked = true;
            }
        }

        public string EEPROMFile
        {
            get { return txtEEPROMFile.Text.Trim(); }
            set { txtEEPROMFile.Text = value; }
        }

        public string EEPROMFileFormat
        {
            get { return ((FileFormat)cmbEEPROMFormat.SelectedItem).name; }
            set
            {
                FileFormat f = Avrdude.fileFormats.Find(s => s.name == value);
                if (f != null)
                    cmbEEPROMFormat.SelectedItem = f;
            }
        }

        public string EEPROMFileOperation
        {
            get { return EEPROMOperation; }
            set
            {
                if (value == "w")
                    rbEEPROMOpWrite.Checked = true;
                else if (value == "r")
                    rbEEPROMOpRead.Checked = true;
                else
                    rbEEPROMOpVerify.Checked = true;
            }
        }

        public bool setFuses
        {
            get { return cbSetFuses.Checked; }
            set { cbSetFuses.Checked = value; }
        }

        public string highFuse
        {
            get { return txtHFuse.Text; }
            set { txtHFuse.Text = value; }
        }

        public string lowFuse
        {
            get { return txtLFuse.Text; }
            set { txtLFuse.Text = value; }
        }

        public string exFuse
        {
            get { return txtEFuse.Text; }
            set { txtEFuse.Text = value; }
        }

        public bool setLock
        {
            get { return cbSetLock.Checked; }
            set { cbSetLock.Checked = value; }
        }

        public string lockSetting
        {
            get { return txtLock.Text; }
            set { txtLock.Text = value; }
        }

        public string additionalSettings
        {
            get { return txtAdditional.Text; }
            set { txtAdditional.Text = value; }
        }

        public byte verbosity
        {
            get { return (byte)cmdVerbose.SelectedItem; }
            set { cmdVerbose.SelectedItem = value; }
        }

        #endregion

        public Form1(string[] args)
        {
            InitializeComponent();

            if (args.Length > 0)
                presetToLoad = args[0];

            Icon = AssemblyData.icon;
            setWindowTitle();

            // Make sure console is the right size
            Form1_Resize(this, null);

            MaximumSize = new Size(Size.Width, int.MaxValue);
            MinimumSize = new Size(Size.Width, Height - txtConsole.Height);

            Util.UI = this;
            Util.consoleSet(txtConsole);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Load saved configuration
            Config.Prop.load();

            // Persist window location across sessions
            // Credits:
            // gl.tter
            if (Config.Prop.windowLocation != null && Config.Prop.windowLocation != new Point(0, 0))
                Location = Config.Prop.windowLocation;

            cmdLine = new CmdLine(this);
            avrdude = new Avrdude();
            avrsize = new Avrsize();

            avrdude.OnProcessStart += avrdude_OnProcessStart;
            avrdude.OnProcessEnd += avrdude_OnProcessEnd;
            avrdude.OnVersionChange += avrdude_OnVersionChange;
            avrdude.OnDetectedMCU += avrdude_OnDetectedMCU;

            avrdude.load();
            avrsize.load();

            // Setup memory files/usage bars
            // Flash
            fileFlash = new MemTypeFile(txtFlashFile, avrsize);
            fileFlash.sizeChanged += fileFlash_sizeChanged;
            pbFlashUsage.Width = txtFlashFile.Width;
            pbFlashUsage.Height = 3;
            pbFlashUsage.Location = new Point(txtFlashFile.Location.X, txtFlashFile.Location.Y - pbFlashUsage.Height);
            pbFlashUsage.Image = new Bitmap(pbFlashUsage.Width, pbFlashUsage.Height);
            memoryUsageBar(fileFlash, pbFlashUsage, 0);

            // EEPROM
            fileEEPROM = new MemTypeFile(txtEEPROMFile, avrsize);
            fileEEPROM.sizeChanged += fileEEPROM_sizeChanged;
            pbEEPROMUsage.Width = txtEEPROMFile.Width;
            pbEEPROMUsage.Height = 3;
            pbEEPROMUsage.Location = new Point(txtEEPROMFile.Location.X, txtEEPROMFile.Location.Y - pbEEPROMUsage.Height);
            pbEEPROMUsage.Image = new Bitmap(pbEEPROMUsage.Width, pbEEPROMUsage.Height);
            memoryUsageBar(fileEEPROM, pbEEPROMUsage, 0);

            enableClientAreaDrag(Controls);

            // Update serial ports etc
            cmbPort.DropDown += cbPort_DropDown;

            // Drag and drop flash file
            gbFlashFile.AllowDrop = true;
            gbFlashFile.DragEnter += event_DragEnter;
            gbFlashFile.DragDrop += event_DragDrop;

            // Drag and drop EEPROM file
            gbEEPROMFile.AllowDrop = true;
            gbEEPROMFile.DragEnter += event_DragEnter;
            gbEEPROMFile.DragDrop += event_DragDrop;

            // Flash file
            openFileDialog1.Filter = "Hex files (*.hex)|*.hex";
            openFileDialog1.Filter += "|EEPROM files (*.eep)|*.eep";
            openFileDialog1.Filter += "|All files (*.*)|*.*";
            openFileDialog1.CheckFileExists = false;
            openFileDialog1.FileName = "";
            openFileDialog1.Title = "Open flash file";

            // EEPROM file
            openFileDialog2.Filter = "EEPROM files (*.eep)|*.eep";
            openFileDialog2.Filter += "|Hex files (*.hex)|*.hex";
            openFileDialog2.Filter += "|All files (*.*)|*.*";
            openFileDialog2.CheckFileExists = false;
            openFileDialog2.FileName = "";
            openFileDialog2.Title = "Open EEPROM file";

            // MCU & programmer combo box data source
            setComboBoxDataSource(cmbMCU, avrdude.mcus, "fullName");
            cmbMCU.SelectedIndexChanged += cmbMCU_SelectedIndexChanged;
            setComboBoxDataSource(cmbProg, avrdude.programmers, "fullName");
            cmbProg.SelectedIndexChanged += cmbProg_SelectedIndexChanged;
            
            // USBasp frequency settings
            cmbUSBaspFreq.Hide();
            setComboBoxDataSource(cmbUSBaspFreq, Avrdude.USBaspFreqs, "name");
            cmbUSBaspFreq.Width = txtBitClock.Width;
            cmbUSBaspFreq.Left = txtBitClock.Left;
            cmbUSBaspFreq.Top = txtBitClock.Top;

            // Flash & EEPROM file formats
            setComboBoxDataSource(cmbFlashFormat, Avrdude.fileFormats, "fullName");
            setComboBoxDataSource(cmbEEPROMFormat, Avrdude.fileFormats, "fullName");

            // Verbosity levels
            cmdVerbose.Items.Clear();
            for (byte i=0;i<5;i++)
                cmdVerbose.Items.Add(i);
            cmdVerbose.SelectedIndex = 0;

            // Tool tips
            ToolTips = new ToolTip();
            ToolTips.ReshowDelay = 100;
            ToolTips.UseAnimation = false;
            ToolTips.UseFading = false;
            ToolTips.SetToolTip(cmbProg, "Programmer");
            ToolTips.SetToolTip(cmbMCU, "MCU to program");
            ToolTips.SetToolTip(cmbPort, "Set COM/LTP/USB port");
            ToolTips.SetToolTip(txtBaudRate, "Port baud rate");
            ToolTips.SetToolTip(txtBitClock, "Bit clock period (us)");
            ToolTips.SetToolTip(txtFlashFile, "Hex file (.hex)" + Environment.NewLine + "You can also drag and drop files here");
            ToolTips.SetToolTip(pFlashOp, "");
            ToolTips.SetToolTip(txtEEPROMFile, "EEPROM file (.eep)" + Environment.NewLine + "You can also drag and drop files here");
            ToolTips.SetToolTip(pEEPROMOp, "");
            ToolTips.SetToolTip(cbForce, "Skip signature check");
            ToolTips.SetToolTip(cbNoVerify, "Don't verify after writing");
            ToolTips.SetToolTip(cbDisableFlashErase, "Don't erase flash before writing" + Environment.NewLine + "Use this if you only want to update EEPROM");
            ToolTips.SetToolTip(cbEraseFlashEEPROM, "Erase both flash and EEPROM");
            ToolTips.SetToolTip(cbDoNotWrite, "Don't write anything, used for debugging AVRDUDE");
            ToolTips.SetToolTip(txtLFuse, "Low fuse");
            ToolTips.SetToolTip(txtHFuse, "High fuse");
            ToolTips.SetToolTip(txtEFuse, "Extended fuse");
            ToolTips.SetToolTip(txtLock, "Lock bits");
            ToolTips.SetToolTip(btnFlashGo, "Only write/read/verify flash");
            ToolTips.SetToolTip(btnEEPROMGo, "Only write/read/verify EEPROM");
            ToolTips.SetToolTip(btnWriteFuses, "Write fuses now");
            ToolTips.SetToolTip(btnWriteLock, "Write lock now");
            ToolTips.SetToolTip(btnReadFuses, "Read fuses now");
            ToolTips.SetToolTip(btnReadLock, "Read lock now");
            ToolTips.SetToolTip(cbSetFuses, "Write fuses when programming");
            ToolTips.SetToolTip(cbSetLock, "Write lock when programming");

            // Load saved presets
            presets = new Presets(this);
            presets.load();
            presets.setDataSource(cmbPresets);

            // Enable/disable tool tips based on saved config
            ToolTips.Active = Config.Prop.toolTips;

            ready = true;

            // If a preset has not been specified by the command line then use the last used preset
            // Credits:
            // Uwe Tanger (specifing preset in command line)
            // neptune (sticky presets)
            if (presetToLoad == null)
                presetToLoad = Config.Prop.preset;

            // Load preset
            PresetData p = presets.presets.Find(s => s.name == presetToLoad);
            cmbPresets.SelectedItem = (p != null) ? p : presets.presets.Find(s => s.name == "Default");

            // Check for updates
            UpdateCheck.check.checkNow();
        }

        // Show AVRDUDE version etc
        private void setWindowTitle()
        {
            string avrdudeVersion = (avrdude != null) ? avrdude.version : "";
            if (avrdudeVersion == "")
                avrdudeVersion = "?";
            Text = String.Format("{0} {1}.{2} ({3})", AssemblyData.title, AssemblyData.version.Major, AssemblyData.version.Minor, avrdudeVersion);
        }

        // Set combo box data source etc
        private void setComboBoxDataSource(ComboBox cmb, object src, string displayMember)
        {
            cmb.DataSource = null;
            cmb.ValueMember = null;
            cmb.DataSource = new BindingSource(src, null);
            cmb.DisplayMember = displayMember;
            if(cmb.Items.Count > 0)
                cmb.SelectedIndex = 0;
        }

        // Flash size changed, update usage bar
        private void fileFlash_sizeChanged(object sender, EventArgs e)
        {
            if (mcu != null)
                memoryUsageBar(fileFlash, pbFlashUsage, mcu.flash);
        }

        // EEPROM size changed, update usage bar
        private void fileEEPROM_sizeChanged(object sender, EventArgs e)
        {
            if (mcu != null)
                memoryUsageBar(fileEEPROM, pbEEPROMUsage, mcu.eeprom);
        }
        
        // Click and drag (almost) anywhere to move window
        private void enableClientAreaDrag(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is GroupBox || c is Label || c is PictureBox)
                {
                    c.MouseDown += Form1_MouseDown;
                    c.MouseUp   += Form1_MouseUp;
                    c.MouseMove += Form1_MouseMove;
                    enableClientAreaDrag(c.Controls);
                }
            }
        }

        // Read and display file contents
        // TODO: Move to avrdude.cs
        private void readFuseFiles(object param)
        {
            string[] fuseFiles = (string[])param;
            TextBox[] boxes = { txtLFuse, txtHFuse, txtEFuse };

            for (byte i = 0; i < fuseFiles.Length; i++)
            {
                // Credits:
                // Simone Chifari (output formatting)
                string fuse = "";
                if (File.Exists(fuseFiles[i]))
                {
                    fuse = "0x" + File.ReadAllText(fuseFiles[i]).Trim().ToUpper().Replace("0X", "").PadLeft(2, '0');
                    File.Delete(fuseFiles[i]);
                }

                Invoke(new MethodInvoker(() =>
                {
                    boxes[i].Text = fuse;
                }));
            }
        }

        // Read and display file contents
        // TODO: Move to avrdude.cs
        private void readLockFile(object param)
        {
            string lockFile = (string)param;

            // Credits:
            // Simone Chifari (output formatting)
            string fuse = "";
            if (File.Exists(lockFile))
            {
                fuse = "0x" + File.ReadAllText(lockFile).Trim().ToUpper().Replace("0X", "").PadLeft(2, '0');
                File.Delete(lockFile);
            }

            Invoke(new MethodInvoker(() =>
            {
                txtLock.Text = fuse;
            }));
        }

        // Draw usage bar and show info in console
        private void memoryUsageBar(MemTypeFile file, PictureBox pic, int availableSpace)
        {
            bool outOfSpace = file.size > availableSpace;

            // Info
            if (file.size != Avrsize.INVALID && file.location != "")
            {
                float perc = 0;
                if (availableSpace > 0)
                    perc = ((float)file.size / availableSpace) * 100;
                string fmt = "{0}: {1:#,#0} / {2:#,#0} Bytes ({3:0.00}%){4}{5}";
                string outOfSpaceStr = outOfSpace ? " [!]" : "";
                Util.consoleWrite(String.Format(fmt, Path.GetFileName(file.location), file.size, availableSpace, perc, outOfSpaceStr, Environment.NewLine));
            }

            Bitmap bmp = (Bitmap)pic.Image;

            int usageWidth;
            Color barColour = Color.Red;

            if (outOfSpace)
                usageWidth = bmp.Width;
            else if (availableSpace > 0 && file.size != Avrsize.INVALID)
                usageWidth = (int)(bmp.Width * ((float)file.size / availableSpace));
            else
                usageWidth = 0;

            // Blue gradient thing
            byte startColour = 128;
            byte endColour = 192;
            float colourDiff = (float)(endColour - startColour) / usageWidth;

            // Fill in used space pixels
            for (int i = 0; i < usageWidth; i++)
            {
                if (!outOfSpace)
                    barColour = Color.FromArgb(0x00, startColour + (byte)(colourDiff * i), 0xFF);

                for (byte h = 0; h < bmp.Height; h++)
                    bmp.SetPixel(i, h, barColour);
            }

            // Fill in available space pixels
            for (int i = usageWidth; i < bmp.Width; i++)
            {
                for (byte h = 0; h < bmp.Height; h++)
                    bmp.SetPixel(i, h, Color.Transparent);
            }

            pic.Image = bmp;
        }

        // AVRDUDE process has started
        private void avrdude_OnProcessStart(object sender, EventArgs e)
        {
            tssStatus.Text = "AVRDUDE is running...";
        }

        // AVRDUDE process has ended
        private void avrdude_OnProcessEnd(object sender, EventArgs e)
        {
            tssStatus.Text = "Ready";
        }

        // Found MCU
        private void avrdude_OnDetectedMCU(object sender, DetectedMCUEventArgs e)
        {
            if (e.mcu != null)
            {
                Util.consoleWrite("Detected " + e.mcu.signature + " = " + e.mcu.fullName + Environment.NewLine);
                Invoke(new MethodInvoker(() =>
                {
                    // Select the MCU that was found
                    cmbMCU.SelectedItem = e.mcu;
                }));
            }
            else
            {
                // Failed to detect MCU, show log so we can see what went wrong
                Util.consoleWrite("Unable to detect MCU" + Environment.NewLine);
                Util.consoleWrite(Environment.NewLine + avrdude.log + Environment.NewLine);
            }
        }

        // Version change
        private void avrdude_OnVersionChange(object sender, EventArgs e)
        {
            setWindowTitle();
        }

        // Disable buttons if a programmer or MCU hasn't been selected
        private void enableControls()
        {
            bool progOK = (prog != null);

            btnDetect.Enabled = progOK;

            bool enable = (mcu != null && progOK);
            btnFuseSelector.Enabled = enable;
            btnWriteFuses.Enabled = enable;
            btnReadFuses.Enabled = enable;
            btnWriteLock.Enabled = enable;
            btnReadLock.Enabled = enable;
            btnProgram.Enabled = enable;
            btnFlashGo.Enabled = enable;
            btnEEPROMGo.Enabled = enable;
        }

        #region UI Events

        // Drag and drop
        private void event_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        // Drag and drop
        private void event_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (((GroupBox)sender).Name == "gbFlashFile")
                txtFlashFile.Text = files[0];
            else
                txtEEPROMFile.Text = files[0];
        }

        // Port drop down, refresh available ports
        private void cbPort_DropDown(object sender, EventArgs e)
        {
            cmbPort.Items.Clear();

            PlatformID os = Environment.OSVersion.Platform;
            if (os == PlatformID.Unix || os == PlatformID.MacOSX)
            {
                string[] devPrefixs = new string[]
                {
                    "ttyS", // Normal serial port
                    "ttyUSB", // USB <-> serial converter
                    "ttyACM", // USB <-> serial converter (usually an Arduino)
                    "lp" // Parallel port
                };

                // http://stackoverflow.com/questions/434494/serial-port-rs232-in-mono-for-multiple-platforms
                string[] devs;
                try
                {
                    devs = Directory.GetFiles("/dev/", "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    return;
                }

                Array.Sort(devs);

                // Loop through each device
                foreach (string dev in devs)
                {
                    // See if device starts with one of the prefixes
                    foreach (string prefix in devPrefixs)
                    {
                        if (dev.StartsWith("/dev/" + prefix))
                        {
                            cmbPort.Items.Add(dev);
                            break;
                        }
                    }
                }
            }
            else // Windows
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string p in ports)
                    cmbPort.Items.Add(p);

                cmbPort.Items.Add("usb");
                cmbPort.Items.Add("LPT1");
                cmbPort.Items.Add("LPT2");
                cmbPort.Items.Add("LPT3");
            }
        }

        // General event for when a control changes
        private void event_controlChanged(object sender, EventArgs e)
        {
            cmdLine.generate();
            enableControls();
        }

        // Programmer choice changed
        private void cmbProg_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Credits:
            // Simone Chifari (USBasp frequency stuff)

            // Hide/show USBasp frequency/bit clock boxes

            if (prog != null && prog.name == "usbasp") // USBasp has been selected
            {
                if (txtBitClock.Visible)
                {
                    // Store bit clock
                    oldBitClock = txtBitClock.Text;

                    // Show/hide stuff
                    txtBitClock.Hide();
                    cmbUSBaspFreq.Show();

                    // Make sure a selected index changed event occurs
                    cmbUSBaspFreq.SelectedIndex = -1;

                    // Restore USBasp frequency
                    if (oldUsbAspFreq != null)
                        cmbUSBaspFreq.SelectedItem = oldUsbAspFreq;
                    else
                        cmbUSBaspFreq.SelectedIndex = 0;
                }
            }
            else
            {
                if (!txtBitClock.Visible)
                {
                    // Store selected USBasp frequency
                    oldUsbAspFreq = ((Avrdude.UsbAspFreq)cmbUSBaspFreq.SelectedItem);

                    // Restore bit clock
                    txtBitClock.Text = oldBitClock;

                    // Show/hide stuff
                    txtBitClock.Show();
                    cmbUSBaspFreq.Hide();
                }
            }
        }

        // MCU choice changed
        private void cmbMCU_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update flash and EEPROM size info
            if (mcu != null)
            {
                lblFlashSize.Text = Util.fileSizeFormat(mcu.flash);
                lblEEPROMSize.Text = Util.fileSizeFormat(mcu.eeprom);
                memoryUsageBar(fileFlash, pbFlashUsage, mcu.flash);
                memoryUsageBar(fileEEPROM, pbEEPROMUsage, mcu.eeprom);
            }
            else
            {
                lblFlashSize.Text = "-";
                lblEEPROMSize.Text = "-";
            }
        }

        // Flash & EEPROM operation radio buttons
        private void radioButton_flashEEPROMOp_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                string op;
                if (radioButton.Text == "Write")
                    op = "w";
                else if (radioButton.Text == "Read")
                    op = "r";
                else
                    op = "v";

                if (radioButton.Parent.Name == "pFlashOp")
                    flashOperation = op;
                else
                    EEPROMOperation = op;

                cmdLine.generate();
            }
        }

        // Browse for flash file
        private void btnFlashBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                txtFlashFile.Text = openFileDialog1.FileName;
        }

        // Browse for EEPROM file
        private void btnEEPROMBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
                txtEEPROMFile.Text = openFileDialog2.FileName;
        }

        // Options
        private void btnOptions_Click(object sender, EventArgs e)
        {
            FormOptions fOptions = new FormOptions();
            fOptions.toolTips = Config.Prop.toolTips;
            fOptions.avrdudeLocation = Config.Prop.avrdudeLoc;
            fOptions.avrdudeConfLocation = Config.Prop.avrdudeConfLoc;
            fOptions.avrSizeLocation = Config.Prop.avrSizeLoc;

            if (fOptions.ShowDialog() != DialogResult.OK)
                return;

            Config.Prop.toolTips = fOptions.toolTips;
            ToolTips.Active = Config.Prop.toolTips;

            bool changedAvrdudeLoc = (Config.Prop.avrdudeLoc != fOptions.avrdudeLocation);
            bool changedAvrdudeConfLoc = (Config.Prop.avrdudeConfLoc != fOptions.avrdudeConfLocation);
            bool changedAvrSizeLoc = (Config.Prop.avrSizeLoc != fOptions.avrSizeLocation);

            Config.Prop.avrdudeLoc = fOptions.avrdudeLocation;
            Config.Prop.avrdudeConfLoc = fOptions.avrdudeConfLocation;
            Config.Prop.avrSizeLoc = fOptions.avrSizeLocation;

            if (changedAvrdudeLoc || changedAvrdudeConfLoc)
            {
                avrdude.load();

                if (changedAvrdudeConfLoc)
                {
                    setComboBoxDataSource(cmbMCU, avrdude.mcus, "fullName");
                    setComboBoxDataSource(cmbProg, avrdude.programmers, "fullName");
                }
            }

            if (changedAvrSizeLoc)
            {
                avrsize.load();
                fileFlash.updateSize();
                fileEEPROM.updateSize();
            }
        }

        // Only allow digits and delete
        // This isn't perfect, but its close enough
        private void txtNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txtBox = ((TextBox)sender);
            if (e.KeyChar != 8 && (Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                if ((!Char.IsDigit(e.KeyChar) && e.KeyChar != '.') || (txtBox.SelectionLength == 0 && txtBox.Text.Length >= 10))
                {
                    e.Handled = true;
                    SystemSounds.Beep.Play();
                }
            }
        }

        // Only allow hex digits and delete
        // This isn't perfect, but its close enough
        private void txtHex_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txtBox = ((TextBox)sender);
            if (e.KeyChar != 8 && (Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                if ((!Char.IsDigit(e.KeyChar) && !"ABCDEFX".Contains(Char.ToUpper(e.KeyChar).ToString())) || (txtBox.SelectionLength == 0 && txtBox.Text.Length >= 4))
                {
                    e.Handled = true;
                    SystemSounds.Beep.Play();
                }
            }
        }

        // Program!
        private void btnProgram_Click(object sender, EventArgs e)
        {
            avrdude.launch(txtCmdLine.Text);
        }

        // Force stop
        private void btnForceStop_Click(object sender, EventArgs e)
        {
            if(avrdude.kill())
                Util.consoleWrite(Environment.NewLine + "AVRDUDE killed" + Environment.NewLine);
        }

        // Save a preset
        private void btnPresetSave_Click(object sender, EventArgs e)
        {
            // Credits:
            // Uwe Tanger (Set preset name by typing directly into presets box instead of a popup window)

            // Check name
            string name = cmbPresets.Text;
            if (name.Length < 1)
                return;
            else if (name == "Default")
            {
                MsgBox.notice("Can't change 'Default'");
                return;
            }

            // Remove old preset with same name
            PresetData p = presets.presets.Find(s => s.name == name);
            if (p != null)
                presets.remove(p);

            // Add new preset
            presets.add(name);
            presets.save();
            presets.setDataSource(cmbPresets, cmbPresets_SelectedIndexChanged);

            // Select the new preset
            p = presets.presets.Find(s => s.name == name);
            if (p != null)
                cmbPresets.SelectedItem = (object)p;
        }

        // Delete a preset
        private void btnPresetDelete_Click(object sender, EventArgs e)
        {
            // Credits:
            // Uwe Tanger (Delete selected preset in the presets box instead of a popup window)

            // Make sure a preset is selected
            if (cmbPresets.SelectedItem != null)
            {
                // Make sure its not the default preset
                if (((PresetData)cmbPresets.SelectedItem).name == "Default")
                {
                    MsgBox.notice("Can't remove 'Default'");
                    return;
                }

                // Confirm preset deletion (too easy to accidentally delete)
                // Credits:
                // gl.tter
                if (MsgBox.confirm("Delete preset '" + ((PresetData)cmbPresets.SelectedItem).name + "'?") == DialogResult.OK)
                {
                    // Remove the preset
                    presets.remove((PresetData)cmbPresets.SelectedItem);
                    presets.save();
                    presets.setDataSource(cmbPresets, cmbPresets_SelectedIndexChanged);

                    // Load up default
                    cmbPresets.SelectedItem = presets.presets.Find(s => s.name == "Default");
                }
            }
        }

        // Preset choice changed
        private void cmbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ready)
            {
                var item = (PresetData)cmbPresets.SelectedItem;
                if (item != null)
                {
                    item.load(this);
                    Config.Prop.preset = item.name;

                    // If preset uses USBasp we need to workout the frequency to use from the bit clock
                    if (prog != null && prog.name == "usbasp")
                    {
                        string bitClockStr = txtBitClock.Text;

                        // Make sure an index changed event occurs
                        cmbUSBaspFreq.SelectedIndex = -1;

                        // Convert bit clock to frequency

                        // String to double
                        double bitClock;
                        if (!double.TryParse(bitClockStr,
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture, // Bit clock is saved with a '.', we don't want it to try and parse it expecting a ',' or something else
                            out bitClock))
                        {
                            cmbUSBaspFreq.SelectedIndex = 0;
                            return;
                        }

                        int freq = (int)(1 / (bitClock * 0.000001));

                        // Make sure frequency is between min and max
                        if (freq > Avrdude.USBaspFreqs[0].freq)
                            freq = Avrdude.USBaspFreqs[0].freq;
                        else if (freq < Avrdude.USBaspFreqs[Avrdude.USBaspFreqs.Count - 1].freq)
                            freq = Avrdude.USBaspFreqs[Avrdude.USBaspFreqs.Count - 1].freq;

                        // Show frequency
                        cmbUSBaspFreq.SelectedItem = Avrdude.USBaspFreqs.Find(s => freq >= s.freq - 1);
                    }
                }
            }
        }

        // Fuse link clicked
        // Credits:
        // buttim (Load up selected MCU and fuses when opening the fuse calc web page)
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string sParam;
            string sURL = WEB_ADDR_FUSE_SETTINGS;

            if (cmbMCU.SelectedIndex != 0)
            {
                sParam = "?P=" + cmbMCU.Text;

                if (txtLFuse.Text != "")
                    sParam += "&V_LOW=" + txtLFuse.Text;

                if (txtHFuse.Text != "")
                    sParam += "&V_HIGH=" + txtHFuse.Text;

                if (txtEFuse.Text != "")
                    sParam += "&V_EXTENDED=" + txtEFuse.Text;

                if (sParam != "" || txtHFuse.Text != "" || txtEFuse.Text != "")
                    sParam += "&O_HEX=Apply+values";

                sURL += sParam;
            }

            System.Diagnostics.Process.Start(sURL);
        }

        // Read fuses
        private void btnReadFuses_Click(object sender, EventArgs e)
        {
            // Generate file names to use for saving fuses values to
            string[] fuseFiles = new string[3];
            for (byte i = 0; i < fuseFiles.Length; i++)
                fuseFiles[i] = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Guid.NewGuid().ToString(), ".TMP"));

            // Get values
            string cmd = cmdLine.generateReadFuses(fuseFiles[0], fuseFiles[1], fuseFiles[2]);
            avrdude.launch(cmd, readFuseFiles, fuseFiles);
        }

        // Read lock byte
        private void btnReadLock_Click(object sender, EventArgs e)
        {
            // Generate file name to use for saving lock value to
            string lockFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Guid.NewGuid().ToString(), ".TMP"));

            // Get value
            string cmd = cmdLine.generateReadLock(lockFile);
            avrdude.launch(cmd, readLockFile, lockFile);
        }

        // Only write fuses
        // Credits:
        // Dean (Option to only set fuses)
        private void btnWriteFuses_Click(object sender, EventArgs e)
        {
            avrdude.launch(cmdLine.generateWriteFuses());
        }

        // Only write lock
        private void btnWriteLock_Click(object sender, EventArgs e)
        {
            avrdude.launch(cmdLine.generateWriteLock());
        }

        // Only read/write/varify flash
        private void btnFlashGo_Click(object sender, EventArgs e)
        {
            avrdude.launch(cmdLine.generateFlash());
        }

        // Only read/write/varify EEPROM
        private void btnEEPROMGo_Click(object sender, EventArgs e)
        {
            avrdude.launch(cmdLine.generateEEPROM());
        }

        // Detect MCU
        // Credits:
        // Simone Chifari (Auto detect MCU)
        private void btnDetect_Click(object sender, EventArgs e)
        {
            avrdude.detectMCU(cmdLine.genReadSig());
        }

        // Open fuse selector window
        // Credits:
        // Simone Chifari (Fuse selector)
        private void btnFuseSelector_Click(object sender, EventArgs e)
        {
            // Make sure MCU is valid
            if (mcu == null)
                return;

            // Get fuse values
            string[] fuses = { txtLFuse.Text, txtHFuse.Text, txtEFuse.Text, txtLock.Text };

            // Remove 0x
            for (int i = 0; i < fuses.Length; i++)
                fuses[i] = fuses[i].ToLower().Replace("0x", "");

            // Open fuse selector form
            FormFuseSelector f = new FormFuseSelector();
            string[] newFuses = f.editFuseAndLocks(mcu, fuses);

            if (newFuses != null)
            {
                // Add 0x back on
                for (int i = 0; i < newFuses.Length; i++)
                    newFuses[i] = "0x" + newFuses[i];

                // Set fuse values
                txtLFuse.Text = newFuses[0];
                txtHFuse.Text = newFuses[1];
                txtEFuse.Text = newFuses[2];
                txtLock.Text = newFuses[3];
            }
        }

        // Workout bit clock for USBasp programmer frequency
        private void cmbUSBaspFreq_SelectedIndexChanged(object sender, EventArgs e)
        {
            Avrdude.UsbAspFreq freq = ((Avrdude.UsbAspFreq)((ComboBox)sender).SelectedItem);
            if (cmbUSBaspFreq.Visible && freq != null)
                txtBitClock.Text = freq.bitClock;
        }

        // About
        private void btnAbout_Click(object sender, EventArgs e)
        {
            string about = "";
            about += AssemblyData.title + Environment.NewLine;
            about += "Version " + AssemblyData.version.ToString() + Environment.NewLine;
            about += AssemblyData.copyright + Environment.NewLine;
            about += Environment.NewLine;
            about += "zakkemble.co.uk";

            MessageBox.Show(about, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Resize console when form resizes
        private void Form1_Resize(object sender, EventArgs e)
        {
            txtConsole.Height = Height - txtConsole.Top - 64;
        }

        // Drag client area
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;

            Point screenPos = PointToScreen(new Point(0, 0));

            dragStart = new Point(e.X + (screenPos.X - Location.X), e.Y + (screenPos.Y - Location.Y));

            Control c = (Control)sender;
            while (c is GroupBox || c is Label || c is PictureBox)
            {
                dragStart.X += c.Location.X;
                dragStart.Y += c.Location.Y;
                c = c.Parent;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
                Location = new Point(Cursor.Position.X - dragStart.X, Cursor.Position.Y - dragStart.Y);
        }

        // CTRL + A to select all
        private void txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        // Tool item select all
        private void tsmiSelectAll_Click(object sender, EventArgs e)
        {
            txtConsole.SelectAll();
        }

        // Tool item copy
        private void tsmiCopy_Click(object sender, EventArgs e)
        {
            if (txtConsole.SelectionLength > 0)
                Clipboard.SetText(txtConsole.SelectedText);
        }

        // Tool item clear
        private void tsmiClear_Click(object sender, EventArgs e)
        {
            Util.consoleClear();
        }

        // Save configuration when closing
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Persist window location across sessions
            // Credits:
            // gl.tter
            if (WindowState != FormWindowState.Minimized)
                Config.Prop.windowLocation = Location;

            Config.Prop.save();
        }

        #endregion
    }
}
