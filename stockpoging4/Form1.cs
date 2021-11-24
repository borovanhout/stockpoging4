using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using YahooFinanceApi;

namespace stockpoging4
{
    public partial class Form1 : Form
    {
        Task _runningTask;
        CancellationTokenSource _cancellationToken;
        int tickerCounter = 1;
        int priceCounter = 1;
        int positionCounter = 0;
        int tableLayoutPanelCounter = 1;
        int saveTickerCounter = 1;
        int savePriceCounter = 1;
        int savePositionCounter = 1;
        List<string> tickerList = new List<string>();

        public Form1()
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            InitializeComponent();
            FillMeta();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string ticker;
            using (Prompt prompt = new Prompt("Enter the ticker symbol", "Add ticker"))
            {
                ticker = prompt.Result;
                ticker = ticker.ToUpper();
                if (!string.IsNullOrEmpty(ticker))
                {

                    using (Prompt prompt2 = new Prompt("Enter your position", "Add ticker"))
                    {
                        double price;
                        int volume;
                        if (Int32.TryParse(prompt2.Result, out volume) == true)
                        {
                            using (Prompt prompt3 = new Prompt("Enter your buy price", "Add ticker"))
                            {
                                if (Double.TryParse(prompt3.Result, out price) == true)
                                {
                                    FillTickerLabel(ticker);
                                    FillPrice(ticker);
                                    FillPositionLabel(volume, price, ticker);
                                    _cancellationToken = new CancellationTokenSource();
                                    _runningTask = StartTimer(() => KeepUpdatingPrice(ticker), _cancellationToken);
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("You did not enter one of the textboxes correctly");
                }
            }
        }

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


        public async Task<string> GetStockPrices(string symbol)
        {
            try
            {
                var securities = await Yahoo.Symbols(symbol).Fields(Field.RegularMarketPrice).QueryAsync();
                var aapl = securities[symbol];
                string price = aapl[Field.RegularMarketPrice].ToString();
                return price;
            }
            catch
            {
                return "404";
            }
        }

        private void onCancelClick()
        {
            _cancellationToken.Cancel();
        }

        public async void FillPrice(string ticker)
        {
            var price = await GetStockPrices(ticker);
            Label label = new Label() { Name = ticker + "Price", Tag = ticker, Text = price, TextAlign = ContentAlignment.MiddleCenter };
            tableLayoutPanel2.Controls.Add(label, 2, priceCounter);
            priceCounter++;
        }


        public void FillPriceLabel(string ticker, string price)
        {
            Label label = new Label() { Name = ticker + "Price", Tag = ticker, Text = price, TextAlign = ContentAlignment.MiddleCenter };
            tableLayoutPanel2.Controls.Add(label, 2, priceCounter);
            priceCounter++;
        }

        private void FillMeta()
        {
            string[] meta = { "Ticker", "Current position", "Current price", "Gain/loss", "Change 1d%", "Change 7d%" };
            int metaCounter = 0;
            foreach (string str in meta)
            {
                Label label = new Label() { Name = str, Text = str, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Label.DefaultFont, FontStyle.Bold) };
                tableLayoutPanel2.Controls.Add(label, metaCounter, 0);
                metaCounter++;
            }
        }

        private void FillTickerLabel(string result)
        {
            Label label = new Label() { Name = result, Text = result, Tag = result, TextAlign = ContentAlignment.MiddleCenter, AutoScrollOffset = new Point(0, 0) };
            if (tickerCounter > 2)
            {
                tableLayoutPanel2.RowCount++;
            }
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel2.Controls.Add(label, 0, tickerCounter);
            tickerCounter++;
        }

        public async void KeepUpdatingPrice(string result)
        {
            var price = await GetStockPrices(result);
            foreach (Label label in tableLayoutPanel2.Controls.OfType<Label>())
            {
                if (label.Name == result + "Price")
                {
                    label.Text = price;
                    label.Refresh();
                }
            }
        }

        private void form_Load(object sender, EventArgs e)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("tickers.xml");
                XmlNodeList tickers = doc.SelectNodes("//Ticker");
                XmlNodeList prices = doc.SelectNodes("//Price");
                XmlNodeList totals = doc.SelectNodes("//Total");
                XmlNodeList volumePrices = doc.SelectNodes("//VolumePrice");
                for (int i = 0; i < tickers.Count; i++)
                {
                    string ticker = tickers[i].InnerText;
                    string price = prices[i].InnerText;
                    double total = Double.Parse(totals[i].InnerText);
                    string volumePrice = volumePrices[i].InnerText;
                    string[] split = volumePrice.Split(' ');

                    FillTickerLabel(ticker);
                    FillPriceLabel(ticker, price);
                    FillPositionLabel(Int32.Parse(split[0]), Double.Parse(split[2]), ticker);
                    _cancellationToken = new CancellationTokenSource();
                    _runningTask = StartTimer(() => KeepUpdatingPrice(ticker), _cancellationToken);
                }
            }
            catch
            {
                MessageBox.Show("No existing file found");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int forCount = tableLayoutPanel2.RowCount;
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.CreateElement("", "Body", "");
            doc.AppendChild(root);

            XmlElement tickerElement1 = doc.CreateElement("", "TickerCollection", "");
            root.AppendChild(tickerElement1);
            for (int i = 0; i < forCount; i++)
            {
                Label ticker = (Label)tableLayoutPanel2.GetControlFromPosition(0, saveTickerCounter);
                XmlElement tickerElement2 = doc.CreateElement("", "Ticker", "");
                tickerElement2.InnerText = ticker.Name;
                tickerElement1.AppendChild(tickerElement2);
                saveTickerCounter++;
            }
            XmlElement priceElement1 = doc.CreateElement("", "PriceCollection", "");
            root.AppendChild(priceElement1);
            for (int i = 0; i < forCount; i++)
            {
                Label price = (Label)tableLayoutPanel2.GetControlFromPosition(2, savePriceCounter);
                XmlElement priceElement2 = doc.CreateElement("", "Price", "");
                priceElement2.InnerText = price.Text;
                priceElement1.AppendChild(priceElement2);
                savePriceCounter++;
            }
            XmlElement positionElement1 = doc.CreateElement("", "PositionCollection", "");
            root.AppendChild(positionElement1);
            for (int i = 0; i < forCount; i++)
            {
                XmlElement positionElement2 = doc.CreateElement("", "Total", "");
                TableLayoutPanel panel = (TableLayoutPanel)tableLayoutPanel2.GetControlFromPosition(1, savePositionCounter);
                Label totalLabel = (Label)panel.GetControlFromPosition(0, 0);
                positionElement2.InnerText = totalLabel.Text;
                positionElement1.AppendChild(positionElement2);


                XmlElement positionElement3 = doc.CreateElement("", "VolumePrice", "");
                Label volumePriceLabel = (Label)panel.GetControlFromPosition(0, 1);
                positionElement3.InnerText = volumePriceLabel.Text;
                positionElement1.AppendChild(positionElement3);
                savePositionCounter++;
            }
            doc.Save("tickers.xml");
            Close();
        }

        private void FillPositionLabel(int volume, double price, string ticker)
        {
            double position = volume * price;
            Label positionLabel = new Label() { Name = ticker + "TotalPos", Text = Convert.ToString(position), Tag = ticker, TextAlign = ContentAlignment.MiddleCenter, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 9) };
            Label metaLabel = new Label() { Name = ticker + "ValuePricePos", Text = Convert.ToString(volume) + " x " + Convert.ToString(price), Tag = ticker, TextAlign = ContentAlignment.MiddleCenter, AutoScrollOffset = new Point(0, 0), Font = new Font("Arial", 6) };
            TableLayoutPanel newTableLayoutPanel = new TableLayoutPanel();
            newTableLayoutPanel = CreateNewTableLayoutPanel(newTableLayoutPanel, ticker);
            newTableLayoutPanel.Controls.Add(positionLabel, 0, 0);
            newTableLayoutPanel.Controls.Add(metaLabel, 0, 1);
        }

        private TableLayoutPanel CreateNewTableLayoutPanel(TableLayoutPanel newTableLayoutPanel, string ticker)
        {
            tableLayoutPanel2.Controls.Add(newTableLayoutPanel, 1, tableLayoutPanelCounter);
            newTableLayoutPanel.Name = ticker + "innerTableLayoutPanel";
            newTableLayoutPanel.AutoSize = true;
            newTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            newTableLayoutPanel.ColumnCount = 1;
            newTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            newTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            newTableLayoutPanel.Location = new System.Drawing.Point(136, 25);
            newTableLayoutPanel.RowCount = 2;
            newTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            newTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanelCounter++;
            return newTableLayoutPanel;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (Prompt prompt = new Prompt("Enter the ticker symbol", "Delete ticker"))
            {
                string ticker = prompt.Result;
                ticker = ticker.ToUpper();
                if (!string.IsNullOrEmpty(ticker))
                {
                    ReplaceAllTheLabels(ticker);
                    tableLayoutPanel2.RowStyles.RemoveAt(tableLayoutPanel2.RowCount);
                    tableLayoutPanel2.RowCount--;
                    tickerCounter--;
                    priceCounter--;
                    positionCounter--;
                    tableLayoutPanelCounter--;
                }
            }
        }

        private void ReplaceAllTheLabels(string ticker)
        {
            var controlsToRemove = new List<Control>();
            int deletedRow = 0;
            foreach (Control c in tableLayoutPanel2.Controls)
            {
                if (c.Name.StartsWith(ticker))
                {
                    controlsToRemove.Add(c);
                    deletedRow = tableLayoutPanel2.GetRow(c);
                }
            }
            foreach (Control c in controlsToRemove)
            {
                c.Dispose();
            }
            tableLayoutPanel2.Refresh();
            int totalRows = tableLayoutPanel2.RowCount;
            int j = 0;
            int h = 1;
            for (int i = 0; i < totalRows; i++)
            {
                if (deletedRow <= i)
                {
                    h = deletedRow + 1;
                    if (deletedRow != totalRows)
                    {
                        //take labels from next row
                        Label label1 = (Label)tableLayoutPanel2.GetControlFromPosition(j, h);
                        j++;
                        TableLayoutPanel panel1 = (TableLayoutPanel)tableLayoutPanel2.GetControlFromPosition(j, h);
                        Label label2 = (Label)panel1.GetControlFromPosition(0, 0);
                        Label label3 = (Label)panel1.GetControlFromPosition(0, 1);
                        j++;
                        Label label4 = (Label)tableLayoutPanel2.GetControlFromPosition(j, h);
                        h--;
                        j = 0;

                        //start adding labels to removed row
                        tableLayoutPanel2.Controls.Add(label1, j, h);
                        j++;
                        tableLayoutPanel2.Controls.Add(panel1, j, h);
                        panel1.Controls.Add(label2, 0, 0);
                        panel1.Controls.Add(label3, 0, 1);
                        j++;
                        tableLayoutPanel2.Controls.Add(label4, j, h);
                        j = 0;

                        //remove labels from next row
                        h++;
                        Label label5 = (Label)tableLayoutPanel2.GetControlFromPosition(j, h);
                        tableLayoutPanel2.Controls.Remove(label5);
                        j++;
                        TableLayoutPanel panel2 = (TableLayoutPanel)tableLayoutPanel2.GetControlFromPosition(j, h);
                        tableLayoutPanel2.Controls.Remove(panel2);
                        j++;
                        Label label6 = (Label)tableLayoutPanel2.GetControlFromPosition(j, h);
                        tableLayoutPanel2.Controls.Remove(label6);
                        deletedRow++;
                        j = 0;
                    }
                }
            }
        }
    }
}


