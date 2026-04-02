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
            dgvBillItems.CellValidating += DgvBillItems_CellValidating;
            dgvBillItems.DataError += (s, ev) => 
            {
                ev.ThrowException = false;
            };
            dgvBillItems.RowsRemoved += (s, ev) => UpdateBillTotal();
            dgvBillItems.CurrentCellDirtyStateChanged += (s, ev) => 
            {
                if (dgvBillItems.IsCurrentCellDirty) 
                {
                    dgvBillItems.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
        }

        private void DgvBillItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvBillItems.Rows[e.RowIndex].IsNewRow) return;

            string colName = dgvBillItems.Columns[e.ColumnIndex].Name;
            string value = Convert.ToString(e.FormattedValue).Trim();

            if (string.IsNullOrEmpty(value)) return;

            try
            {
                if (colName == "Qty")
                {
                    if (!int.TryParse(value, out _))
                    {
                        MessageBox.Show("Quantity must be a valid integer.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                    }
                }
                else if (colName == "Rate" || colName == "Discount" || colName == "Amount")
                {
                    if (!decimal.TryParse(value, out _))
                    {
                        MessageBox.Show(colName + " must be a valid number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                    }
                }
                else if (colName == "Expiry")
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^(0[1-9]|1[0-2])\/\d{4}$"))
                    {
                        MessageBox.Show("Expiry date must be in MM/yyyy format.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Validation error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvBillItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("Error calculating amount: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateBillTotal()
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show("Error updating total: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtGiven_TextChanged(object sender, EventArgs e)
        {
            CalculateBalance();
        }

        private void CalculateBalance()
        {
            try
            {
                decimal total = 0;
                decimal.TryParse(lblTotal.Text.Replace("Total: ", ""), out total);
                
                decimal given = 0;
                decimal.TryParse(txtGiven.Text, out given);
                
                decimal balance = given - total;
                txtBalance.Text = balance.ToString("0.00");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error calculating balance: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                bool hasItems = false;

                foreach (DataGridViewRow row in dgvBillItems.Rows)
                {
                    if (row.IsNewRow) continue;
                    hasItems = true;

                    string drugName = Convert.ToString(row.Cells["DrugName"].Value ?? "").Trim();
                    string scheme = Convert.ToString(row.Cells["Scheme"].Value ?? "").Trim();
                    string manufacturer = Convert.ToString(row.Cells["Manufacturer"].Value ?? "").Trim();
                    string batchNo = Convert.ToString(row.Cells["BatchNo"].Value ?? "").Trim();
                    string expiry = Convert.ToString(row.Cells["Expiry"].Value ?? "").Trim();
                    string strQty = Convert.ToString(row.Cells["Qty"].Value ?? "").Trim();
                    string strRate = Convert.ToString(row.Cells["Rate"].Value ?? "").Trim();
                    string strDiscount = Convert.ToString(row.Cells["Discount"].Value ?? "").Trim();
                    string strAmount = Convert.ToString(row.Cells["Amount"].Value ?? "").Trim();

                    if (string.IsNullOrEmpty(drugName) || string.IsNullOrEmpty(scheme) || 
                        string.IsNullOrEmpty(manufacturer) || string.IsNullOrEmpty(batchNo) || 
                        string.IsNullOrEmpty(expiry) || string.IsNullOrEmpty(strQty) || 
                        string.IsNullOrEmpty(strRate) || string.IsNullOrEmpty(strDiscount) || 
                        string.IsNullOrEmpty(strAmount))
                    {
                        MessageBox.Show("All bill item fields except Patient Name and Doctor Name are mandatory to save.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    int qty;
                    if (!int.TryParse(strQty, out qty))
                    {
                        MessageBox.Show("Quantity must be a valid integer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    
                    decimal rate;
                    if (!decimal.TryParse(strRate, out rate))
                    {
                        MessageBox.Show("Rate must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    
                    decimal amt;
                    if (!decimal.TryParse(strAmount, out amt))
                    {
                        MessageBox.Show("Amount must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    decimal discount;
                    if (!decimal.TryParse(strDiscount, out discount))
                    {
                        MessageBox.Show("Discount must be a valid number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(expiry, @"^(0[1-9]|1[0-2])\/\d{4}$"))
                    {
                        MessageBox.Show("Expiry date must be in MM/yyyy format.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    var item = new BillingRecord
                    {
                        Patient = pName,
                        Prescriber = dName,
                        BillNo = bNo,
                        BillDate = bDate,
                        Qty = qty,
                        DrugName = drugName,
                        Scheme = scheme,
                        Manufacturer = manufacturer,
                        BatchNo = batchNo,
                        ExpiryDate = expiry,
                        Rate = rate,
                        Discount = discount,
                        Amount = amt
                    };
                    
                    records.Add(item);
                }

                if (!hasItems)
                {
                    MessageBox.Show("Please add at least one item to the bill before saving.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                _billingService.SaveRecords(records);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
