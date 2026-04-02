using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Drawing;
using CentralMed.Models;
using CentralMed.Services;

namespace CentralMed.UI
{
    public partial class BillingForm : Form
    {
        private BillingService _billingService;
        
        public BillingForm()
        {
            InitializeComponent();
            _billingService = new BillingService();
        }

        private void BillingForm_Load(object sender, EventArgs e)
        {
            txtBillNo.Text = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            dtpDate.Value = DateTime.Now;

            dgvBillItems.CellValueChanged += DgvBillItems_CellValueChanged;
            dgvBillItems.RowsRemoved += (s, ev) => UpdateBillTotal();
            dgvBillItems.CurrentCellDirtyStateChanged += (s, ev) => 
            {
                if (dgvBillItems.IsCurrentCellDirty) 
                {
                    dgvBillItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
        }

        private void DgvBillItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string colName = dgvBillItems.Columns[e.ColumnIndex].Name;
                if (colName == "Qty" || colName == "Rate" || colName == "Discount")
                {
                    var row = dgvBillItems.Rows[e.RowIndex];
                    
                    int qty = 0;
                    int.TryParse(Convert.ToString(row.Cells["Qty"].Value), out qty);
                    
                    decimal rate = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Rate"].Value), out rate);
                    
                    decimal discount = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Discount"].Value), out discount);
                    
                    row.Cells["Amount"].Value = (qty * rate) - discount;
                    UpdateBillTotal();
                }
            }
        }

        private void UpdateBillTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvBillItems.Rows)
            {
                if (!row.IsNewRow)
                {
                    decimal amt = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Amount"].Value), out amt);
                    total += amt;
                }
            }
            lblTotal.Text = "Total: " + total.ToString("0.00");
            CalculateBalance();
        }

        private void txtGiven_TextChanged(object sender, EventArgs e)
        {
            CalculateBalance();
        }

        private void CalculateBalance()
        {
            decimal total = 0;
            decimal.TryParse(lblTotal.Text.Replace("Total: ", ""), out total);
            
            decimal given = 0;
            decimal.TryParse(txtGiven.Text, out given);
            
            decimal balance = given - total;
            txtBalance.Text = balance.ToString("0.00");
        }

        private bool SaveBillData()
        {
            try
            {
                var records = new List<BillingRecord>();
                string pName = txtPatientName.Text;
                string dName = txtDoctorName.Text;
                string bNo = txtBillNo.Text;
                DateTime bDate = dtpDate.Value;

                foreach (DataGridViewRow row in dgvBillItems.Rows)
                {
                    if (row.IsNewRow) continue;

                    int qty = 0;
                    int.TryParse(Convert.ToString(row.Cells["Qty"].Value), out qty);
                    
                    decimal rate = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Rate"].Value), out rate);
                    
                    decimal amt = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Amount"].Value), out amt);

                    decimal discount = 0;
                    decimal.TryParse(Convert.ToString(row.Cells["Discount"].Value), out discount);

                    var item = new BillingRecord
                    {
                        Patient = pName,
                        Prescriber = dName,
                        BillNo = bNo,
                        BillDate = bDate,
                        Qty = qty,
                        DrugName = Convert.ToString(row.Cells["DrugName"].Value ?? ""),
                        Scheme = Convert.ToString(row.Cells["Scheme"].Value ?? ""),
                        Manufacturer = Convert.ToString(row.Cells["Manufacturer"].Value ?? ""),
                        BatchNo = Convert.ToString(row.Cells["BatchNo"].Value ?? ""),
                        ExpiryDate = Convert.ToString(row.Cells["Expiry"].Value ?? ""),
                        Rate = rate,
                        Discount = discount,
                        Amount = amt
                    };
                    
                    records.Add(item);
                }

                _billingService.SaveRecords(records);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ResetForm()
        {
            // Reset form
            txtBillNo.Text = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            txtPatientName.Clear();
            txtDoctorName.Clear();
            dgvBillItems.Rows.Clear();
            txtGiven.Clear();
            lblTotal.Text = "Total: 0.00";
            txtBalance.Clear();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (SaveBillData())
            {
                MessageBox.Show("Records saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetForm();
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (SaveBillData())
            {
                try
                {
                    PrintDocument pd = new PrintDocument();
                    pd.PrintPage += new PrintPageEventHandler(PrintBillPage);
                    
                    int itemCount = 0;
                    foreach (DataGridViewRow row in dgvBillItems.Rows)
                    {
                        if (!row.IsNewRow) itemCount++;
                    }
                    
                    // Adjust height dynamically based on items (assuming about ~20 units per row, + ~320 for headers/footers)
                    // Width = 800 (8 inches width standard receipt / A4 width chunk)
                    int requiredHeight = 320 + (itemCount * 22);
                    if (requiredHeight < 350) requiredHeight = 350; // Minimum size trimmed
                    
                    pd.DefaultPageSettings.PaperSize = new PaperSize("CustomReceipt", 800, requiredHeight);
                    
                    PrintPreviewDialog printPreview = new PrintPreviewDialog();
                    printPreview.Document = pd;
                    printPreview.ShowDialog();
                    
                    MessageBox.Show("Records saved and printed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ResetForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Printing error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintBillPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Font font = new Font("Courier New", 10);
            Font boldFont = new Font("Courier New", 10, FontStyle.Bold);
            Font titleFont = new Font("Courier New", 18, FontStyle.Bold);
            float fontHeight = font.GetHeight();
            
            int startX = 30; // slightly wider margin
            int startY = 40;
            int offset = 30;

            graphics.DrawString("MEDICAL SHOP INVOICE", titleFont, Brushes.Black, startX, startY);
            
            offset += 40;
            graphics.DrawString("Bill No: " + txtBillNo.Text.PadRight(20) + "Date: " + dtpDate.Value.ToString("dd/MMM/yyyy"), font, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 5;
            graphics.DrawString("Patient: " + txtPatientName.Text.PadRight(20) + "Dr: " + txtDoctorName.Text, font, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 20;
            graphics.DrawString("--------------------------------------------------------------------------------", font, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 5;
            // Qty(4) Drug(15) Sche(8) Mfrs(10) No(8) DE(7) Rate(8) Disc(6) Amount(10)
            graphics.DrawString("Qty".PadRight(4) + "Drug Name".PadRight(15) + "Sche".PadRight(8) + "Mfrs".PadRight(10) + "No".PadRight(8) + "DE".PadRight(7) + "Rate".PadRight(8) + "Disc".PadRight(6) + "Amount", boldFont, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 5;
            graphics.DrawString("--------------------------------------------------------------------------------", font, Brushes.Black, startX, startY + offset);

            foreach (DataGridViewRow row in dgvBillItems.Rows)
            {
                if (row.IsNewRow) continue;
                
                string qty = Convert.ToString(row.Cells["Qty"].Value ?? "0");
                
                string name = Convert.ToString(row.Cells["DrugName"].Value ?? "");
                if (name.Length > 13) name = name.Substring(0, 13);
                
                string scheme = Convert.ToString(row.Cells["Scheme"].Value ?? "");
                if (scheme.Length > 6) scheme = scheme.Substring(0, 6);
                
                string mfrs = Convert.ToString(row.Cells["Manufacturer"].Value ?? "");
                if (mfrs.Length > 8) mfrs = mfrs.Substring(0, 8);
                
                string batch = Convert.ToString(row.Cells["BatchNo"].Value ?? "");
                if (batch.Length > 6) batch = batch.Substring(0, 6);
                
                string expiry = Convert.ToString(row.Cells["Expiry"].Value ?? "");
                if (expiry.Length > 5) expiry = expiry.Substring(0, 5);

                string rate = Convert.ToDecimal(row.Cells["Rate"].Value ?? 0).ToString("0.00");
                string discount = Convert.ToDecimal(row.Cells["Discount"].Value ?? 0).ToString("0.00");
                string amt = Convert.ToDecimal(row.Cells["Amount"].Value ?? 0).ToString("0.00");

                offset += (int)fontHeight + 5;
                graphics.DrawString(qty.PadRight(4) + name.PadRight(15) + scheme.PadRight(8) + mfrs.PadRight(10) + batch.PadRight(8) + expiry.PadRight(7) + rate.PadRight(8) + discount.PadRight(6) + amt, font, Brushes.Black, startX, startY + offset);
            }

            offset += 15;
            graphics.DrawString("--------------------------------------------------------------------------------", font, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 5;
            graphics.DrawString(lblTotal.Text, titleFont, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 10;
            graphics.DrawString("Amount Given: " + txtGiven.Text, boldFont, Brushes.Black, startX, startY + offset);
            
            offset += (int)fontHeight + 5;
            graphics.DrawString("Balance     : " + txtBalance.Text, boldFont, Brushes.Black, startX, startY + offset);
        }
    }
}
