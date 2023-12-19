using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PasswordManager
{
    public partial class MainForm : Form
    {
        private static readonly string connStr = @"Server=localhost\SQLEXPRESS;Database=Passwords;Trusted_Connection=True";
        private string PlaceHolderEmail = "email@example.com";
        private string PlaceHolderPassword = "password";
        private string userEmail;
        private bool operationInProgress = false;

        public MainForm(string email)
        {
            userEmail = email;

            // Antiflickering
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            InitializeComponent();
            InitializeListView();

            GetAllPlatforms();
        }

        private void GetAllPlatforms()
        {
            using (var conn = new SqlConnection(connStr))
            {
                try { conn.Open(); }
                catch (Exception e) { MessageBox.Show($"Could not connect to db. Error: {e.Message}"); }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "spSelectPlatforms";
                    cmd.Parameters.AddWithValue("@userEmail", userEmail);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            AddListItem(dr.GetString(0));
                        }
                    }
                }
            }
        }

        private void InitializeListView()
        {
            // Set up the ListView
            accountsListView.View = View.Tile;
            accountsListView.OwnerDraw = true;
            accountsListView.DrawItem += listView_DrawItem;

            accountsListView.TileSize = new Size(300, 200);
            accountsListView.BorderStyle = BorderStyle.Fixed3D;

            accountsListView.Columns.Add("Platform", 100);
            accountsListView.Columns.Add("Email", 150);
            accountsListView.Columns.Add("Password", 100);

            var contextMenu = new ContextMenu();
            var deleteMenu = new MenuItem();
            deleteMenu.Text = "Remove Account";
            deleteMenu.Click += (s, e) =>
            {
                if (operationInProgress)
                    return;
                operationInProgress = true;

                if (!VerifyIdentity())
                    return;

                var menuItem = s as MenuItem;
                ContextMenu = menuItem.GetContextMenu();

                if (contextMenu != null && contextMenu.SourceControl is ListView)
                {
                    var list = contextMenu.SourceControl as ListView;
                    DeleteCredentialsFor(list.FocusedItem.Text);
                    list.Items.Remove(list.FocusedItem);
                    list.Invalidate();
                }

                operationInProgress = false;
            };
            contextMenu.MenuItems.Add(deleteMenu);
            accountsListView.ContextMenu = contextMenu;

            accountsListView.MouseClick += (sender, eventArgs) =>
            {
                if (eventArgs.Button != MouseButtons.Left)
                    return;

                if (operationInProgress)
                    return;
                operationInProgress = true;

                if (!VerifyIdentity())
                    return;

                var item = ((System.Windows.Forms.ListView)sender).FocusedItem;

                GetCredentialsFor(item.Text, out string email, out string password);
                item.SubItems[1].Text = email;
                item.SubItems[2].Text = password;
                item.SubItems[1].ForeColor = item.SubItems[2].ForeColor = Color.Red;

                Timer timer = new Timer()
                {
                    Interval = 1000
                };
                int ticks = 15;
                timer.Tick += (o, e) =>
                {
                    statusLabel.Text = $"{ticks} seconds until hiding";
                    if (ticks == 0)
                    {
                        item.SubItems[1].Text = PlaceHolderEmail;
                        item.SubItems[2].Text = PlaceHolderPassword;
                        item.SubItems[1].ForeColor = item.SubItems[2].ForeColor = Color.Black;
                        statusLabel.Text = "";
                        timer.Stop();
                        operationInProgress = false;
                    }
                    ticks--;
                };
                timer.Start();
            };
        }

        private bool VerifyIdentity()
        {
            var frm = new VerifyForm(userEmail);
            frm.ShowDialog();
            return frm.Verified;
        }

        private void DeleteCredentialsFor(string platform)
        {
            using (var conn = new SqlConnection(connStr))
            {
                try { conn.Open(); }
                catch (Exception e) { MessageBox.Show($"Could not connect to db. Error: {e.Message}"); }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "spDeleteCredentials";
                    cmd.Parameters.AddWithValue("@platform", platform);
                    cmd.Parameters.AddWithValue("@userEmail", userEmail);

                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        statusLabel.Text = "Account deleted successfully";
                        FlickerLabel(2, Color.Yellow);

                        Timer t = new Timer { Interval = 5 * 1000 };
                        t.Tick += (s, e) =>
                        {
                            statusLabel.Text = "";
                            t.Stop();
                        };
                        t.Start();
                    }
                    else
                        MessageBox.Show("Account could not be deleted");
                }
            }
        }

        private void GetCredentialsFor(string platform, out string email, out string password)
        {
            using (var conn = new SqlConnection(connStr))
            {
                try { conn.Open(); }
                catch (Exception e) { MessageBox.Show($"Could not connect to db. Error: {e.Message}"); }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "spGetCredentials";
                    cmd.Parameters.AddWithValue("@platform", platform);
                    cmd.Parameters.AddWithValue("@userEmail", userEmail);

                    using (var dr = cmd.ExecuteReader())
                    {
                        dr.Read();
                        email = dr.GetString(0);
                        password = dr.GetString(1);
                    }
                }
            }
        }

        private void listView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawBackground();

            // Draws border
            e.Graphics.DrawLines(new Pen(Color.Gray, 2), new Point[] { 
                new Point(e.Bounds.X, e.Bounds.Y),
                new Point(e.Bounds.X, e.Bounds.Y + e.Bounds.Height),
                new Point(e.Bounds.X + e.Bounds.Width, e.Bounds.Y + e.Bounds.Height),
                new Point(e.Bounds.X + e.Bounds.Width, e.Bounds.Y),
                new Point(e.Bounds.X, e.Bounds.Y),
            });

            int centerY = e.Bounds.Top + (e.Bounds.Height - GetTotalHeight(e.Item)) / 2;

            for (int i = 0; i < e.Item.SubItems.Count; i++)
            {
                var subItem = e.Item.SubItems[i];

                Size textSize = TextRenderer.MeasureText(subItem.Text, subItem.Font);

                int x = e.Bounds.Left + (e.Bounds.Width - textSize.Width) / 2;
                int y = centerY;

                e.Graphics.DrawRectangle(new Pen(subItem.BackColor), subItem.Bounds);
                TextRenderer.DrawText(e.Graphics, subItem.Text, subItem.Font, new Point(x, y), subItem.ForeColor);
                
                if (i == 0)
                    centerY += textSize.Height + 20; // Add extra spacing for the first item
                else
                    centerY += textSize.Height; 
            }
        }

        private int GetTotalHeight(ListViewItem item)
        {
            int totalHeight = 0;
            foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                totalHeight += TextRenderer.MeasureText(subItem.Text, item.Font).Height;
            return totalHeight;
        }

        private void AddListItem(string platform)
        {
            ListViewItem item = new ListViewItem(platform);
            item.SubItems.Add(PlaceHolderEmail);
            item.SubItems.Add(PlaceHolderPassword);
            item.BackColor = Color.Lavender;

            item.SubItems[0].Font = new Font("Arial", 20, FontStyle.Bold);
            item.SubItems[1].Font = new Font("Arial", 14);
            item.SubItems[2].Font = new Font("Arial", 14);

            accountsListView.Items.Add(item);
        }

        private void btnAddAccount_Click(object sender, EventArgs e)
        {
            string platform = txtAddPlatform.Text.Trim();
            string email    = txtAddEmail.Text.Trim();
            string password = txtAddPassword.Text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Incorrect input data");
                return;
            }

            txtAddPlatform.Text = "";
            txtAddEmail   .Text = "";
            txtAddPassword.Text = "";

            AddAccount(platform, email, password);
        }

        void AddAccount(string platform, string email, string password)
        {
            using (var conn = new SqlConnection(connStr))
            {
                try { conn.Open(); }
                catch (Exception e) { MessageBox.Show($"Could not connect to db. Error: {e.Message}"); }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "spInsertAccount";
                    cmd.Parameters.AddWithValue("@platform", platform);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@userEmail", userEmail);

                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        statusLabel.Text = "Account added successfully";
                        FlickerLabel(2, Color.Yellow);

                        Timer t = new Timer { Interval = 5 * 1000 };
                        t.Tick += (s, e) =>
                        {
                            statusLabel.Text = "";
                            t.Stop();
                        };
                        t.Start();
                        AddListItem(platform);
                    }
                    else
                        MessageBox.Show("Account could not be added");
                }
            }
        }

        private void FlickerLabel(int noTimes, Color color)
        {
            noTimes *= 2;
            Timer t = new Timer { Interval = 500 };
            t.Tick += (s, e) =>
            {
                statusLabel.BackColor = noTimes % 2 ==  1 ? Color.White : color; 
                statusLabel.Invalidate();
                noTimes--;
                if(noTimes == 0) 
                    t.Stop();
            };
            t.Start();
        }

        internal static bool ExistsUser(string email, string password)
        {
            using (var conn = new SqlConnection(connStr))
            {
                try { conn.Open(); }
                catch (Exception e) { MessageBox.Show($"Could not connect to db. Error: {e.Message}"); }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "spExistsUser";
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);

                    return (int)cmd.ExecuteScalar() == 1;
                }
            }
        }
    }
}
