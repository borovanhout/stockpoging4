using NodaTime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using YahooQuotesApi;

namespace stockpoging4
{
    public partial class Form1 : Form
    {
        YahooQuotes yahooQuotes = new YahooQuotesBuilder().HistoryStarting(Instant.FromUtc(DateTime.Now.Year - 1, DateTime.Now.Month, DateTime.Now.Day, 0, 0)).Build();

        Task _runningTask;
        CancellationTokenSource _cancellationToken;
        int tickerCounter = 1;
        int changeRowCounter = 1;
        int changeColumnCounter = 2;
        int changeLabelCounter = 0;
        int tableLayoutPanelCounter = 1;
        List<string> tickerList = new List<string>() { "USDEUR=X", "GBPEUR=X" };
        List<List<object>> positionList = new List<List<object>>();
        List<string> currencyList = new List<string>() { "USDEUR=X", "GBPEUR=X" };

        public Form1()
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            InitializeComponent();
            FillMeta();
            this.BackColor = Color.Brown;
            this.TransparencyKey = BackColor;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            e.Graphics.DrawString(" ", Font, new SolidBrush(ForeColor), 0, 0);
        }
        /// <summary>
        /// Add ticker functions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button1_Click(object sender, EventArgs e)
        {
            string ticker;
            using (Prompt prompt = new Prompt("Enter the ticker symbol", "Add ticker"))
            {
                ticker = prompt.Result;
                ticker = ticker.ToUpper();
                if (!string.IsNullOrEmpty(ticker))
                {

                    using (Prompt prompt2 = new Prompt("Enter your volume", "Add ticker"))
                    {
                        if (Int32.TryParse(prompt2.Result, out int volume) == true)
                        {
                            using (Prompt prompt3 = new Prompt("Enter your buy price", "Add ticker"))
                            {
                                if (Double.TryParse(prompt3.Result, out double buyPrice) == true)
                                {
                                    try
                                    {
                                        tableLayoutPanel1.SuspendLayout();
                                        tickerList.Add(ticker);
                                        string[] lastTicker = new string[1] { ticker };

                                        var priceInfo = await GetStockPrices(lastTicker);
                                        FillTickerLabel(ticker);

                                        List<object> priceInfoLastTicker = priceInfo[0];

                                        List<object> lastPositionInfo = new List<object>() { volume, buyPrice, ticker };
                                        positionList.Add(lastPositionInfo);

                                        List<object> positionInfo = GetPositionVars(ticker, volume, buyPrice, Convert.ToDouble(priceInfoLastTicker[1]));
                                        FillPositionLabel(ticker, priceInfoLastTicker[0].ToString(), positionInfo);

                                        List<object> changeInfo = GetChangeVars(priceInfoLastTicker);
                                        FillChangeLabels(ticker, priceInfoLastTicker[0].ToString(), changeInfo);

                                        if (tickerList.Count <= 1)
                                        {
                                            _cancellationToken = new CancellationTokenSource();
                                            _runningTask = StartTimer(() => KeepUpdatingEverything(tickerList), _cancellationToken);
                                        }
                                        tableLayoutPanel1.ResumeLayout();
                                        tableLayoutPanel1.PerformLayout();
                                    }
                                    catch
                                    {
                                        MessageBox.Show("Ticker does not exist, or entered incorrect value somewhere else");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("You did not enter one of the textboxes correctly");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("You did not enter one of the textboxes correctly");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("You did not enter one of the textboxes correctly");
                }
            }
        }
        /// <summary>
        /// Fill functions for add-ticker function/load function
        /// </summary>
        /// 
        private void FillMeta()
        {
            string[] meta = { "Ticker", "Current position", "Change 1d", "Change 7d", "Change 30d", "Change 90d", "Change 180d", "Change 1y" };
            int metaCounter = 0;
            foreach (string str in meta)
            {
                Label label = new Label() { Name = str, Text = str, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(Label.DefaultFont, FontStyle.Bold), ForeColor = Color.White };
                tableLayoutPanel1.Controls.Add(label, metaCounter, 0);
                metaCounter++;
            }
        }

        private void FillTickerLabel(string ticker)
        {
            Label label = new Label() { Name = ticker, Text = ticker, Tag = ticker, TextAlign = ContentAlignment.MiddleCenter, AutoScrollOffset = new Point(0, 0), ForeColor = Color.White };
            tableLayoutPanel1.RowCount++;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.Controls.Add(label, 0, tickerCounter);
            tickerCounter++;
        }

        private void FillPositionLabel(string ticker, string currency, List<object> positionInfo)
        {
            TableLayoutPanel newTableLayoutPanel = new TableLayoutPanel();
            newTableLayoutPanel = CreateNewTableLayoutPanel(newTableLayoutPanel, ticker);
            FillPositionLeft(Convert.ToInt32(positionInfo[0]), currency, Convert.ToDouble(positionInfo[2]), ticker, Convert.ToDouble(positionInfo[3]), newTableLayoutPanel);
            FillPositionRight(currency, Convert.ToDouble(positionInfo[1]), Convert.ToDouble(positionInfo[4]), Convert.ToDouble(positionInfo[5]), ticker, newTableLayoutPanel);
        }

        private void FillPositionLeft(int volume, string currency, double currentPrice, string ticker, double currentPosition, TableLayoutPanel newTableLayoutPanel)
        {
            Label positionLabel = new Label() { Name = ticker + "TotalPos", Text = currency + Convert.ToString(Math.Round(currentPosition, 2)), Tag = ticker, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 9), ForeColor = Color.White };
            Label metaLabel = new Label() { Name = ticker + "ValuePricePos", Text = Convert.ToString(volume) + " x " + currency + Convert.ToString(currentPrice), Tag = ticker, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 7), ForeColor = Color.White };
            newTableLayoutPanel.Controls.Add(positionLabel, 0, 0);
            newTableLayoutPanel.Controls.Add(metaLabel, 0, 1);
        }

        private void FillPositionRight(string currency, double buyPrice, double originalPosition, double totalChange, string ticker, TableLayoutPanel newTableLayoutPanel)
        {
            Label positionLabel = new Label() { Name = ticker + "TotalChange", Tag = ticker, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 9), ForeColor = Color.White };
            Label metaLabel = new Label() { Name = ticker + "OldValue", Tag = ticker, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 7), AutoSize = true, ForeColor = Color.White };

            if (totalChange > 0)
            {
                positionLabel.Text = "+" + currency + Convert.ToString(Math.Round(totalChange, 2));
                positionLabel.ForeColor = Color.Green;
                metaLabel.Text = "(" + currency + Convert.ToString(Math.Round(buyPrice, 2)) + ", " + currency + Convert.ToString(Math.Round(originalPosition, 2)) + ")";
                metaLabel.ForeColor = Color.Green;
            }
            else
            {
                totalChange = Math.Round(totalChange, 2, MidpointRounding.AwayFromZero);
                positionLabel.Text = "-" + currency + totalChange.ToString().Remove(0, 1);
                positionLabel.ForeColor = Color.Red;
                metaLabel.Text = "(" + currency + Convert.ToString(Math.Round(buyPrice, 2)) + ", " + currency + Convert.ToString(Math.Round(originalPosition, 2)) + ")";
                metaLabel.ForeColor = Color.Red;
            }
            newTableLayoutPanel.Controls.Add(positionLabel, 1, 0);
            newTableLayoutPanel.Controls.Add(metaLabel, 1, 1);
        }

        private void FillChangeLabels(string ticker, string currency, List<object> changeNumbers)
        {
            for (int i = 0; i < changeNumbers.Count; i++)
            {
                TableLayoutPanel newTableLayoutPanel = CreateNewChangeTableLayoutPanel(ticker, changeNumbers[i].ToString(), changeNumbers[i + 1].ToString());
                Label changeAbsolute = new Label();
                Label changePercentage = new Label();
                if (Convert.ToDouble(changeNumbers[i]) < 0)
                {
                    changeAbsolute = new Label() { Name = ticker + "ChangeAbs" + changeLabelCounter, Text = changeNumbers[i].ToString(), Tag = ticker, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 9), AutoSize = true, ForeColor = Color.Red };
                    changePercentage = new Label() { Name = ticker + "ChangePerc" + changeLabelCounter, Text = "(" + changeNumbers[i + 1].ToString() + "%)", Tag = ticker, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 7), AutoSize = true, ForeColor = Color.Red };
                }
                else
                {
                    changeAbsolute = new Label() { Name = ticker + "ChangeAbs" + changeLabelCounter, Text = "+" + changeNumbers[i].ToString(), Tag = ticker, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 9), AutoSize = true, ForeColor = Color.Green };
                    changePercentage = new Label() { Name = ticker + "ChangePerc" + changeLabelCounter, Text = "(+" + changeNumbers[i + 1].ToString() + "%)", Tag = ticker, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 7), AutoSize = true, ForeColor = Color.Green };
                }
                changeLabelCounter++;
                newTableLayoutPanel.Controls.Add(changeAbsolute, 0, 0);
                newTableLayoutPanel.Controls.Add(changePercentage, 1, 0);
                i++;
            }
            changeLabelCounter = 0;
            changeRowCounter++;
            changeColumnCounter = 2;
        }


        /// <summary>
        /// Helper functions to do various things
        /// </summary>
        /// <param name="newTableLayoutPanel"></param>
        /// <param name="ticker"></param>
        /// <returns></returns>
        private TableLayoutPanel CreateNewTableLayoutPanel(TableLayoutPanel newTableLayoutPanel, string ticker)
        {
            tableLayoutPanel1.Controls.Add(newTableLayoutPanel, 1, tableLayoutPanelCounter);
            newTableLayoutPanel.Name = ticker + "innerTableLayoutPanel";
            newTableLayoutPanel.AutoSize = true;
            newTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            newTableLayoutPanel.ColumnCount = 2;
            newTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            newTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            newTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            newTableLayoutPanel.Location = new System.Drawing.Point(136, 25);
            newTableLayoutPanel.RowCount = 2;
            newTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            newTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanelCounter++;
            return newTableLayoutPanel;
        }

        private TableLayoutPanel CreateNewChangeTableLayoutPanel(string ticker, string changeAbsolute, string changePerc)
        {
            TableLayoutPanel changeTableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel1.Controls.Add(changeTableLayoutPanel, changeColumnCounter, changeRowCounter);
            changeTableLayoutPanel.Name = ticker + "changeTableLayoutPanel" + changeColumnCounter;
            changeTableLayoutPanel.AutoSize = true;
            changeTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            changeTableLayoutPanel.ColumnCount = 2;
            changeTableLayoutPanel.Dock = DockStyle.Fill;
            changeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            changeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            changeTableLayoutPanel.RowCount = 1;
            changeTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            changeColumnCounter++;

            return changeTableLayoutPanel;
        }

        private List<object> GetPositionVars(string ticker, double volume, double buyPrice, double currentPrice)
        {
            double currentPosition = volume * currentPrice;
            double originalPosition = volume * buyPrice;
            double totalChange = (currentPosition - originalPosition);
            List<object> list = new List<object>() { volume, buyPrice, currentPrice, currentPosition, originalPosition, totalChange };
            return list;
        }

        private List<object> GetChangeVars(List<object> priceInfo)
        {
            double change1D = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[2]), 3);
            string change1DPerc = Math.Round(change1D / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            double change7D = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[3]), 3);
            string change7DPerc = Math.Round(change7D / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            double change30D = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[4]), 3);
            string change30DPerc = Math.Round(change30D / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            double change90D = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[5]), 3);
            string change90DPerc = Math.Round(change90D / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            double change180D = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[6]), 3);
            string change180DPerc = Math.Round(change180D / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            double change1Y = Math.Round(Convert.ToDouble(priceInfo[1]) - Convert.ToDouble(priceInfo[7]), 3);
            string change1YPerc = Math.Round(change1Y / Convert.ToDouble(priceInfo[1]) * 100, 2).ToString();

            List<object> list = new List<object> { change1D, change1DPerc, change7D, change7DPerc, change30D, change30DPerc, change90D, change90DPerc, change180D, change180DPerc, change1Y, change1YPerc };
            return list;
        }

        /// <summary>
        /// Main tasks that gets the info from the API
        /// </summary>
        /// 
        //private TableLayoutPanel CreateChangeNewTableLayoutPanel(TableLayoutPanel changeTableLayoutPanel)
        public async Task<List<List<object>>> GetStockPrices(string[] tickerList)
        {
            List<List<object>> priceInfo = new List<List<object>>();
            Dictionary<string, Security?> securities = await yahooQuotes.GetAsync(tickerList, HistoryFlags.PriceHistory);
            for (int i = 0; i < tickerList.Count(); i++)
            {
                    double? openPrice1D = null;
                    double? openPrice7D = null;
                    double? openPrice30D = null;
                    double? openPrice90D = null;
                    double? openPrice180D = null;
                    double? openPrice1Y = null;

                    Security? security = securities[tickerList[i]];
                    string? currency = security.Currency;
                    switch (currency)
                    {
                        case "USD":
                            currency = "$";
                            break;

                        case "EUR":
                            currency = "€";
                            break;

                        case "GBp":
                            currency = "£p";
                            break;

                    }
                    decimal? currentPrice = security.RegularMarketPrice;
                    var priceHistory = security.PriceHistory.Value.Reverse().ToArray();
                    int? index = priceHistory.Count<PriceTick>();

                    if (index > 0)
                    {
                        openPrice1D = Convert.ToDouble(priceHistory[0].Open);
                    }
                    if (index > 4)
                    {
                        openPrice7D = Convert.ToDouble(priceHistory[4].Open);
                    }
                    if (index > 19)
                    {
                        openPrice30D = Convert.ToDouble(priceHistory[19].Open);
                    }
                    if (index > 62)
                    {
                        openPrice90D = Convert.ToDouble(priceHistory[62].Open);
                    }
                    if (index > 126)
                    {
                        openPrice180D = Convert.ToDouble(priceHistory[126].Open);
                    }
                    if (index > 250)
                    {
                        openPrice1Y = Convert.ToDouble(priceHistory[250].Open);
                    }
                    List<object> securityInfo = new List<object>() { currency, currentPrice, openPrice1D, openPrice7D, openPrice30D, openPrice90D, openPrice180D, openPrice1Y, security.Symbol.Name };
                    priceInfo.Add(securityInfo);
            }
            return priceInfo;
        }

        public async Task<List<List<double>>> GetCurrencyPrices()
        {
            List<List<double>> currencyInfo = new List<List<double>>();
            Dictionary<string, Security?> securities = await yahooQuotes.GetAsync(currencyList);
            for (int i = 0; i < currencyList.Count(); i++)
            {
                Security? security = securities[currencyList[i]];
                double currentPrice = (double)security.RegularMarketPrice;
                List<double> securityInfo = new List<double>() { currentPrice };
                currencyInfo.Add(securityInfo);
            }
            return currencyInfo;
        }

        /// <summary>
        /// Main task that keeps updating everything
        /// </summary>
        public async void KeepUpdatingEverything(List<string> tickerList)
        {
            int j = 0;

            var currencyInfo =  await GetCurrencyPrices();
            var allPriceInfo = await GetStockPrices(tickerList.ToArray());
            foreach (List<object> priceInfo in allPriceInfo)
            {
                List<object> changeInfo = GetChangeVars(priceInfo);
                List<object> positionInfo = GetPositionVars(priceInfo[8].ToString(), Convert.ToDouble(positionList[j][0]), Convert.ToDouble(positionList[j][1]), Convert.ToDouble(priceInfo[1]));
                Control controlToFindRow = tableLayoutPanel1.Controls[priceInfo[8].ToString()];
                int correctRow = tableLayoutPanel1.GetRow(controlToFindRow);
                for (int i = 1; i < 3; i++)
                {
                    switch (i)
                    {
                        case 1:
                            TableLayoutPanel panelPos = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(i, correctRow);

                            Label totalPos = (Label)panelPos.GetControlFromPosition(0, 0);
                            totalPos.Text = priceInfo[0] + Math.Round(Convert.ToDouble(positionInfo[3]), 2).ToString();

                            Label valuePricePos = (Label)panelPos.GetControlFromPosition(0, 1);
                            valuePricePos.Text = Convert.ToString(positionInfo[0]) + " x " + priceInfo[0] + Convert.ToString(priceInfo[1]);

                            Label totalChange = (Label)panelPos.GetControlFromPosition(1, 0);
                            Label oldValuePos = (Label)panelPos.GetControlFromPosition(1, 1);

                            double totalChangeNumber = Math.Round(Convert.ToDouble(positionInfo[5]), 2);
                            if (totalChangeNumber > 0)
                            {
                                totalChange.Text = "+" + priceInfo[0].ToString() + Convert.ToString(totalChangeNumber);
                                totalChange.ForeColor = Color.Green;
                                oldValuePos.ForeColor = Color.Green;
                            }
                            else
                            {
                                totalChange.Text = "-" + priceInfo[0] + totalChangeNumber.ToString().Remove(0, 1);
                                totalChange.ForeColor = Color.Red;
                                oldValuePos.ForeColor = Color.Red;
                            }
                            break;

                        case 2:
                            int k = 0;
                            for (int h = 2; h < 7; h++)
                            {
                                TableLayoutPanel changePanel = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(h, correctRow);
                                Label changeAbs = (Label)changePanel.GetControlFromPosition(0, 0);
                                Label changePerc = (Label)changePanel.GetControlFromPosition(1, 0);
                                if (Convert.ToInt32(changeInfo[k]) > 0)
                                {
                                    changeAbs.Text = "+" + changeInfo[k].ToString();
                                    changePerc.Text = "(+" + changeInfo[k + 1].ToString() + "%)";
                                    changeAbs.ForeColor = Color.Green;
                                    changePerc.ForeColor = Color.Green;
                                }
                                else
                                {
                                    changeAbs.Text = changeInfo[k].ToString();
                                    changePerc.Text = "(" + changeInfo[k + 1].ToString() + "%)";
                                    changeAbs.ForeColor = Color.Red;
                                    changePerc.ForeColor = Color.Red;
                                }
                                k++;
                                k++;
                            }
                            break;
                    }
                }
                j++;
            }
        }

        /// <summary>
        /// Save/Load related functions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void form_Load(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("tickers.xml");
                XmlNodeList tickers = doc.SelectNodes("//Ticker");
                XmlNodeList buyPrices = doc.SelectNodes("//BuyPrice");
                XmlNodeList volumes = doc.SelectNodes("//Volume");
                this.SuspendLayout();
                for (int i = 0; i < tickers.Count; i++)
                {
                    tickerList.Add(tickers[i].InnerText);
                    FillTickerLabel(tickers[i].InnerText);

                    var priceInfo = await GetStockPrices(new string[] { tickers[i].InnerText }) ;
                    List<object> priceInfoLastTicker = priceInfo[0];

                    List<object> lastPositionInfo = new List<object>() { volumes[i].InnerText, buyPrices[i].InnerText, tickers[i].InnerText };
                    positionList.Add(lastPositionInfo);

                    List<object> positionInfo = GetPositionVars(tickers[i].InnerText, Convert.ToDouble(volumes[i].InnerText), Convert.ToDouble(buyPrices[i].InnerText), Convert.ToDouble(priceInfoLastTicker[1]));
                    FillPositionLabel(tickers[i].InnerText, priceInfoLastTicker[0].ToString(), positionInfo);

                    List<object> changeInfo = GetChangeVars(priceInfoLastTicker);
                    FillChangeLabels(tickers[i].InnerText, priceInfoLastTicker[0].ToString(), changeInfo);
                }
                this.ResumeLayout();
                this.PerformLayout();
                _cancellationToken = new CancellationTokenSource();
                _runningTask = StartTimer(() => KeepUpdatingEverything(tickerList), _cancellationToken);
            }
            catch
            {
                MessageBox.Show("No existing file found");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int forCount = tableLayoutPanel1.RowCount;
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("", "Body", "");
            doc.AppendChild(root);

            XmlElement tickerElement1 = doc.CreateElement("", "TickerCollection", "");
            root.AppendChild(tickerElement1);
            for (int i = 1; i < forCount; i++)
            {
                Label ticker = (Label)tableLayoutPanel1.GetControlFromPosition(0, i);
                XmlElement tickerElement2 = doc.CreateElement("", "Ticker", "");
                tickerElement2.InnerText = ticker.Text;
                tickerElement1.AppendChild(tickerElement2);
            }
            XmlElement positionElement1 = doc.CreateElement("", "VolumeCollection", "");
            root.AppendChild(positionElement1);
            for (int i = 1; i < forCount; i++)
            {
                TableLayoutPanel panel = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(1, i);
                Label volumeLabel = (Label)panel.GetControlFromPosition(0, 1);
                string[] textSplit = volumeLabel.Text.Split(" ");
                XmlElement positionElement2 = doc.CreateElement("", "Volume", "");
                positionElement2.InnerText = textSplit[0];
                positionElement1.AppendChild(positionElement2);
            }
            XmlElement priceElement1 = doc.CreateElement("", "BuyPriceCollection", "");
            root.AppendChild(priceElement1);
            for (int i = 1; i < forCount; i++)
            {
                TableLayoutPanel panel = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(1, i);
                Label buyPriceLabel = (Label)panel.GetControlFromPosition(1, 1);
                string[] textSplit = buyPriceLabel.Text.Split(" ");
                string buyPrice = textSplit[0].Remove(0, 2);
                buyPrice = buyPrice.Remove(buyPrice.Length - 1, 1);
                XmlElement priceElement2 = doc.CreateElement("", "BuyPrice", "");
                priceElement2.InnerText = buyPrice;
                priceElement1.AppendChild(priceElement2);
            }
            doc.Save("tickers.xml");
            Close();
        }
        /// <summary>
        /// Delete button functions
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            using (Prompt prompt = new Prompt("Enter the ticker symbol", "Delete ticker"))
            {
                string ticker = prompt.Result;
                ticker = ticker.ToUpper();
                if (!string.IsNullOrEmpty(ticker))
                {
                    tableLayoutPanel1.SuspendLayout();
                    ReplaceAllTheLabels(ticker);
                    tableLayoutPanel1.RowStyles.RemoveAt(tableLayoutPanel1.RowCount - 1);
                    tableLayoutPanel1.RowCount--;
                    tickerCounter--;
                    tableLayoutPanelCounter--;
                    changeRowCounter--;
                    tickerList.Remove(ticker);
                    foreach (List<object> list in positionList.ToList())
                    {
                        foreach (object item in list)
                        {
                            if (item.ToString() == ticker)
                            {
                                positionList.Remove(list);
                                break;
                            }
                        }
                    }
                    tableLayoutPanel1.ResumeLayout();
                    tableLayoutPanel1.PerformLayout();
                    this.Size = new System.Drawing.Size(new System.Drawing.Point(200));
                    this.PerformLayout();
                }
            }
        }

        private void ReplaceAllTheLabels(string ticker)
        {
            var controlsToRemove = new List<Control>();
            int deletedRow = 0;
            foreach (Control c in tableLayoutPanel1.Controls)
            {
                if (c.Name.StartsWith(ticker))
                {
                    controlsToRemove.Add(c);
                    deletedRow = tableLayoutPanel1.GetRow(c);
                }
            }
            foreach (Control c in controlsToRemove)
            {
                c.Dispose();
            }
            int totalRows = tableLayoutPanel1.RowCount;
            int j = 0;
            int h = 1;
            for (int i = 0; i < totalRows; i++)
            {
                if (deletedRow <= i)
                {
                    h = deletedRow + 1;
                    if (h != totalRows)
                    {
                        //take labels from next row
                        Label label1 = (Label)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel1 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel2 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel3 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel4 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel5 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel6 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel7 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        j = 0;

                        //start adding labels to removed row
                        h--;
                        tableLayoutPanel1.Controls.Add(label1, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel1, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel2, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel3, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel4, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel5, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel6, j, h);
                        j++;
                        tableLayoutPanel1.Controls.Add(panel7, j, h);
                        j = 0;

                        //remove labels from next row
                        h++;
                        label1 = (Label)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(label1);
                        j++;
                        panel1 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel1);
                        j++;
                        panel2 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel2);
                        j++;
                        panel3 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel3);
                        j++;
                        panel4 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel4);
                        j++;
                        panel5 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel5);
                        j++;
                        panel6 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel6);
                        j++;
                        panel7 = (TableLayoutPanel)tableLayoutPanel1.GetControlFromPosition(j, h);
                        tableLayoutPanel1.Controls.Remove(panel7);
                        deletedRow++;
                        j = 0;
                    }
                }
            }
        }
        /// <summary>
        /// Timer related functions
        /// </summary>
        private async Task StartTimer(Action action, CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationTokenSource.Token);
                    action();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void onCancelClick()
        {
            _cancellationToken.Cancel();
        }
    }
}


