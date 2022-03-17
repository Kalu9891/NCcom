using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NCcom
{
    public class Section
    {
        #region fields
        public List<Element> Elements;
        public int Margin = 6;
        public FontFamily FontFamily = Control.DefaultFont.FontFamily;
        #endregion

        #region constructors
        public Section(int margin = 6)
        {
            this.Elements = new List<Element>();
            this.Margin = margin;
        }
        #endregion

        #region methods
        public void InitialyzeSection(Form form)
        {
            form.ClientSizeChanged += new EventHandler((s, ea) =>
            {
                foreach (Element el in Elements)
                {
                    if (el.GetType() == typeof(ReportElement))
                    {
                    }
                    else if (el.GetType() == typeof(TrackElement))
                    {
                    }
                    else if (el.GetType() == typeof(ConsoleElement))
                    {
                    }
                    else if (el.GetType() == typeof(OptionSelectionElement))
                    {
                        OptionSelectionElement ose = (OptionSelectionElement)el;

                        int w = (int)((form.ClientRectangle.Width - ose.LabelControl.Width - 2 * Margin) / (ose.Options.Count()));
                        w -= Margin;

                        for (int i = 0; i < ose.Options.Count(); i++)
                        {
                            ose.ValueControls[i].Location = new Point(Margin + ose.LabelControl.Width + Margin + i * (w + Margin), ose.ValueControls[i].Location.Y);
                            ose.ValueControls[i].Width = w;
                        }
                    }
                    else if (el.GetType() == typeof(PadElement))
                    {
                        PadElement pad = (PadElement)el;
                        for (int i = 0; i < pad.ButtonElements.Count(); i++)
                        {
                            if (pad.CenterAlign)
                            {
                                pad.ButtonElements[i].Location =
                                new Point((int)((form.ClientRectangle.Width - pad.Width) / 2.0) + pad.ButtonElements[i].X * (pad.ButtonElements[i].Width + Margin), pad.ButtonElements[i].Location.Y);
                            }
                            else
                            {
                                pad.ButtonElements[i].Location = new Point(Margin + pad.ButtonElements[i].X * (pad.ButtonElements[i].Width + Margin), pad.ButtonElements[i].Location.Y);
                            }
                        }
                    }
                }
            });

            int ypos = Margin;

            for (int i = 0; i < Elements.Count; i++)
            {
                Elements[i].PosY = ypos;

                if (Elements[i].GetType() == typeof(ReportElement))
                {
                    ReportElement re = (ReportElement)Elements[i];

                    re.LabelControl = new Label();
                    re.LabelControl.Font = new Font(FontFamily, re.FontSize, FontStyle.Regular);
                    re.LabelControl.Location = new Point(Margin, ypos);
                    re.LabelControl.BackColor = re.BackColorLabel;
                    re.LabelControl.ForeColor = re.ForeColorLabel;
                    re.LabelControl.AutoSize = false;
                    re.LabelControl.Height = (int)(2 * re.FontSize);
                    re.LabelControl.Width = re.LabelWidth;
                    re.LabelControl.Text = re.Label;
                    re.LabelControl.TextAlign = ContentAlignment.MiddleCenter;

                    form.Controls.Add(re.LabelControl);

                    re.ValueControl = new Label();
                    re.ValueControl.Font = new Font(FontFamily, re.FontSize, FontStyle.Regular);
                    re.ValueControl.Location = new Point(Margin + re.LabelControl.Width + Margin, ypos);
                    re.ValueControl.BackColor = re.BackColorValue;
                    re.ValueControl.ForeColor = re.ForeColorValue;
                    re.ValueControl.AutoSize = false;
                    re.ValueControl.Width = form.ClientRectangle.Width - re.LabelControl.Width - 3 * Margin;
                    re.ValueControl.Height = re.LabelControl.Height;
                    re.ValueControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    re.ValueControl.TextAlign = ContentAlignment.MiddleRight;
                    re.ValueControl.Text = re.Value;

                    form.Controls.Add(re.ValueControl);

                    ypos = ypos + re.ValueControl.Height + Margin;
                }
                else if (Elements[i].GetType() == typeof(TrackElement))
                {
                    TrackElement te = (TrackElement)Elements[i];

                    te.LabelControl = new Label();
                    te.LabelControl.Font = new Font(FontFamily, te.FontSize, FontStyle.Regular);
                    te.LabelControl.Location = new Point(Margin, ypos);
                    if (te.CanCheck && te.Checked)
                    {
                        te.LabelControl.BackColor = te.BackColorSelection;
                        te.LabelControl.ForeColor = te.ForeColorSelection;
                    }
                    else
                    {
                        te.LabelControl.BackColor = te.BackColorLabel;
                        te.LabelControl.ForeColor = te.ForeColorLabel;
                    }
                    te.LabelControl.AutoSize = false;
                    te.LabelControl.Height = (int)(2 * te.FontSize);
                    te.LabelControl.Width = te.LabelWidth;
                    te.LabelControl.Text = te.Label;
                    te.LabelControl.TextAlign = ContentAlignment.MiddleCenter;
                    
                    if (te.CanCheck)
                    {
                        te.LabelControl.Click += new EventHandler((s, ea) =>
                        {
                            te.Checked = !te.Checked;
                        });
                    }
                    
                    form.Controls.Add(te.LabelControl);

                    te.ValueControl = new Label();
                    te.ValueControl.Font = new Font(FontFamily, te.FontSize, FontStyle.Regular);
                    te.ValueControl.Location = new Point(Margin + te.LabelControl.Width + Margin, ypos);
                    te.ValueControl.BackColor = te.BackColorValue;
                    te.ValueControl.ForeColor = te.ForeColorValue;
                    te.ValueControl.AutoSize = false;
                    te.ValueControl.Width = form.ClientRectangle.Width - te.LabelControl.Width - 3 * Margin;
                    te.ValueControl.Height = te.LabelControl.Height;
                    te.ValueControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    te.ValueControl.TextAlign = ContentAlignment.MiddleRight;
                    te.ValueControl.Text = te.Value;

                    form.Controls.Add(te.ValueControl);

                    ypos = ypos + te.ValueControl.Height + Margin;

                    if (te.TrackControl != null)
                    {
                        te.ValueControl.Text = te.TrackControl.CurrentValue.ToString();
                        te.TrackControl.Width = form.ClientRectangle.Width - 2 * Margin;
                        te.TrackControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        te.TrackControl.Location = new Point(Margin, ypos);

                        form.Controls.Add(te.TrackControl);

                        ypos = ypos + te.TrackControl.Height + Margin;
                    }
                }
                else if (Elements[i].GetType() == typeof(OptionSelectionElement))
                {
                    OptionSelectionElement ose = (OptionSelectionElement)Elements[i];

                    ose.LabelControl = new Label();
                    ose.LabelControl.Font = new Font(FontFamily, ose.FontSize, FontStyle.Regular);
                    ose.LabelControl.Location = new Point(Margin, ypos);
                    ose.LabelControl.BackColor = ose.BackColorLabel;
                    ose.LabelControl.ForeColor = ose.ForeColorLabel;
                    ose.LabelControl.AutoSize = false;
                    ose.LabelControl.Height = (int)(2 * ose.FontSize);
                    ose.LabelControl.Width = ose.LabelWidth;
                    ose.LabelControl.Text = ose.Label;
                    ose.LabelControl.TextAlign = ContentAlignment.MiddleCenter;

                    form.Controls.Add(ose.LabelControl);

                    int w = (int)((form.ClientRectangle.Width - ose.LabelControl.Width - 2 * Margin) / (ose.Options.Count()));
                    w -= Margin;

                    for (int j = 0; j < ose.Options.Count(); j++)
                    {
                        ose.ValueControls.Add(new Label());
                        ose.ValueControls[j].Font = new Font(FontFamily, ose.FontSize, FontStyle.Regular);
                        ose.ValueControls[j].Location = new Point(Margin + ose.LabelControl.Width + Margin + j * (w + Margin), ypos);
                        if (j == ose.Selection)
                        {
                            ose.ValueControls[j].BackColor = ose.BackColorSelection;
                            ose.ValueControls[j].ForeColor = ose.ForeColorSelection;
                        }
                        else
                        {
                            ose.ValueControls[j].BackColor = ose.BackColorValue;
                            ose.ValueControls[j].ForeColor = ose.ForeColorValue;
                        }
                        ose.ValueControls[j].AutoSize = false;
                        ose.ValueControls[j].Width = w;
                        ose.ValueControls[j].Height = ose.LabelControl.Height;
                        ose.ValueControls[j].TextAlign = ContentAlignment.MiddleRight;
                        ose.ValueControls[j].Text = ose.Options[j];

                        ose.ValueControls[j].Click += new EventHandler((s, ea) => 
                        {
                            Label label = (Label)s;
                            ose.Selection = ose.Options.IndexOf(label.Text); 
                        });

                        form.Controls.Add(ose.ValueControls[j]);
                    }

                    ypos = ypos + ose.LabelControl.Height + Margin;
                }
                else if (Elements[i].GetType() == typeof(PadElement))
                {
                    PadElement pad = (PadElement)Elements[i];

                    pad.GetSize(Margin);

                    foreach (PadButton be in pad.ButtonElements)
                    {
                        be.Name = be.Name;
                        be.UseVisualStyleBackColor = false;
                        be.ForeColor = be.ForeColor;
                        be.FlatStyle = FlatStyle.Flat;
                        be.TextAlign = ContentAlignment.MiddleCenter;
                        be.BackColor = Color.White;
                        be.Font = new Font(FontFamily, be.FontScale * pad.FontSize, FontStyle.Regular);
                        be.Text = be.Text;
                        be.UseVisualStyleBackColor = false;
                        be.Height = (int)(3.0 * pad.FontSize);
                        be.Width = be.Height;
                        if (pad.CenterAlign)
                        {
                            be.Location = new Point((int)((form.ClientRectangle.Width - pad.Width) / 2.0) + be.X * (be.Width + Margin), pad.PosY + be.Y * (be.Width + Margin));
                        }
                        else
                        {
                            be.Location = new Point(Margin + be.X * (be.Width + Margin), pad.PosY + be.Y * (be.Width + Margin));
                        }

                        form.Controls.Add(be);
                    }

                    ypos = ypos + pad.Height + Margin;
                }
                else if (Elements[i].GetType() == typeof(ConsoleElement))
                {
                    ConsoleElement mdi = (ConsoleElement)Elements[i];

                    if (mdi.InputBox)
                    {
                        mdi.InputControl = new TextBox();
                        mdi.InputControl.BorderStyle = BorderStyle.FixedSingle;
                        mdi.InputControl.Font = new Font(FontFamily, mdi.FontSize, FontStyle.Regular);
                        if (mdi.SendButton && mdi.ClearButton)
                        {
                            mdi.InputControl.Width = form.ClientRectangle.Width - 2 * mdi.InputControl.Height - 4 * Margin;
                        }
                        else if (mdi.SendButton || mdi.ClearButton)
                        {
                            mdi.InputControl.Width = form.ClientRectangle.Width - mdi.InputControl.Height - 3 * Margin;
                        }
                        else
                        {
                            mdi.InputControl.Width = form.ClientRectangle.Width - 2 * Margin;
                        }
                        mdi.InputControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                        mdi.InputControl.Location = new Point(Margin, ypos);

                        form.Controls.Add(mdi.InputControl);

                        if (mdi.SendButton)
                        {
                            mdi.SendButtonControl = new Button();
                            mdi.SendButtonControl.UseVisualStyleBackColor = false;
                            mdi.SendButtonControl.ForeColor = mdi.SendButtonControl.ForeColor;
                            mdi.SendButtonControl.FlatStyle = FlatStyle.Flat;
                            mdi.SendButtonControl.TextAlign = ContentAlignment.MiddleCenter;
                            mdi.SendButtonControl.BackColor = Color.White;

                            mdi.SendButtonControl.Width = mdi.InputControl.Height;
                            mdi.SendButtonControl.Height = mdi.InputControl.Height;

                            if (mdi.ClearButton)
                            {
                                mdi.SendButtonControl.Location = new Point(form.ClientRectangle.Width - 2 * mdi.SendButtonControl.Width - 2 * Margin, ypos);
                            }
                            else
                            {
                                mdi.SendButtonControl.Location = new Point(form.ClientRectangle.Width - mdi.SendButtonControl.Width - Margin, ypos);
                            }

                            mdi.SendButtonControl.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                            mdi.SendButtonControl.Paint += new PaintEventHandler((s, ea) =>
                            {
                                ea.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                                int mg = mdi.SendButtonControl.Width / 6;

                                Point[] pl =
                                {
                                    new Point(mg, mg),
                                    new Point(mg, mdi.SendButtonControl.Height - mg),
                                    new Point(mdi.SendButtonControl.Width - mg, mdi.SendButtonControl.Width/2),
                                };
                                ea.Graphics.FillPolygon(new SolidBrush(Color.Black), pl);

                                ea.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                            });

                            form.Controls.Add(mdi.SendButtonControl);
                        }

                        if (mdi.ClearButton)
                        {
                            mdi.ClearButtonControl = new Button();
                            mdi.ClearButtonControl.UseVisualStyleBackColor = false;
                            mdi.ClearButtonControl.ForeColor = mdi.ClearButtonControl.ForeColor;
                            mdi.ClearButtonControl.FlatStyle = FlatStyle.Flat;
                            mdi.ClearButtonControl.TextAlign = ContentAlignment.MiddleCenter;
                            mdi.ClearButtonControl.BackColor = Color.White;

                            mdi.ClearButtonControl.Width = mdi.InputControl.Height;
                            mdi.ClearButtonControl.Height = mdi.InputControl.Height;

                            mdi.ClearButtonControl.Location = new Point(form.ClientRectangle.Width - mdi.ClearButtonControl.Width - Margin, ypos);

                            mdi.ClearButtonControl.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                            mdi.ClearButtonControl.Paint += new PaintEventHandler((s, ea) =>
                            {
                                ea.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                                int mg = mdi.SendButtonControl.Width / 6;

                                Point[] pl =
                                {
                                    new Point(mg, 2*mg),
                                    new Point(2*mg, mg),
                                    new Point(mdi.SendButtonControl.Width/2, mdi.SendButtonControl.Height/2 - mg),
                                    new Point(mdi.SendButtonControl.Width-2*mg, mg),
                                    new Point(mdi.SendButtonControl.Width-mg, 2*mg),
                                    new Point(mdi.SendButtonControl.Width/2 + mg, mdi.SendButtonControl.Height/2),
                                    new Point(mdi.SendButtonControl.Width - mg, mdi.SendButtonControl.Width - 2*mg),
                                    new Point(mdi.SendButtonControl.Width - 2*mg, mdi.SendButtonControl.Width - mg),
                                    new Point(mdi.SendButtonControl.Width/2, mdi.SendButtonControl.Height/2 + mg),
                                    new Point(2*mg, mdi.SendButtonControl.Height - mg),
                                    new Point(mg, mdi.SendButtonControl.Height - 2*mg),
                                    new Point(mdi.SendButtonControl.Width/2 - mg, mdi.SendButtonControl.Height/2),
                                };
                                ea.Graphics.FillPolygon(new SolidBrush(Color.Black), pl);

                                ea.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                            });

                            form.Controls.Add(mdi.ClearButtonControl);
                        }
                        ypos = ypos + mdi.InputControl.Height + Margin;
                    }

                    mdi.ConsoleControl = new TextBox();

                    mdi.ConsoleControl.WordWrap = false;
                    mdi.ConsoleControl.ScrollBars = ScrollBars.Both;

                    mdi.ConsoleControl.Multiline = true;
                    mdi.ConsoleControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    if (mdi.Docked && i == Elements.Count() - 1)
                    {
                        mdi.ConsoleControl.Height = form.ClientRectangle.Height - ypos - Margin;
                        mdi.ConsoleControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                    }
                    else if (mdi.LineNumber > 1)
                    {
                        mdi.ConsoleControl.Height *= (mdi.LineNumber + 1);
                    }

                    mdi.ConsoleControl.BorderStyle = BorderStyle.FixedSingle;
                    mdi.ConsoleControl.Font = new Font(FontFamily, mdi.FontSize, FontStyle.Regular);
                    mdi.ConsoleControl.Width = form.ClientRectangle.Width - 2 * Margin;
                    mdi.ConsoleControl.Location = new Point(Margin, ypos);

                    mdi.ConsoleControl.ReadOnly = true;
                    mdi.ConsoleControl.BackColor = Color.White;

                    form.Controls.Add(mdi.ConsoleControl);

                    ypos = ypos + mdi.ConsoleControl.Height + Margin;
                }
                else if (Elements[i].GetType() == typeof(ListElement))
                {
                    ListElement le = (ListElement)Elements[i];

                    le.ListViewControl = new ListViewNF();
                    le.ListViewControl.View = View.Details;
                    le.ListViewControl.Font = new Font(FontFamily, le.FontSize, FontStyle.Regular);

                    le.ListViewControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    if (le.Docked && i == Elements.Count() - 1)
                    {
                        le.ListViewControl.Height = form.ClientRectangle.Height - ypos - Margin;
                        le.ListViewControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                    }
                    else
                    {
                        le.ListViewControl.Height = (int)(le.FontSize * 2 * (le.LineNumber + 1 + 0.1));
                    }

                    le.ListViewControl.Width = form.ClientRectangle.Width - 2 * Margin;
                    le.ListViewControl.Location = new Point(Margin, ypos);

                    foreach (string str in le.ColumnHeaders)
                    {
                        le.ListViewControl.Columns.Add(str);
                    }

                    form.Controls.Add(le.ListViewControl);

                    ypos = ypos + le.ListViewControl.Height + Margin;
                }
                else if (Elements[i].GetType() == typeof(GridElement))
                {
                    GridElement ge = (GridElement)Elements[i];

                    ge.PropertyGridControl = new PropertyGrid();
                    ge.PropertyGridControl.LineColor = Color.White;
                    ge.PropertyGridControl.ToolbarVisible = false;
                    ge.PropertyGridControl.PropertySort = PropertySort.Alphabetical;
                    ge.PropertyGridControl.HelpVisible = false;
                    ge.PropertyGridControl.Font = new Font(FontFamily, ge.FontSize, FontStyle.Regular);

                    ge.PropertyGridControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    if (ge.Docked && i == Elements.Count() - 1)
                    {
                        ge.PropertyGridControl.Height = form.ClientRectangle.Height - ypos - Margin;
                        ge.PropertyGridControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                    }
                    else
                    {
                    }

                    ge.PropertyGridControl.Width = form.ClientRectangle.Width - 2 * Margin;
                    ge.PropertyGridControl.Location = new Point(Margin, ypos);

                    form.Controls.Add(ge.PropertyGridControl);

                    ypos = ypos + ge.PropertyGridControl.Height + Margin;
                }
            }
        }
        #endregion
    }

    public class Element
    {
        #region fields
        public int PosY;
        #endregion
    }

    public class ReportElement : Element
    {
        #region fields
        public string Label = "LABEL";

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (ValueControl != null) { ValueControl.Text = _value; }
            }
        }

        public float FontSize = 16f;
        public int LabelWidth = 35;
        public Color BackColorLabel = Color.White;
        public Color BackColorValue = Color.Black;
        public Color ForeColorLabel = Color.Black;
        public Color ForeColorValue = Color.White;

        public Label LabelControl;
        public Label ValueControl;
        #endregion

        #region constructor
        public ReportElement(string label, string value, float size, int labelWidth,
            Color backColorLabel, Color foreColorLabel,
            Color backColorValue, Color foreColorValue)
        {
            this.Label = label;
            this.Value = value;
            this.FontSize = size;
            this.LabelWidth = labelWidth;
            this.BackColorLabel = backColorLabel;
            this.BackColorValue = backColorValue;
            this.ForeColorLabel = foreColorLabel;
            this.ForeColorValue = foreColorValue;
        }
        #endregion
    }

    public class TrackElement : Element
    {
        #region fields
        public string Label = "LABEL";
        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                if (ValueControl != null) 
                { 
                    ValueControl.Text = _value; 
                }
            }
        }
        public float FontSize = 16f;
        public int LabelWidth = 35;
        public Color BackColorLabel = Color.White;
        public Color BackColorValue = Color.Black;
        public Color BackColorSelection = Color.Blue;
        public Color ForeColorLabel = Color.Black;
        public Color ForeColorValue = Color.White;
        public Color ForeColorSelection = Color.Black;

        public Label LabelControl;
        public Label ValueControl;
        public Track TrackControl = null;

        public bool CanCheck = false;
        private bool _checked;
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (CanCheck && _checked != value)
                {
                    _checked = value;

                    if (LabelControl != null)
                    {
                        if (_checked)
                        {
                            LabelControl.ForeColor = ForeColorSelection;
                            LabelControl.BackColor = BackColorSelection;
                        }
                        else
                        {
                            LabelControl.ForeColor = ForeColorLabel;
                            LabelControl.BackColor = BackColorLabel;
                        }
                    }

                    CheckChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        #endregion

        #region events
        public event EventHandler CheckChanged;
        #endregion

        #region constructor
        public TrackElement(string label, string format, float size, int labelWidth,
            Color backColorLabel, Color foreColorLabel,
            Color backColorValue, Color foreColorValue,
            Color backColorSelection, Color foreColorSelection,
            Track tb,
            bool cancheck, bool check)
        {
            this.Label = label;
            this.Value = tb.CurrentValue.ToString(format);
            this.FontSize = size;
            this.LabelWidth = labelWidth;
            this.BackColorLabel = backColorLabel;
            this.BackColorValue = backColorValue;
            this.BackColorSelection = backColorSelection;
            this.ForeColorLabel = foreColorLabel;
            this.ForeColorValue = foreColorValue;
            this.ForeColorSelection = foreColorSelection;

            this.CanCheck = cancheck;
            if (cancheck) { this.Checked = check; }

            TrackControl = tb;

            tb.CurrentValueChanged += new EventHandler((s, ea) =>
            {
                this.Value = tb.CurrentValue.ToString(format);
            });
        }
        #endregion
    }

    public class ConsoleElement : Element
    {
        #region fields
        public bool InputBox;
        public bool SendButton;
        public bool ClearButton;

        public bool Docked;
        public float FontSize = 16f;
        public int LineNumber = 10;

        public Button SendButtonControl;
        public Button ClearButtonControl;
        public TextBox InputControl;
        public TextBox ConsoleControl;
        #endregion

        #region constructors
        public ConsoleElement(bool inputBox, bool sendButton, bool clearButton, bool docked, float fontSize, int lineNb = 10)
        {
            this.InputBox = inputBox;
            this.SendButton = sendButton;
            this.ClearButton = clearButton;

            this.Docked = docked;

            this.FontSize = fontSize;
            this.LineNumber = lineNb;
        }
        #endregion
    }

    public class ListElement : Element
    {
        #region fields
        public List<string> ColumnHeaders;

        public bool Docked;
        public float FontSize = 16f;
        public int LineNumber = 10;

        public ListViewNF ListViewControl;
        #endregion

        #region constructors
        public ListElement(List<string> columnHeaders, bool docked, float fontSize, int lineNb = 10)
        {
            this.ColumnHeaders = columnHeaders;
            this.Docked = docked;
            this.FontSize = fontSize;
            this.LineNumber = lineNb;
        }
        #endregion
    }

    public class PadElement : Element
    {
        #region fields
        public float FontSize = 16f;
        public List<PadButton> ButtonElements = new List<PadButton>();
        public int Width;
        public int Height;
        public bool CenterAlign;
        #endregion

        #region constructor
        public PadElement(float size, bool centerAlign = true)
        {
            this.FontSize = size;
            this.CenterAlign = centerAlign;
        }
        #endregion

        #region methods
        public void GetSize(int margin)
        {
            int nx = 0;
            int ny = 0;
            foreach (PadButton be in this.ButtonElements)
            {
                if (be.X > nx)
                {
                    nx = be.X;
                }
                if (be.Y > ny)
                {
                    ny = be.Y;
                }
            }
            nx++;
            ny++;

            Width = (int)(3.0 * FontSize) * nx + (nx - 1) * margin;
            Height = (int)(3.0 * FontSize) * ny + (ny - 1) * margin;
        }
        #endregion
    }

    public class OptionSelectionElement : Element
    {
        public List<string> Options;
        public string Label = "LABEL";

        public float FontSize = 16f;
        public int LabelWidth = 35;

        public Color BackColorLabel = Color.White;
        public Color BackColorValue = Color.Black;
        public Color BackColorSelection = Color.Blue;
        public Color ForeColorLabel = Color.Black;
        public Color ForeColorValue = Color.White;
        public Color ForeColorSelection = Color.Black;

        public Label LabelControl;
        public List<Label> ValueControls = new List<Label>();

        private int _selection = 0;
        public int Selection
        {
            get { return _selection; }
            set
            {
                if (_selection != value)
                {
                    _selection = value;

                    for (int k = 0; k < Options.Count(); k++)
                    {
                        if (LabelControl != null)
                        {
                            if (k == _selection)
                            {
                                ValueControls[k].ForeColor = ForeColorSelection;
                                ValueControls[k].BackColor = BackColorSelection;
                            }
                            else
                            {
                                ValueControls[k].ForeColor = ForeColorValue;
                                ValueControls[k].BackColor = BackColorValue;
                            }
                        }
                    }

                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Selection, Options[Selection]));
                }
            }
        }

        #region events
        public event SelectionChangedEventHandler SelectionChanged;

        public delegate void SelectionChangedEventHandler(object source, SelectionChangedEventArgs e);

        public class SelectionChangedEventArgs : EventArgs
        {
            private int SelectionIndex;

            private string Selection;

            public SelectionChangedEventArgs(int id, string sel)
            {
                SelectionIndex = id;
                Selection = sel;
            }
            public int GetSelectionIndex()
            {
                return SelectionIndex;
            }

            public string GetSelection()
            {
                return Selection;
            }
        }
        #endregion

        #region constructor
        public OptionSelectionElement(string label, List<string> opts, int sel, float size, int labelWidth,
            Color backColorLabel, Color foreColorLabel,
            Color backColorValue, Color foreColorValue,
            Color backColorSelection, Color foreColorSelection)
        {
            this.Label = label;
            this.Options = opts;

            this.Selection = sel;
            this.FontSize = size;

            this.LabelWidth = labelWidth;
            this.BackColorLabel = backColorLabel;
            this.BackColorValue = backColorValue;
            this.BackColorSelection = backColorSelection;
            this.ForeColorLabel = foreColorLabel;
            this.ForeColorValue = foreColorValue;
            this.ForeColorSelection = foreColorSelection;
        }
        #endregion
    }

    public class GridElement : Element
    {
        #region fields
        public bool Docked;
        public float FontSize = 16f;
        public int LineNumber = 10;

        public IDictionary ObjectList = new Hashtable();

        public PropertyGrid PropertyGridControl;
        #endregion

        #region constructors
        public GridElement(bool docked, float fontSize, int lineNb = 10)
        {
            this.Docked = docked;

            this.FontSize = fontSize;
            this.LineNumber = lineNb;
        }
        #endregion

        #region methods
        public void Refresh()
        {
            PropertyGridControl.SelectedObject = new DictionaryPropertyGridAdapter(ObjectList);
        }
        #endregion

        #region
        class DictionaryPropertyGridAdapter : ICustomTypeDescriptor
        {
            IDictionary _dictionary;

            public DictionaryPropertyGridAdapter(IDictionary d)
            {
                _dictionary = d;
            }

            public string GetComponentName()
            {
                return TypeDescriptor.GetComponentName(this, true);
            }

            public EventDescriptor GetDefaultEvent()
            {
                return TypeDescriptor.GetDefaultEvent(this, true);
            }

            public string GetClassName()
            {
                return TypeDescriptor.GetClassName(this, true);
            }

            public EventDescriptorCollection GetEvents(Attribute[] attributes)
            {
                return TypeDescriptor.GetEvents(this, attributes, true);
            }

            EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents()
            {
                return TypeDescriptor.GetEvents(this, true);
            }

            public TypeConverter GetConverter()
            {
                return TypeDescriptor.GetConverter(this, true);
            }

            public object GetPropertyOwner(PropertyDescriptor pd)
            {
                return _dictionary;
            }

            public AttributeCollection GetAttributes()
            {
                return TypeDescriptor.GetAttributes(this, true);
            }

            public object GetEditor(Type editorBaseType)
            {
                return TypeDescriptor.GetEditor(this, editorBaseType, true);
            }

            public PropertyDescriptor GetDefaultProperty()
            {
                return null;
            }

            PropertyDescriptorCollection
                System.ComponentModel.ICustomTypeDescriptor.GetProperties()
            {
                return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
            }

            public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                ArrayList properties = new ArrayList();
                foreach (DictionaryEntry e in _dictionary)
                {
                    properties.Add(new DictionaryPropertyDescriptor(_dictionary, e.Key));
                }

                PropertyDescriptor[] props =
                    (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

                return new PropertyDescriptorCollection(props);
            }
        }

        class DictionaryPropertyDescriptor : PropertyDescriptor
        {
            IDictionary _dictionary;
            object _key;

            internal DictionaryPropertyDescriptor(IDictionary d, object key)
                : base(key.ToString(), null)
            {
                _dictionary = d;
                _key = key;
            }

            public override Type PropertyType
            {
                get { return _dictionary[_key].GetType(); }
            }

            public override void SetValue(object component, object value)
            {
                _dictionary[_key] = value;
            }

            public override object GetValue(object component)
            {
                return _dictionary[_key];
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override Type ComponentType
            {
                get { return null; }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override void ResetValue(object component)
            {
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }
        #endregion
    }

    public class PadButton : Button
    {
        #region fields
        public int X = 0;
        public int Y = 0;
        public float FontScale = 1f;
        public bool CanCheck = false;
        private bool _checked;
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (CanCheck && _checked != value)
                {
                    _checked = value;

                    Color tmpcolor = ForeColor;
                    ForeColor = BackColor;
                    BackColor = tmpcolor;
                }
            }
        }
        #endregion

        #region constructor
        public PadButton(string text, float fontscale, int x, int y, Color col, bool cancheck = false, bool check = false)
        {
            this.FontScale = fontscale;
            this.Text = text;
            this.X = x;
            this.Y = y;
            this.Name = x.ToString() + ";" + y.ToString();
            this.ForeColor = col;
            this.CanCheck = cancheck;
            if (cancheck) { this.Checked = check; }
        }
        #endregion
    }

    public partial class Track : UserControl
    {
        private double _CurrentValue;
        private bool _IsDragging;
        private double _MaximumValue;
        private double _MinimumValue;
        private double _Interval;

        public Track(Color trackColor, Color backColor, int height, int trackwidth, int min, int max, int interval)
        {
            TrackColor = trackColor;
            BackColor = backColor;
            Height = height;
            TrackWidth = trackwidth;
            MinimumValue = min;
            MaximumValue = max;
            Interval = interval;
            CurrentValue = MinimumValue;
            HotTrackEnabled = true;
        }

        public event EventHandler CurrentValueChanged;

        public double CurrentValue
        {
            get => _CurrentValue;
            set
            {
                _CurrentValue = value;
                ValidateCurrentValue();
                Invalidate();
                RaiseEvent(CurrentValueChanged);
            }
        }

        public bool HotTrackEnabled { get; set; }

        public double MaximumValue
        {
            get => _MaximumValue;
            set
            {
                _MaximumValue = value;

                if (_MaximumValue < _MinimumValue)
                    _MaximumValue = _MinimumValue;

                ValidateCurrentValue();
                Invalidate();
            }
        }

        public double MinimumValue
        {
            get => _MinimumValue;
            set
            {
                _MinimumValue = value;

                if (_MinimumValue > _MaximumValue)
                    _MinimumValue = _MaximumValue;

                ValidateCurrentValue();
                Invalidate();
            }
        }

        public double Interval
        {
            get => _Interval;
            set
            {
                _Interval = Math.Abs(value);

                if (_Interval > _MaximumValue - _MinimumValue)
                    _Interval = _MaximumValue - _MinimumValue;

                ValidateCurrentValue();
                Invalidate();
            }
        }

        public Color TrackColor { get; set; }

        public int TrackWidth { get; set; }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            UpdateCurrentValueFromPosition(e.X);

            if (HotTrackEnabled)
                _IsDragging = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_IsDragging)
            {
                UpdateCurrentValueFromPosition(e.X);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _IsDragging = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var brush = new SolidBrush(TrackColor))
            {
                e.Graphics.FillRectangle(brush, CreateRectangle());
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private RectangleF CreateRectangle()
        {
            var position = GetRectanglePosition();
            var rectangle = new RectangleF((float)position, 0, TrackWidth, Height);
            return rectangle;
        }

        private double GetRectanglePosition()
        {
            var range = _MaximumValue - _MinimumValue;
            var value = _CurrentValue - _MinimumValue;
            var percentage = value * 100 / range;
            var position = percentage * Width / 100;

            return position - TrackWidth / 2;
        }

        private void RaiseEvent(EventHandler handler)
        {
            handler?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCurrentValueFromPosition(float x)
        {
            var percentage = x * 100 / Width;
            var range = _MaximumValue - _MinimumValue;
            var rawValue = percentage * range / 100;
            var value = rawValue + _MinimumValue;

            value = Math.Round(value / _Interval) * _Interval;

            CurrentValue = value;
        }

        private void ValidateCurrentValue()
        {
            if (_CurrentValue < _MinimumValue)
                _CurrentValue = _MinimumValue;

            if (_CurrentValue > _MaximumValue)
                _CurrentValue = _MaximumValue;

            _CurrentValue = Math.Round(_CurrentValue / _Interval) * _Interval;
        }
    }

    public class ListViewNF : ListView
    {
        public ListViewNF()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }

        protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected) { e.Item.Selected = false; }
        }
    }
}
