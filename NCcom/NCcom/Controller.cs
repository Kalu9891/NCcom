using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NCcom
{
    public class Controller
    {
        #region enums
        public enum MachineStatus
        {
            Idle,
            Hold,
            Run,
            Alarm,
            Disconnected,
        }
        public enum OperatingMode
        {
            Disconnected,
            Manual,
            SendFile,
        }
        public enum ConnectionType
        {
            Serial,
            Ethernet
        }
        #endregion

        #region fields
        public OperatingMode Mode;
        public MachineStatus Status;

        public string SerialPortName;
        public int SerialPortBaudrate;

        public string TcpClientHostName;
        public int TcpClientPort;

        public int BufferState = 0;
        public int ControllerBufferSize = 60;

        public string OkReplyRegexPattern;
        public string ErrorReplyRegexPattern;
        public string AlarmReplyRegexPattern;
        public string StatusReplyRegexPattern;

        private Regex OkReplyRegex;
        private Regex ErrorReplyRegex;
        private Regex AlarmReplyRegex;
        private Regex StatusReplyRegex;

        public ConnectionType Type;

        private bool _connected = false;
        public bool Connected
        {
            get { return _connected; }
            private set
            {
                _connected = value;

                if (!_connected)
                {
                    Mode = OperatingMode.Disconnected;
                    Status = MachineStatus.Disconnected;
                }

                ConnectionStatusChanged?.Invoke(this, new ReportConnectionStatusEventArgs(_connected));
            }
        }

        private Stream Stream;
        private Thread WorkerThread;
        private TcpClient ClientEthernet;

        private Queue Sent = Queue.Synchronized(new Queue());
        private Queue ToSend = Queue.Synchronized(new Queue());
        private Queue ToSendPriority = Queue.Synchronized(new Queue());
        #endregion

        #region events

        public event ReportConnectionStatusEventHandler ConnectionStatusChanged;

        public event ReplyEventHandler Reply;

        public event ReportStatusEventHandler ReportStatus;

        public event SentDequeuedEventHandler SentDequeued;

        public event SentQueuedEventHandler SentQueued;

        public delegate void ReportConnectionStatusEventHandler(object source, ReportConnectionStatusEventArgs e);

        public delegate void ReplyEventHandler(object source, ReplyEventArgs e);

        public delegate void ReportStatusEventHandler(object source, ReportStatusEventArgs e);

        public delegate void SentDequeuedEventHandler(object source, SentDequeuedEventArgs e);

        public delegate void SentQueuedEventHandler(object source, SentQueuedEventArgs e);

        public class ReportConnectionStatusEventArgs : EventArgs
        {
            private bool Connected;

            public ReportConnectionStatusEventArgs(bool connected)
            {
                Connected = connected;
            }

            public bool IsConnected()
            {
                return Connected;
            }
        }

        public class ReplyEventArgs : EventArgs
        {
            private string Reply;
            public ReplyEventArgs(string str)
            {
                Reply = str;
            }
            public string GetReply()
            {
                return Reply;
            }
        }

        public class ReportStatusEventArgs : EventArgs
        {
            private string Status;
            public ReportStatusEventArgs(string str)
            {
                Status = str;
            }
            public string GetStatus()
            {
                return Status;
            }
        }

        public class SentDequeuedEventArgs : EventArgs
        {
            private string Sent;

            private bool IsFile;

            private int Line;

            private bool Error;

            private string ErrorMsg;

            public SentDequeuedEventArgs(string sent, bool isFile = false, int line = -1, bool error = false, string errorMsg = "")
            {
                Sent = sent;
                IsFile = isFile;
                Line = line;
                Error = error;
                ErrorMsg = errorMsg;
            }

            public string GetSent()
            {
                return Sent;
            }

            public bool IsOk()
            {
                return !Error;
            }

            public string GetErrorMsg()
            {
                return ErrorMsg;
            }

            public bool FromFile()
            {
                return IsFile;
            }

            public int GetLine()
            {
                if (!IsFile) { return -1; }
                return Line;
            }

        }

        public class SentQueuedEventArgs : EventArgs
        {
            private string Sent;

            private bool IsFile;

            private int Line;

            public SentQueuedEventArgs(string sent, bool isFile = false, int line = -1)
            {
                Sent = sent;
                IsFile = isFile;
                Line = line;
            }

            public string GetSent()
            {
                return Sent;
            }

            public bool FromFile()
            {
                return IsFile;
            }

            public int GetLine()
            {
                if (!IsFile) { return -1; }
                return Line;
            }
        }

        #endregion

        #region constructors
        public Controller()
        {
            Application.ApplicationExit += new EventHandler((s, ea) =>
            {
                Task.Factory.StartNew(() => Disconnect());
            });
        }
        #endregion

        #region files
        private ReadOnlyCollection<string> _file = new ReadOnlyCollection<string>(new string[0]);
        public ReadOnlyCollection<string> File
        {
            get { return _file; }
            private set
            {
                _file = value;
                FilePositionSent = 0;
                FilePositionReceived = 0;
            }
        }

        private int _filePositionSent = 0;
        public int FilePositionSent
        {
            get { return _filePositionSent; }
            private set
            {
                _filePositionSent = value;
            }
        }

        private int _filePositionReceived = 0;
        public int FilePositionReceived
        {
            get { return _filePositionReceived; }
            private set
            {
                _filePositionReceived = value;
            }
        }

        public void SetFileStream(IList<string> file)
        {
            if (Status != MachineStatus.Idle)
            {
                return;
            }
            if (Sent.Count > 0)
            {
                return;
            }
            File = new ReadOnlyCollection<string>(file);
        }

        public void FileStreamHold()
        {
            if (Mode == OperatingMode.SendFile)
            {
                Mode = OperatingMode.Manual;
            }
        }

        public void FileStreamStop()
        {
            Mode = OperatingMode.Manual;
            FilePositionSent = 0;
            FilePositionReceived = 0;
        }

        public void FileStreamStart()
        {
            if (!Connected)
            {
                return;
            }
            if (Status != MachineStatus.Idle)
            {
                return;
            }
            if (Sent.Count > 0)
            {
                return;
            }

            Mode = OperatingMode.SendFile;
        }

        #endregion

        #region methods
        public void Work()
        {
            try
            {
                OkReplyRegex = new Regex(OkReplyRegexPattern, RegexOptions.Compiled);
                ErrorReplyRegex = new Regex(ErrorReplyRegexPattern, RegexOptions.Compiled);
                AlarmReplyRegex = new Regex(AlarmReplyRegexPattern, RegexOptions.Compiled);
                StatusReplyRegex = new Regex(StatusReplyRegexPattern, RegexOptions.Compiled);

                StreamReader reader = new StreamReader(Stream);
                StreamWriter writer = new StreamWriter(Stream);

                int StatusPollInterval = 120; // (ms)
                double WaitTimeValue = 0.1;
                BufferState = 0;

                IniWork(reader, writer, ref BufferState, ref StatusPollInterval, ref WaitTimeValue);

                TimeSpan WaitTime = TimeSpan.FromMilliseconds(WaitTimeValue);
                DateTime LastStatusPoll = DateTime.Now + WaitTime;
                DateTime StartTime = DateTime.Now;

                bool IsFile = false;

                while (true)
                {
                    Task<string> lineTask = reader.ReadLineAsync();

                    // WRITE
                    while (!lineTask.IsCompleted)
                    {
                        if (!Connected)
                        {
                            return;
                        }

                        while (ToSendPriority.Count > 0)
                        {
                            writer.Write((string)ToSendPriority.Dequeue());
                            writer.Write('\n');
                            writer.Flush();
                        }

                        if (Mode == OperatingMode.SendFile)
                        {
                            if (File.Count > FilePositionSent && (File[FilePositionSent].Length + 1) < (ControllerBufferSize - BufferState))
                            {
                                string send_line = File[FilePositionSent];

                                writer.Write(send_line);
                                writer.Write('\n');
                                writer.Flush();

                                Sent.Enqueue(send_line);
                                BufferState += send_line.Length + 1;
                                IsFile = true;
                                SentQueued?.Invoke(this, new SentQueuedEventArgs(send_line, IsFile, FilePositionSent));

                                if (++FilePositionSent >= File.Count)
                                {
                                    Mode = OperatingMode.Manual;
                                }
                            }
                        }
                        else if (ToSend.Count > 0 && (((string)ToSend.Peek()).Length + 1) < (ControllerBufferSize - BufferState))
                        {
                            string send_line = ((string)ToSend.Dequeue());

                            writer.Write(send_line);
                            writer.Write('\n');
                            writer.Flush();

                            Sent.Enqueue(send_line);
                            BufferState += send_line.Length + 1;
                            IsFile = false;
                            SentQueued?.Invoke(this, new SentQueuedEventArgs(send_line, IsFile));
                        }

                        DateTime Now = DateTime.Now;

                        if ((Now - LastStatusPoll).TotalMilliseconds > StatusPollInterval)
                        {
                            GetStatus(writer);
                            LastStatusPoll = Now;
                        }

                        Thread.Sleep(WaitTime);

                    }

                    // READ

                    string line = lineTask.Result;

                    if (OkReplyRegex.IsMatch(line))
                    {
                        if (Sent.Count != 0)
                        {
                            string lineok = (string)Sent.Dequeue();
                            BufferState -= lineok.Length + 1;
                            if (IsFile)
                            {
                                SentDequeued?.Invoke(this, new SentDequeuedEventArgs(lineok, true, FilePositionReceived));
                                FilePositionReceived++;
                            }
                            else 
                            { 
                                SentDequeued?.Invoke(this, new SentDequeuedEventArgs(lineok)); 
                            }
                        }
                        else
                        {
                            BufferState = 0;
                        }
                    }
                    else
                    {
                        if (StatusReplyRegex.IsMatch(line))
                        {
                            ReportStatus?.Invoke(this, new ReportStatusEventArgs(line));
                        }
                        else if (ErrorReplyRegex.IsMatch(line))
                        {
                            if (Sent.Count != 0)
                            {
                                string lineerror = (string)Sent.Dequeue();
                                BufferState -= lineerror.Length + 1;
                                if (IsFile)
                                {
                                    SentDequeued?.Invoke(this, new SentDequeuedEventArgs(lineerror, true, FilePositionReceived, true, line));
                                    FilePositionReceived++;
                                }
                                else
                                {
                                    SentDequeued?.Invoke(this, new SentDequeuedEventArgs(lineerror, false, -1, true, line));
                                }
                            }
                            else
                            {
                                if ((DateTime.Now - StartTime).TotalMilliseconds > 200)
                                {
                                    SentDequeued?.Invoke(this, new SentDequeuedEventArgs("", false, -1, true, $"Received <{line}> without anything in the Sent Buffer"));
                                }
                                BufferState = 0;
                                Mode = OperatingMode.Manual;
                            }
                        }
                        else if (AlarmReplyRegex.IsMatch(line))
                        {
                            Status = MachineStatus.Alarm;
                            Mode = OperatingMode.Manual;
                            ToSend.Clear();
                        }
                        else
                        {
                            Reply?.Invoke(this, new ReplyEventArgs(line));
                        }
                    }
                }
            }
            catch
            {
                Task.Factory.StartNew(() => Disconnect());
                return;
            }
        }

        public void Connect()
        {
            if (Connected)
            {
                MessageBox.Show("Can't Connect: Already Connected");
                return;
            }

            switch (Type)
            {
                case ConnectionType.Serial:
                    try
                    {
                        SerialPort port = new SerialPort(SerialPortName, SerialPortBaudrate);
                        port.DtrEnable = false;
                        port.Open();
                        Stream = port.BaseStream;
                        Connected = true;
                    }
                    catch (IOException)
                    {
                        Connected = false;
                    }
                    break;
                case ConnectionType.Ethernet:
                    try
                    {
                        ClientEthernet = new TcpClient(TcpClientHostName, TcpClientPort);
                        Connected = true;
                        Stream = ClientEthernet.GetStream();
                    }
                    catch (ArgumentNullException)
                    {
                        MessageBox.Show("Invalid address or port");
                    }
                    catch (SocketException)
                    {
                        MessageBox.Show("Connection failure");
                    }

                    break;
                default:
                    throw new Exception("Invalid Connection Type");
            }

            if (!Connected)
            {
                return;
            }

            ToSend.Clear();
            ToSendPriority.Clear();
            Sent.Clear();

            Mode = OperatingMode.Manual;

            WorkerThread = new Thread(Work);
            WorkerThread.Priority = ThreadPriority.AboveNormal;
            WorkerThread.Start();
        }

        public void Disconnect()
        {
            if (Connected)
            {
                Connected = false;

                WorkerThread.Join();

                switch (Type)
                {
                    case ConnectionType.Serial:

                        try
                        {
                            Stream.Close();
                        }
                        catch { }
                        Stream.Dispose();
                        Stream = null;

                        break;
                    case ConnectionType.Ethernet:

                        if (Stream != null)
                        {
                            Stream.Close();
                            ClientEthernet.Close();
                        }
                        Stream = null;

                        break;
                }
            }

            BufferState = 0;

            ClearQueues();
        }

        public void ClearQueues()
        {
            ToSend.Clear();
            ToSendPriority.Clear();
            Sent.Clear();
        }

        public void SendLine(string line)
        {
            if (!Connected)
            {
                return;
            }

            if (Mode != OperatingMode.Manual)
            {
                return;
            }

            ToSend.Enqueue(line);
        }

        public void SendPriorityLine(string line)
        {
            if (!Connected)
            {
                return;
            }

            ToSendPriority.Enqueue(line);
        }

        public virtual void IniWork(StreamReader reader, StreamWriter writer, ref int bufferstate, ref int statusPollInterval, ref double waitTimeValue) { }

        public virtual void GetStatus(StreamWriter writer) { }

        public virtual void SoftReset() { }

        #endregion
    }

    public class GRBL : Controller
    {
        #region constructors
        public GRBL(string com, int baudrate)
        {
            Type = ConnectionType.Serial;
            SerialPortName = com;
            SerialPortBaudrate = baudrate;

            OkReplyRegexPattern = "ok";
            ErrorReplyRegexPattern = "(error).+";
            AlarmReplyRegexPattern = "(alarm).+";
            StatusReplyRegexPattern = @"(?<=[<|])(\w+):?([^|>]*)?(?=[|>])";
        }
        #endregion

        #region methods
        public override void IniWork(StreamReader reader, StreamWriter writer,
            ref int bufferstate, ref int statusPollInterval, ref double waitTimeValue)
        {
            writer.Write("\n$G\n");
            writer.Write("\n$#\n");
            writer.Write("\n$$\n");
            writer.Flush();
        }

        public override void GetStatus(StreamWriter writer)
        {
            writer.Write('?');
            writer.Flush();
        }

        public override void SoftReset()
        {
            if (!Connected)
            {
                return;
            }

            Mode = OperatingMode.Manual;

            ClearQueues();

            BufferState = 0;

            SendLine("$G");
            SendLine("$#");
        }

        #endregion
    }
}
