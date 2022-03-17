using NCcom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace PanelGRBL
{
    public partial class GUI : Form
    {
        Controller Controller;
        int DockContentId = 0;

        public GUI()
        {
            InitializeComponent();

            DockPanel DockPanel = new DockPanel();
            DockPanel.Theme = new VS2015LightTheme();
            DockPanel.DocumentStyle = DocumentStyle.DockingSdi;
            DockPanel.Dock = DockStyle.Fill;
            this.Controls.Add(DockPanel);

            string[] ports = SerialPort.GetPortNames();
            if (ports.Any())
            {
                if (ports.Contains(Properties.Settings.Default.Port))
                {
                    Controller = new GRBL(Properties.Settings.Default.Port, 115200);
                }
                else
                {
                    Controller = new GRBL(ports[0], 115200);
                }
            }
            else
            {
                Controller = new GRBL("COM1", 115200);
            }

            DockContent Main = new DockContent();
            Main.DockAreas = DockAreas.Document;
            Main.Name = "MAIN";
            Main.Text = "MAIN";
            Main.FormClosing += new FormClosingEventHandler((s, ea) => { ea.Cancel = true; });
            MainInterfaceGRBL(Main);

            DockContent Settings = new DockContent();
            Settings.DockAreas = DockAreas.DockBottom | DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop;
            Settings.Name = "SETTINGS";
            Settings.Text = "SETTINGS";
            Settings.FormClosing += new FormClosingEventHandler((s, ea) => { ea.Cancel = true; });
            SettingsGRBL(Settings);

            DockContent Console = new DockContent();
            Console.DockAreas = DockAreas.DockBottom | DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop;
            Console.Name = "CONSOLE";
            Console.Text = "CONSOLE";
            Console.FormClosing += new FormClosingEventHandler((s, ea) => { ea.Cancel = true; });
            ConsoleGRBL(Console);

            DockContent Pad = new DockContent();
            Pad.DockAreas = DockAreas.DockBottom | DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop;
            Pad.Name = "PAD";
            Pad.Text = "PAD";
            Pad.FormClosing += new FormClosingEventHandler((s, ea) => { ea.Cancel = true; });
            PadGRBL(Pad);

            //this.ShowIcon = false;
            this.Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "/icon.ico");
            this.Text = "GRBL GUI";

            this.Load += new EventHandler((s, ea) =>
            {
                try
                {
                    if (Properties.Settings.Default.Size.Width == 0 || Properties.Settings.Default.Size.Height == 0)
                    {
                        // FIRST START
                    }
                    else
                    {
                        this.WindowState = Properties.Settings.Default.State;
                        if (this.WindowState == FormWindowState.Minimized) this.WindowState = FormWindowState.Normal;
                        this.Location = Properties.Settings.Default.Location;
                        this.Size = Properties.Settings.Default.Size;
                    }

                    DeserializeDockContent d = new DeserializeDockContent(GetContentFromPersistString);

                    DockPanel.LoadFromXml("DockLayout.xml", d);
                }
                catch
                {
                    //ERROR LOADING LAYOUT

                    Main.Show(DockPanel, DockState.Document);
                    Settings.Show(Main.DockPanel, DockState.DockRight);
                    Console.Show(Main.DockPanel, DockState.DockRight);
                    Pad.Show(Main.DockPanel, DockState.DockRight);

                    Main.DockPanel.DockRightPortion = ClientRectangle.Width / 2.5;
                }

                Controller.Connect();
            });

            this.FormClosing += new FormClosingEventHandler((s, ea) =>
            {
                try
                {
                    Properties.Settings.Default.State = this.WindowState;
                    if (this.WindowState == FormWindowState.Normal)
                    {
                        Properties.Settings.Default.Location = this.Location;
                        Properties.Settings.Default.Size = this.Size;
                    }
                    else
                    {
                        Properties.Settings.Default.Location = this.RestoreBounds.Location;
                        Properties.Settings.Default.Size = this.RestoreBounds.Size;
                    }

                    Properties.Settings.Default.Save();

                    DockPanel.SaveAsXml("DockLayout.xml");
                }
                catch { } //ERROR SAVING LAYOUT
            });

            IDockContent GetContentFromPersistString(string persistString)
            {
                DockContentId++;
                switch (DockContentId)
                {
                    case 1: return Main;
                    case 2: return Settings;
                    case 3: return Console;
                    case 4: return Pad;
                    default: return null;
                }
            }
        }

        #region methods
        public void MainInterfaceGRBL(Form form)
        {
            double[] WorkOffset = new double[3] { 0.0, 0.0, 0.0 };
            double[] MachPos = new double[3] { 0.0, 0.0, 0.0 };
            double[] WorkPos = new double[3] { 0.0, 0.0, 0.0 };
            double[] FS = new double[2] { 0.0, 0.0 };

            Section Sec = new Section();

            PadElement ComTools = new PadElement(15f, false);
            ComTools.ButtonElements = new List<PadButton>();
            PadButton ConnectButton = new PadButton("🔗", 1.5f, 0, 0, Color.Black, true, false);
            ComTools.ButtonElements.Add(ConnectButton);
            PadButton ConnectOptButton = new PadButton("⚙", 1.5f, 1, 0, Color.Black);
            ComTools.ButtonElements.Add(ConnectOptButton);
            Sec.Elements.Add(ComTools);

            ReportElement RepSTATUS = new ReportElement("STATUS", "DISC.", 16f, 120, Color.Black, Color.White, Color.Red, Color.Black);
            ReportElement RepX = new ReportElement("X", "", 20f, 36, Color.DarkGreen, Color.White, Color.White, Color.DarkGreen);
            ReportElement RepMachX = new ReportElement("X MACH", "", 12f, 80, Color.Black, Color.White, Color.White, Color.Black);
            ReportElement RepY = new ReportElement("Y", "", 20f, 36, Color.DarkRed, Color.White, Color.White, Color.DarkRed);
            ReportElement RepMachY = new ReportElement("Y MACH", "", 12f, 80, Color.Black, Color.White, Color.White, Color.Black);
            ReportElement RepZ = new ReportElement("Z", "", 20f, 36, Color.DarkBlue, Color.White, Color.White, Color.DarkBlue);
            ReportElement RepMachZ = new ReportElement("Z MACH", "", 12f, 80, Color.Black, Color.White, Color.White, Color.Black);
            ReportElement RepF = new ReportElement("F", "", 20f, 36, Color.DarkMagenta, Color.White, Color.White, Color.DarkMagenta);
            ReportElement RepS = new ReportElement("S", "", 20f, 36, Color.DarkMagenta, Color.White, Color.White, Color.DarkMagenta);
            Sec.Elements.AddRange(new List<ReportElement>() { RepSTATUS, RepX, RepMachX, RepY, RepMachY, RepZ, RepMachZ, RepF, RepS });

            PadElement FileTools = new PadElement(15f, false);
            FileTools.ButtonElements = new List<PadButton>();
            PadButton FileLoadButton = new PadButton("📁", 1.5f, 0, 0, Color.Black);
            FileTools.ButtonElements.Add(FileLoadButton);
            PadButton FileRun = new PadButton("▶", 1.5f, 1, 0, Color.Black);
            FileTools.ButtonElements.Add(FileRun);
            PadButton FileHold = new PadButton("✋", 1.5f, 2, 0, Color.Black);
            FileTools.ButtonElements.Add(FileHold);
            PadButton FileRestart = new PadButton("🔁", 1.5f, 3, 0, Color.Black);
            FileTools.ButtonElements.Add(FileRestart);
            Sec.Elements.Add(FileTools);

            ReportElement FileLoaded = new ReportElement("FILE", "", 12f, 80, Color.Black, Color.White, Color.White, Color.Black);
            Sec.Elements.Add(FileLoaded);

            ListElement FileList = new ListElement(new List<string>() { "#", "CMD", "STATE" }, true, 12f);
            Sec.Elements.Add(FileList);

            Sec.InitialyzeSection(form);

            ConnectButton.Click += new EventHandler((s, ea) =>
            {
                if (!Controller.Connected)
                {
                    Controller.Connect();
                }
                else
                {
                    Controller.Disconnect();
                }
            });

            ConnectOptButton.Click += new EventHandler((s, ea) =>
            {
                using (Form connectOptForm = new Form())
                {
                    connectOptForm.Text = string.Empty;
                    connectOptForm.ShowIcon = false;
                    connectOptForm.Size = new Size(320, 120);
                    connectOptForm.MinimizeBox = false;
                    connectOptForm.MaximizeBox = false;
                    connectOptForm.ShowInTaskbar = false;
                    connectOptForm.FormBorderStyle = FormBorderStyle.FixedSingle;
                    connectOptForm.StartPosition = FormStartPosition.CenterParent;

                    connectOptForm.Controls.Add(new Label()
                    {
                        Text = "Port:",
                        Width = 60,
                        Location = new Point(10, 10)
                    });

                    connectOptForm.Controls.Add(new Label()
                    {
                        Text = "Baud:",
                        Width = 60,
                        Location = new Point(10, 40)
                    });

                    string[] ports = SerialPort.GetPortNames();

                    connectOptForm.Controls.Add(new ComboBox()
                    {
                        Text = Controller.SerialPortName,
                        Width = 200,
                        Location = new Point(80, 10),
                        DataSource = ports
                    });

                    connectOptForm.Controls.Add(new TextBox()
                    {
                        Text = Controller.SerialPortBaudrate.ToString(),
                        Width = 200,
                        Location = new Point(80, 40)
                    });

                    connectOptForm.FormClosing += new FormClosingEventHandler((fcs, fcea) =>
                    {
                        Controller.SerialPortName = connectOptForm.Controls[2].Text;
                        Properties.Settings.Default.Port = Controller.SerialPortName;
                        Int32.TryParse(connectOptForm.Controls[3].Text, out Controller.SerialPortBaudrate);
                    });

                    connectOptForm.ShowDialog();
                }
            });

            FileLoadButton.Click += new EventHandler((s, ea) => 
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 2;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        FileLoaded.Value = openFileDialog.FileName;

                        List<string> GList = File.ReadAllLines(FileLoaded.Value).ToList();
                        Controller.SetFileStream(GList);

                        FileList.ListViewControl.Items.Clear();
                        for (int i = 0; i < GList.Count(); i++)
                        {
                            FileList.ListViewControl.Items.Add(new ListViewItem(new string[] { i.ToString(), GList[i], "" }));
                        }

                        FileList.ListViewControl.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                        FileList.ListViewControl.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                        FileList.ListViewControl.Columns[2].Width = 120;
                    }
                }
            });

            FileRun.Click += new EventHandler((s, ea) => 
            {
                for (int i = Controller.FilePositionSent; i < FileList.ListViewControl.Items.Count; i++)
                {
                    FileList.ListViewControl.Items[i].BackColor = Color.White;
                    FileList.ListViewControl.Items[i].SubItems[2].Text = "QUEUE";
                }

                Controller.FileStreamStart();
            });

            FileHold.Click += new EventHandler((s, ea) =>
            {
                Controller.FileStreamHold();
            });

            FileRestart.Click += new EventHandler((s, ea) => 
            { 
                if (Controller.Mode != Controller.OperatingMode.SendFile)
                {
                    Controller.FileStreamStop();

                    for (int i = 0; i < FileList.ListViewControl.Items.Count; i++)
                    {
                        FileList.ListViewControl.Items[i].BackColor = Color.White;
                        FileList.ListViewControl.Items[i].SubItems[2].Text = "";
                    }
                }
            });

            Controller.ConnectionStatusChanged += new Controller.ReportConnectionStatusEventHandler((s, ea) =>
            {
                if (ea.IsConnected())
                {
                    ConnectButton.Checked = true;
                }
                else
                {
                    ConnectButton.Checked = false;

                    if (form.IsHandleCreated)
                    {
                        form.Invoke(new Action(() =>
                        {
                            RepSTATUS.ValueControl.BackColor = Color.Red;
                            RepSTATUS.Value = "DISC.";

                            RepX.Value = "";
                            RepY.Value = "";
                            RepZ.Value = "";

                            RepMachX.Value = "";
                            RepMachY.Value = "";
                            RepMachZ.Value = "";

                            RepF.Value = "";
                            RepS.Value = "";
                        }));
                    }
                }
            });

            Controller.SentQueued += new Controller.SentQueuedEventHandler((s, ea) => 
            {
                form.Invoke(new Action(() =>
                {
                    if (ea.FromFile())
                    {
                        FileList.ListViewControl.Items[ea.GetLine()].BackColor = Color.LightBlue;
                        FileList.ListViewControl.Items[ea.GetLine()].SubItems[2].Text = "SENT";
                        FileList.ListViewControl.Items[ea.GetLine()].EnsureVisible();
                    }
                }));
            });

            Controller.SentDequeued += new Controller.SentDequeuedEventHandler((s, ea) =>
            {
                form.Invoke(new Action(() =>
                {
                    if (ea.FromFile())
                    {
                        if (ea.IsOk())
                        {
                            FileList.ListViewControl.Items[ea.GetLine()].BackColor = Color.LightGreen;
                            FileList.ListViewControl.Items[ea.GetLine()].SubItems[2].Text = "OK";
                        }
                        else
                        {
                            FileList.ListViewControl.Items[ea.GetLine()].BackColor = Color.LightSalmon;
                            FileList.ListViewControl.Items[ea.GetLine()].SubItems[2].Text = ea.GetErrorMsg();
                        }
                    }
                }));
            });

            Controller.ReportStatus += new Controller.ReportStatusEventHandler((s, ea) =>
            {
                Regex StatusEx = new Regex(Controller.StatusReplyRegexPattern);
                MatchCollection statusMatch = StatusEx.Matches(ea.GetStatus());

                if (statusMatch.Count == 0)
                {
                    return;
                }

                foreach (Match m in statusMatch)
                {
                    if (m.Index == 1)
                    {
                        if (m.Groups[1].Value == "Idle")
                        {
                            Controller.Status = Controller.MachineStatus.Idle;
                            form.Invoke(new Action(() =>
                            {
                                RepSTATUS.ValueControl.BackColor = Color.Green;
                                RepSTATUS.Value = "IDLE";
                            }));
                        }
                        else if (m.Groups[1].Value == "Run")
                        {
                            Controller.Status = Controller.MachineStatus.Run;
                            form.Invoke(new Action(() =>
                            {
                                RepSTATUS.ValueControl.BackColor = Color.Blue;
                                RepSTATUS.Value = "RUN";
                            }));
                        }
                        else if (m.Groups[1].Value == "Hold")
                        {
                            Controller.Status = Controller.MachineStatus.Hold;
                            form.Invoke(new Action(() =>
                            {
                                RepSTATUS.ValueControl.BackColor = Color.Yellow;
                                RepSTATUS.Value = "HOLD";
                            }));
                        }
                        else if (m.Groups[1].Value == "Jog")
                        {
                            Controller.Status = Controller.MachineStatus.Run;
                            form.Invoke(new Action(() =>
                            {
                                RepSTATUS.ValueControl.BackColor = Color.Blue;
                                RepSTATUS.Value = "JOG";
                            }));
                        }
                        else
                        {
                            Controller.Status = Controller.MachineStatus.Alarm;
                            form.Invoke(new Action(() =>
                            {
                                RepSTATUS.ValueControl.BackColor = Color.Orange;
                                RepSTATUS.Value = "ALARM";
                            }));
                        }
                        continue;
                    }
                }

                foreach (Match m in statusMatch)
                {
                    if (m.Groups[1].Value == "MPos")
                    {
                        try
                        {
                            string PositionString = m.Groups[2].Value;

                            double.TryParse(PositionString.Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out MachPos[0]);
                            double.TryParse(PositionString.Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out MachPos[1]);
                            double.TryParse(PositionString.Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out MachPos[2]);

                        }
                        catch { } // NON FATAL ERROR

                        WorkPos[0] = MachPos[0] - WorkOffset[0];
                        WorkPos[1] = MachPos[1] - WorkOffset[1];
                        WorkPos[2] = MachPos[2] - WorkOffset[2];

                        form.Invoke(new Action(() =>
                        {
                            RepX.Value = WorkPos[0].ToString("#0.000");
                            RepY.Value = WorkPos[1].ToString("#0.000");
                            RepZ.Value = WorkPos[2].ToString("#0.000");

                            RepMachX.Value = MachPos[0].ToString("#0.000");
                            RepMachY.Value = MachPos[1].ToString("#0.000");
                            RepMachZ.Value = MachPos[2].ToString("#0.000");
                        }));
                    }

                    if (m.Groups[1].Value == "WPos")
                    {
                        try
                        {
                            string PositionString = m.Groups[2].Value;

                            double.TryParse(PositionString.Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkPos[0]);
                            double.TryParse(PositionString.Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkPos[1]);
                            double.TryParse(PositionString.Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkPos[2]);

                        }
                        catch { } // NON FATAL ERROR

                        MachPos[0] = WorkPos[0] + WorkOffset[0];
                        MachPos[1] = WorkPos[1] + WorkOffset[1];
                        MachPos[2] = WorkPos[2] + WorkOffset[2];

                        form.Invoke(new Action(() =>
                        {
                            RepX.Value = WorkPos[0].ToString("#0.000");
                            RepY.Value = WorkPos[1].ToString("#0.000");
                            RepZ.Value = WorkPos[2].ToString("#0.000");

                            RepMachX.Value = MachPos[0].ToString("#0.000");
                            RepMachY.Value = MachPos[1].ToString("#0.000");
                            RepMachZ.Value = MachPos[2].ToString("#0.000");
                        }));
                    }

                    if (m.Groups[1].Value == "WCO")
                    {
                        try
                        {
                            string PositionString = m.Groups[2].Value;

                            double.TryParse(PositionString.Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkOffset[0]);
                            double.TryParse(PositionString.Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkOffset[1]);
                            double.TryParse(PositionString.Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out WorkOffset[2]);
                        }
                        catch { } // NON FATAL ERROR
                    }

                    if (m.Groups[1].Value == "FS")
                    {
                        try
                        {
                            string FSString = m.Groups[2].Value;

                            double.TryParse(FSString.Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out FS[0]);
                            double.TryParse(FSString.Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out FS[1]);
                            
                        }
                        catch { } // NON FATAL ERROR

                        form.Invoke(new Action(() =>
                        {
                            RepF.Value = FS[0].ToString("#0.000");
                            RepS.Value = FS[1].ToString("#0.000");
                        }));

                    }
                }
            });
        }

        public void ConsoleGRBL(Form form)
        {
            Section Sec = new Section();
            ConsoleElement Console = new ConsoleElement(true, true, true, true, 12f);
            Sec.Elements.Add(Console);
            Sec.InitialyzeSection(form);

            Console.SendButtonControl.Click += new EventHandler((s, ea) =>
            {
                Controller.SendLine(Console.InputControl.Text);

                form.Invoke(new Action(() =>
                {
                    Console.InputControl.Text = "";
                }));
            });

            Console.InputControl.KeyDown += new KeyEventHandler((s, ea) =>
            {
                if (ea.KeyCode == Keys.Enter)
                {
                    Controller.SendLine(Console.InputControl.Text);

                    form.Invoke(new Action(() =>
                    {
                        Console.InputControl.Text = "";
                    }));

                    ea.Handled = ea.SuppressKeyPress = true;
                }
            });

            Console.ClearButtonControl.Click += new EventHandler((s, ea) =>
            {
                form.Invoke(new Action(() =>
                {
                    Console.ConsoleControl.Text = "";
                    Console.InputControl.Text = "";
                }));
            });

            Controller.SentDequeued += new Controller.SentDequeuedEventHandler((s, ea) =>
            {
                form.Invoke(new Action(() =>
                {
                    if (!ea.FromFile())
                    {
                        Console.ConsoleControl.Text = ea.GetSent() + " < ok\r\n" + Console.ConsoleControl.Text;
                    }
                }));
            });

            Controller.Reply += new Controller.ReplyEventHandler((s, ea) =>
            {
                form.Invoke(new Action(() =>
                {
                    Console.ConsoleControl.Text = ea.GetReply() + "\r\n" + Console.ConsoleControl.Text;
                }));
            });
        }

        public void PadGRBL(Form form)
        {
            Section Sec = new Section();

            Track TrackS = new Track(Color.Red, Color.DarkGray, 15, 10, 0, 1000, 10);
            TrackS.CurrentValue = 500;
            TrackElement TrackElementS = new TrackElement("Spindle", "#0", 12f, 80,
                Color.Black, Color.White, Color.White, Color.Black, Color.DarkBlue, Color.White, TrackS, true, false);
            Sec.Elements.Add(TrackElementS);

            TrackElementS.CheckChanged += new EventHandler((s, ea) =>
            {
                if (TrackElementS.Checked)
                {
                    Controller.SendLine("S" + TrackS.CurrentValue.ToString("#0") + " M3");
                }
                else
                {
                    Controller.SendLine("M5");
                }
            });

            OptionSelectionElement StepsSel = new OptionSelectionElement("JOG STEPS", new List<string> { "0.01", "0.1", "1", "10" }, 2, 12f, 100,
                Color.White, Color.Black,
                Color.Black, Color.White,
                Color.DarkBlue, Color.White);

            Sec.Elements.Add(StepsSel);

            OptionSelectionElement SpeedSel = new OptionSelectionElement("JOG F", new List<string> { "1", "10", "100", "500" }, 2, 12f, 100,
                Color.White, Color.Black,
                Color.Black, Color.White,
                Color.DarkBlue, Color.White);

            Sec.Elements.Add(SpeedSel);

            PadElement Pad = new PadElement(25f);
            Pad.ButtonElements = new List<PadButton>();

            PadButton UnlockButton = new PadButton("🔓", 1f, 0, 0, Color.Black);
            Pad.ButtonElements.Add(UnlockButton);

            PadButton StopButton = new PadButton("⌀", 1.5f, 1, 1, Color.Black);
            Pad.ButtonElements.Add(StopButton);

            PadButton ResetButton = new PadButton("↻", 1.5f, 0, 2, Color.Black);
            Pad.ButtonElements.Add(ResetButton);

            PadButton XmButton = new PadButton("X-", 1f, 0, 1, Color.DarkGreen);
            Pad.ButtonElements.Add(XmButton);

            PadButton XpButton = new PadButton("X+", 1f, 2, 1, Color.DarkGreen);
            Pad.ButtonElements.Add(XpButton);

            PadButton YmButton = new PadButton("Y-", 1f, 1, 2, Color.DarkRed);
            Pad.ButtonElements.Add(YmButton);

            PadButton YpButton = new PadButton("Y+", 1f, 1, 0, Color.DarkRed);
            Pad.ButtonElements.Add(YpButton);

            PadButton ZmButton = new PadButton("Z-", 1f, 2, 2, Color.DarkBlue);
            Pad.ButtonElements.Add(ZmButton);

            PadButton ZpButton = new PadButton("Z+", 1f, 2, 0, Color.DarkBlue);
            Pad.ButtonElements.Add(ZpButton);

            PadButton HomeButton = new PadButton("Home", 0.5f, 3, 0, Color.Black);
            Pad.ButtonElements.Add(HomeButton);

            PadButton SafePos1Button = new PadButton("Safe Pos 1", 0.5f, 3, 1, Color.Black);
            Pad.ButtonElements.Add(SafePos1Button);

            PadButton SafePos2Button = new PadButton("Safe Pos 2", 0.5f, 3, 2, Color.Black);
            Pad.ButtonElements.Add(SafePos2Button);

            Sec.Elements.Add(Pad);

            OptionSelectionElement OffsetSel = new OptionSelectionElement("OFFSET", new List<string> { "G54", "G55", "G56", "G57", "G58", "G59" }, 0, 12f, 100,
                Color.White, Color.Black,
                Color.Black, Color.White,
                Color.DarkBlue, Color.White);

            Sec.Elements.Add(OffsetSel);

            PadElement PadOffset = new PadElement(25f);
            PadOffset.ButtonElements = new List<PadButton>();

            PadButton OffsetX0Button = new PadButton("Offset\r\nX=0", 0.5f, 0, 0, Color.DarkGreen);
            PadOffset.ButtonElements.Add(OffsetX0Button);

            PadButton OffsetY0Button = new PadButton("Offset\r\nY=0", 0.5f, 1, 0, Color.DarkRed);
            PadOffset.ButtonElements.Add(OffsetY0Button);

            PadButton OffsetZ0Button = new PadButton("Offset\r\nZ=0", 0.5f, 2, 0, Color.DarkBlue);
            PadOffset.ButtonElements.Add(OffsetZ0Button);

            PadButton OffsetXYZ0Button = new PadButton("Offset\r\nXYZ=0", 0.5f, 3, 0, Color.Black);
            PadOffset.ButtonElements.Add(OffsetXYZ0Button);

            PadButton OriginX0Button = new PadButton("Origin\r\nX=0", 0.5f, 0, 1, Color.DarkGreen);
            PadOffset.ButtonElements.Add(OriginX0Button);

            PadButton OriginY0Button = new PadButton("Origin\r\nY=0", 0.5f, 1, 1, Color.DarkRed);
            PadOffset.ButtonElements.Add(OriginY0Button);

            PadButton OriginZ0Button = new PadButton("Origin\r\nZ=0", 0.5f, 2, 1, Color.DarkBlue);
            PadOffset.ButtonElements.Add(OriginZ0Button);

            PadButton OriginXYZ0Button = new PadButton("Origin\r\nXYZ=0", 0.5f, 3, 1, Color.Black);
            PadOffset.ButtonElements.Add(OriginXYZ0Button);

            Sec.Elements.Add(PadOffset);

            Sec.InitialyzeSection(form);

            ResetButton.Click += new EventHandler((s, ea) => 
            {
                Controller.SendPriorityLine(((char)(0x18)).ToString());
                Controller.SoftReset(); 
            });
            UnlockButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$X"); });
            StopButton.Click += new EventHandler((s, ea) => 
            {
                Controller.SendPriorityLine(((char)(0x18)).ToString()); 
            });

            HomeButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$H"); });
            SafePos1Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G28"); });
            SafePos2Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G30"); });

            XmButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91X-" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });
            XpButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91X+" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });
            YmButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91Y-" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });
            YpButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91Y+" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });
            ZmButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91Z-" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });
            ZpButton.Click += new EventHandler((s, ea) => { Controller.SendLine("$J=G91Z+" + StepsSel.Options[StepsSel.Selection] + "F" + SpeedSel.Options[SpeedSel.Selection]); });

            OffsetX0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G10 L20 X0"); });
            OffsetY0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G10 L20 Y0"); });
            OffsetZ0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G10 L20 Z0"); });
            OffsetXYZ0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G10 L20 X0 Y0 Z0"); });

            OriginX0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G92 X0"); });
            OriginY0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G92 Y0"); });
            OriginZ0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G92 Z0"); });
            OriginXYZ0Button.Click += new EventHandler((s, ea) => { Controller.SendLine("G92 X0 Y0 Z0"); });

            OffsetSel.SelectionChanged += new OptionSelectionElement.SelectionChangedEventHandler((s, ea) =>
            {
                Controller.SendLine(ea.GetSelection());
            });

            Controller.Reply += new Controller.ReplyEventHandler((s, ea) =>
            {
                Regex GcodeEx;
                MatchCollection GcodeMatch;

                GcodeEx = new Regex(@"\[GC\:.+\]");
                if (GcodeEx.IsMatch(ea.GetReply()))
                {
                    GcodeEx = new Regex(@"[GMTFS][0-9]+");
                    GcodeMatch = GcodeEx.Matches(ea.GetReply());

                    foreach (Match m in GcodeMatch)
                    {
                        form.Invoke(new Action(() =>
                        {
                            if (OffsetSel.Options.Contains(m.Value))
                            {
                                OffsetSel.Selection = OffsetSel.Options.IndexOf(m.Value);
                            }
                        }));
                    }

                    GcodeEx = new Regex(@"[S][0-9]+");
                    GcodeMatch = GcodeEx.Matches(ea.GetReply());

                    foreach (Match m in GcodeMatch)
                    {
                        double curS = TrackS.CurrentValue;
                        double.TryParse(m.Value.Replace("S", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out curS);

                        form.Invoke(new Action(() =>
                        {
                            TrackS.CurrentValue = curS;
                        }));
                    }

                    GcodeEx = new Regex(@"[M][0-9]+");
                    GcodeMatch = GcodeEx.Matches(ea.GetReply());

                    foreach (Match m in GcodeMatch)
                    {
                        int StateM = 5;
                        Int32.TryParse(m.Value.Replace("M", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out StateM);

                        form.Invoke(new Action(() =>
                        {
                            if (StateM == 3 || StateM == 4) 
                            { 
                                TrackElementS.Checked = true; 
                            }
                        }));
                    }
                }
            });
        }

        public void SettingsGRBL(Form form)
        {
            Section Sec = new Section();

            PadElement SettingsTools = new PadElement(15f, false);
            PadButton SettingsRefresh = new PadButton("↻", 1.5f, 0, 0, Color.Black);
            SettingsTools.ButtonElements.Add(SettingsRefresh);
            PadButton GridSave = new PadButton("💾", 1.5f, 1, 0, Color.Black);
            SettingsTools.ButtonElements.Add(GridSave);
            Sec.Elements.Add(SettingsTools);

            ListElement GList = new ListElement(new List<string>() { "", "X", "Y", "Z" }, false, 12f, 10);

            Sec.Elements.Add(GList);

            GridElement Grid = new GridElement(true, 12f);
            Sec.Elements.Add(Grid);

            Sec.InitialyzeSection(form);

            SettingsRefresh.Click += new EventHandler((s, ea) =>
            {
                Grid.ObjectList.Clear();
                Controller.SendLine("$$");
                GList.ListViewControl.Items.Clear();
                Controller.SendLine("$#");
            });

            GridSave.Click += new EventHandler((s, ea) =>
            {
                foreach (DictionaryEntry obj in Grid.ObjectList)
                {
                    Controller.SendLine(obj.Key + "=" + obj.Value);
                }
                Controller.SendLine("$$");
            });

            Controller.Reply += new Controller.ReplyEventHandler((s, ea) =>
            {
                Regex SettingEx;
                MatchCollection SettingMatch;

                SettingEx = new Regex(@"(\$[0-9]+)=([0-9.]+)");
                SettingMatch = SettingEx.Matches(ea.GetReply());

                foreach (Match m in SettingMatch)
                {
                    if (Grid.ObjectList.Contains(m.Groups[1].Value))
                    {
                        Grid.ObjectList[m.Groups[1].Value] = m.Groups[2].Value;
                    }
                    else
                    {
                        Grid.ObjectList.Add(m.Groups[1].Value, m.Groups[2].Value);
                    }

                    form.Invoke(new Action(() =>
                    {
                        Grid.Refresh();
                    }));
                }

                // [G92:0.000,0.000,0.000]
                SettingEx = new Regex(@"\[(G[0-9]+)\:([0-9.]+)\,([0-9.]+)\,([0-9.]+)\]");
                SettingMatch = SettingEx.Matches(ea.GetReply());
                foreach (Match m in SettingMatch)
                {
                    form.Invoke(new Action(() =>
                    {
                        if (GList.ListViewControl.Items.ContainsKey(m.Groups[1].Value))
                        {
                            GList.ListViewControl.Items[m.Groups[1].Value].SubItems[1].Text = m.Groups[2].Value;
                            GList.ListViewControl.Items[m.Groups[1].Value].SubItems[2].Text = m.Groups[3].Value;
                            GList.ListViewControl.Items[m.Groups[1].Value].SubItems[3].Text = m.Groups[4].Value;
                        }
                        else
                        {
                            ListViewItem item = new ListViewItem();

                            item.Name = m.Groups[1].Value;
                            item.Text = m.Groups[1].Value;
                            item.SubItems.Add(m.Groups[2].Value);
                            item.SubItems.Add(m.Groups[3].Value);
                            item.SubItems.Add(m.Groups[4].Value);

                            GList.ListViewControl.Items.Add(item);
                        }
                    }));
                }
            });
        }

        #endregion
    }
}
